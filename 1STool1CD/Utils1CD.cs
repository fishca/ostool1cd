using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static _1STool1CD.Constants;
using static _1STool1CD.Structures;

namespace _1STool1CD
{
    public static class Utils1CD
    {
                

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
