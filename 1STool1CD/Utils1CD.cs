using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _1STool1CD
{
    public static class Utils1CD
    {
        public struct IndexRecord
        {
            private V8Field field;
            private Int32 len;

            public V8Field Field { get { return field; } set { field = value; } }

            public int Len { get { return len; } set { len = value; } }
        }

        public struct UnpackIndexRecord
        {
            UInt32 _record_number; // номер (индекс) записи в таблице записей
                                   //unsigned char _index[1]; // значение индекса записи. Реальная длина значения определяется полем length класса index
            private byte[] index;

            public byte[] Index { get { return index; } set { index = value; } }
        }
        
        /// <summary>
        /// Версии формата базы 1С
        /// </summary>
        public enum DBVer
        {
            ver8_0_3_0  = 1,
        	ver8_0_5_0  = 2,
	        ver8_1_0_0  = 3,
	        ver8_2_0_0  = 4,
	        ver8_2_14_0 = 5,
	        ver8_3_8_0  = 6
        }

        public enum NodeType
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

        /// <summary>
        /// Заголовок страницы
        /// </summary>
        public struct LeafPageHeader
        {
            private Int16 flags; // offset 0
            private UInt16 number_indexes; // offset 2
            private UInt32 prev_page; // offset 4 // для 8.3.8 - это номер страницы (реальное смещение = prev_page * pagesize), до 8.3.8 - это реальное смещение
            private UInt32 next_page; // offset 8 // для 8.3.8 - это номер страницы (реальное смещение = next_page * pagesize), до 8.3.8 - это реальное смещение
            private UInt16 freebytes; // offset 12
            private UInt32 numrecmask; // offset 14
            private UInt16 leftmask; // offset 18
            private UInt16 rightmask; // offset 20
            private UInt16 numrecbits; // offset 22
            private UInt16 leftbits; // offset 24
            private UInt16 rightbits; // offset 26
            private UInt16 recbytes; // offset 28

            public short Flags { get { return flags; } set { flags = value; } }

            public ushort Number_indexes { get { return number_indexes; } set { number_indexes = value; } }

            public uint Prev_page    { get { return prev_page;  } set { prev_page = value;  }  }
            public uint Next_page    { get { return next_page;  } set { next_page = value;  }  }
            public ushort Freebytes  { get { return freebytes;  } set { freebytes = value;  }  }
            public uint Numrecmask   { get { return numrecmask; } set { numrecmask = value; }  }
            public ushort Leftmask   { get { return leftmask;   } set { leftmask = value;   }  }
            public ushort Rightmask  { get { return rightmask;  } set { rightmask = value;  }  }
            public ushort Numrecbits { get { return numrecbits; } set { numrecbits = value; }  }
            public ushort Leftbits   { get { return leftbits;   } set { leftbits = value;   }  }
            public ushort Rightbits  { get { return rightbits;  } set { rightbits = value;  }  }
            public ushort Recbytes   { get { return recbytes;   } set { recbytes = value;   }  }
        }


        public static LeafPageHeader ByteArrayToLeafPageHeader(byte[] src)
        {

            LeafPageHeader Res = new LeafPageHeader();

            Res.Flags = BitConverter.ToInt16(src, 0);
            Res.Number_indexes = BitConverter.ToUInt16(src, 2);
            Res.Prev_page = BitConverter.ToUInt32(src, 4);
            Res.Next_page = BitConverter.ToUInt32(src, 8);
            Res.Freebytes = BitConverter.ToUInt16(src, 12);
            Res.Numrecmask = BitConverter.ToUInt32(src, 14);
            Res.Leftmask = BitConverter.ToUInt16(src, 18);
            Res.Rightmask = BitConverter.ToUInt16(src, 20);
            Res.Numrecbits = BitConverter.ToUInt16(src, 22);
            Res.Leftbits = BitConverter.ToUInt16(src, 24);
            Res.Rightbits = BitConverter.ToUInt16(src, 26);
            Res.Recbytes = BitConverter.ToUInt16(src, 28);

            return Res;

        }

        public static ObjTab ByteArrayToObjtab(byte[] src)
        {
            ObjTab Res = new ObjTab(0, new UInt32[1023]);

            Res.Numblocks = BitConverter.ToInt32(src, 0);
            Array.Copy(src, 4, Res.Blocks, 0, Res.Numblocks);

            return Res;
        }

        public static Objtab838 ByteArrayToObjtab838(byte[] src)
        {
            Objtab838 Res = new Objtab838();

            Res.Blocks = new UInt32[1023];
            Array.Clear(Res.Blocks, 0, Res.Blocks.Length);
            Array.Copy(src, 0, Res.Blocks, 0, src.Length);

            return Res;
        }

        public static V8ob ByteArrayToV8ob(byte[] src)
        {
            // public char[] sig; // сигнатура SIG_OBJ
            // public UInt32 len; // длина файла
            // public _version version;
            // public UInt32[] blocks; // 1018

            //V8ob Res = new V8ob();
            V8ob Res = new V8ob();

            Res.Sig = Encoding.UTF8.GetChars(src, 0, 8);
            Res.Len = BitConverter.ToUInt32(src, 8);
            //Res.Version.Version_1 = BitConverter.ToUInt32(src, 12);

            _version VV = new _version(0, 0, 0);
            VV.Version_1 = BitConverter.ToUInt32(src, 12);
            VV.Version_2 = BitConverter.ToUInt32(src, 16);
            VV.Version_3 = BitConverter.ToUInt32(src, 20);


            Res.Version = VV;
            //Res.Version.Version_1 = 1;
            //Res.Version.Version_2 = BitConverter.ToUInt32(src, 16);
            //Res.Version.Version_3 = BitConverter.ToUInt32(src, 20);
            Res.Blocks = new UInt32[1018];
            Array.Clear(Res.Blocks, 0, Res.Blocks.Length);
            Array.Copy(src, 24, Res.Blocks, 0, src.Length - 24);

            return Res;
        }

        public static V838ob_data ByteArrayTov838ob(byte[] src)
        {
            // public char[] sig;       // сигнатура 0x1C 0xFD (1C File Data?)  sig[2];
            // public Int16 fatlevel;   // уровень таблицы размещения (0x0000 - в таблице blocks номера страниц с данными, 0x0001 - в таблице blocks номера страниц с таблицами размещения второго уровня, в которых уже, в свою очередь, находятся номера страниц с данными)
            // public _version version;
            // public UInt64 len;       // длина файла
            // public UInt32[] blocks;  // Реальная длина массива зависит от размера страницы и равна pagesize/4-6 (от это 1018 для 4К до 16378 для 64К)  blocks[1];

            V838ob_data Res = new V838ob_data();

            //Res.sig = Encoding.UTF8.GetChars(src, 0, 2);

            Res.Sig = new byte[2];
            Array.Copy(src, 0, Res.Sig, 0, 2);

            Res.Fatlevel = BitConverter.ToInt16(src, 2);

            _version VV = new _version(0, 0, 0);


            VV.Version_1 = BitConverter.ToUInt32(src, 4);
            VV.Version_2 = BitConverter.ToUInt32(src, 8);
            VV.Version_3 = BitConverter.ToUInt32(src, 12);

            Res.Version = VV;

            Res.Len = BitConverter.ToUInt64(src, 16);
            //Res.blocks = new UInt32[16378];
            Res.Blocks = new UInt32[1];
            Array.Clear(Res.Blocks, 0, Res.Blocks.Length);
            //Array.Copy(src, 24, Res.blocks, 0, src.Length - 20);
            Array.Copy(src, 24, Res.Blocks, 0, 1);

            return Res;

        }

        public static V838ob_free ByteArrayTov838ob_free(byte[] src)
        {
            // public char[] sig;     // сигнатура 0x1C 0xFF (1C File Free?)
            // public Int16 fatlevel; // 0x0000 пока! но может ... уровень таблицы размещения (0x0000 - в таблице blocks номера страниц с данными, 0x0001 - в таблице blocks номера страниц с таблицами размещения второго уровня, в которых уже, в свою очередь, находятся номера страниц с данными)
            // public UInt32 version;        // ??? предположительно...
            // public UInt32[] blocks;       // Реальная длина массива зависит от размера страницы и равна pagesize/4-6 (от это 1018 для 4К до 16378 для 64К)

            V838ob_free Res = new V838ob_free();

            //Res.sig = Encoding.UTF8.GetChars(src, 0, 2);
            Res.Sig = new byte[2];
            Array.Copy(src, 0, Res.Sig, 0, 2);
            Res.Fatlevel = BitConverter.ToInt16(src, 2);
            Res.Version = BitConverter.ToUInt32(src, 4);
            //Res.blocks = new UInt32[16378];
            Res.Blocks = new UInt32[1];
            Array.Clear(Res.Blocks, 0, Res.Blocks.Length);
            //Array.Copy(src, 8, Res.blocks, 0, src.Length - 4);
            Array.Copy(src, 8, Res.Blocks, 0, 1);

            return Res;

        }


    }
}
