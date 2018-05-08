using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _1STool1CD
{
    public static class Utils1CD
    {
        public struct index_record
        {
            public v8Field field;
            public Int32 len;
        }

        struct unpack_index_record
        {
            UInt32 _record_number; // номер (индекс) записи в таблице записей
                                   //unsigned char _index[1]; // значение индекса записи. Реальная длина значения определяется полем length класса index
            public byte[] _index;
        }
        /// <summary>
        /// Версии формата базы 1С
        /// </summary>
        public enum db_ver
        {
            ver8_0_3_0  = 1,
        	ver8_0_5_0  = 2,
	        ver8_1_0_0  = 3,
	        ver8_2_0_0  = 4,
	        ver8_2_14_0 = 5,
	        ver8_3_8_0  = 6
        }

        public enum node_type
        {
            nd_empty      = 0,	// пусто
	        nd_string     = 1,	// строка
	        nd_number     = 2,	// число
	        nd_number_exp = 3,	// число с показателем степени
	        nd_guid       = 4,	// уникальный идентификатор
	        nd_list       = 5,	// список
	        nd_binary     = 6,	// двоичные данные (с префиксом #base64:)
	        nd_binary2    = 7,	// двоичные данные формата 8.2 (без префикса)
	        nd_link       = 8,	// ссылка
	        nd_binary_d   = 9,	// двоичные данные (с префиксом #data:)
	        nd_unknown          // неизвестный тип
        }
                
        /// <summary>
        /// 0x7FFFFFFF - Обозначение последней страницы
        /// </summary>
        public static readonly Int32 LAST_PAGE = Int32.MaxValue;

        public struct leaf_page_header
        {
            public Int16 flags; // offset 0
            public UInt16 number_indexes; // offset 2
            public UInt32 prev_page; // offset 4 // для 8.3.8 - это номер страницы (реальное смещение = prev_page * pagesize), до 8.3.8 - это реальное смещение
            public UInt32 next_page; // offset 8 // для 8.3.8 - это номер страницы (реальное смещение = next_page * pagesize), до 8.3.8 - это реальное смещение
            public UInt16 freebytes; // offset 12
            public UInt32 numrecmask; // offset 14
            public UInt16 leftmask; // offset 18
            public UInt16 rightmask; // offset 20
            public UInt16 numrecbits; // offset 22
            public UInt16 leftbits; // offset 24
            public UInt16 rightbits; // offset 26
            public UInt16 recbytes; // offset 28
        }

        public static leaf_page_header ByteArrayToLeafPageHeader(byte[] src)
        {

            leaf_page_header Res;

            Res.flags = BitConverter.ToInt16(src, 0);
            Res.number_indexes = BitConverter.ToUInt16(src, 2);
            Res.prev_page = BitConverter.ToUInt32(src, 4);
            Res.next_page = BitConverter.ToUInt32(src, 8);
            Res.freebytes = BitConverter.ToUInt16(src, 12);
            Res.numrecmask = BitConverter.ToUInt32(src, 14);
            Res.leftmask = BitConverter.ToUInt16(src, 18);
            Res.rightmask = BitConverter.ToUInt16(src, 20);
            Res.numrecbits = BitConverter.ToUInt16(src, 22);
            Res.leftbits = BitConverter.ToUInt16(src, 24);
            Res.rightbits = BitConverter.ToUInt16(src, 26);
            Res.recbytes = BitConverter.ToUInt16(src, 28);

            return Res;

        }
        public static objtab ByteArrayToObjtab(byte[] src)
        {
            objtab Res = new objtab(0, new UInt32[1023]);

            Res.numblocks = BitConverter.ToInt32(src, 0);
            Array.Copy(src, 4, Res.blocks, 0, Res.numblocks);

            return Res;
        }

        public static objtab838 ByteArrayToObjtab838(byte[] src)
        {
            objtab838 Res;

            Res.blocks = new UInt32[1023];
            Array.Clear(Res.blocks, 0, Res.blocks.Length);
            Array.Copy(src, 0, Res.blocks, 0, src.Length);

            return Res;
        }

        public static v8ob ByteArrayToV8ob(byte[] src)
        {
            // public char[] sig; // сигнатура SIG_OBJ
            // public UInt32 len; // длина файла
            // public _version version;
            // public UInt32[] blocks; // 1018

            v8ob Res;

            Res.sig = Encoding.UTF8.GetChars(src, 0, 8);
            Res.len = BitConverter.ToUInt32(src, 8);
            Res.version.version_1 = BitConverter.ToUInt32(src, 12);
            Res.version.version_2 = BitConverter.ToUInt32(src, 16);
            Res.version.version_3 = BitConverter.ToUInt32(src, 20);
            Res.blocks = new UInt32[1018];
            Array.Clear(Res.blocks, 0, Res.blocks.Length);
            Array.Copy(src, 24, Res.blocks, 0, src.Length - 24);

            return Res;
        }

        public static v838ob_data ByteArrayTov838ob(byte[] src)
        {
            // public char[] sig;       // сигнатура 0x1C 0xFD (1C File Data?)  sig[2];
            // public Int16 fatlevel;   // уровень таблицы размещения (0x0000 - в таблице blocks номера страниц с данными, 0x0001 - в таблице blocks номера страниц с таблицами размещения второго уровня, в которых уже, в свою очередь, находятся номера страниц с данными)
            // public _version version;
            // public UInt64 len;       // длина файла
            // public UInt32[] blocks;  // Реальная длина массива зависит от размера страницы и равна pagesize/4-6 (от это 1018 для 4К до 16378 для 64К)  blocks[1];

            v838ob_data Res;

            //Res.sig = Encoding.UTF8.GetChars(src, 0, 2);

            Res.sig = new byte[2];
            Array.Copy(src, 0, Res.sig, 0, 2);

            Res.fatlevel = BitConverter.ToInt16(src, 2);
            Res.version.version_1 = BitConverter.ToUInt32(src, 4);
            Res.version.version_2 = BitConverter.ToUInt32(src, 8);
            Res.version.version_3 = BitConverter.ToUInt32(src, 12);
            Res.len = BitConverter.ToUInt64(src, 16);
            //Res.blocks = new UInt32[16378];
            Res.blocks = new UInt32[1];
            Array.Clear(Res.blocks, 0, Res.blocks.Length);
            //Array.Copy(src, 24, Res.blocks, 0, src.Length - 20);
            Array.Copy(src, 24, Res.blocks, 0, 1);

            return Res;

        }

        public static v838ob_free ByteArrayTov838ob_free(byte[] src)
        {
            // public char[] sig;     // сигнатура 0x1C 0xFF (1C File Free?)
            // public Int16 fatlevel; // 0x0000 пока! но может ... уровень таблицы размещения (0x0000 - в таблице blocks номера страниц с данными, 0x0001 - в таблице blocks номера страниц с таблицами размещения второго уровня, в которых уже, в свою очередь, находятся номера страниц с данными)
            // public UInt32 version;        // ??? предположительно...
            // public UInt32[] blocks;       // Реальная длина массива зависит от размера страницы и равна pagesize/4-6 (от это 1018 для 4К до 16378 для 64К)

            v838ob_free Res;

            //Res.sig = Encoding.UTF8.GetChars(src, 0, 2);
            Res.sig = new byte[2];
            Array.Copy(src, 0, Res.sig, 0, 2);
            Res.fatlevel = BitConverter.ToInt16(src, 2);
            Res.version = BitConverter.ToUInt32(src, 4);
            //Res.blocks = new UInt32[16378];
            Res.blocks = new UInt32[1];
            Array.Clear(Res.blocks, 0, Res.blocks.Length);
            //Array.Copy(src, 8, Res.blocks, 0, src.Length - 4);
            Array.Copy(src, 8, Res.blocks, 0, 1);

            return Res;

        }


    }
}
