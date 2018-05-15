using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static _1STool1CD.Constants;

namespace _1STool1CD
{
    /// <summary>
    /// класс кешированного блока в памяти
    ///  первый в цепочке кешированный блок - тот, к которому наиболее давно обращались
    ///  последний в цепочке - с самым последним обращением
    /// </summary>
    public class V8MemBlock
    {
        private static UInt32 count; // текущее количество кешированных блоков

        public V8MemBlock(FileStream fs, UInt32 _numblock, bool for_write, bool read)
        {
            Numblock = _numblock;

            Lastdataget = 0;

            if (Count >= Maxcount)
                First = null;  // если количество кешированных блоков превышает максимальное, удаляем последний, к которому было обращение

            Count++;

            Prev = Last;

            Next = null;

            if (Last != null)
                Last.Next = this;
            else
                First = this;

            Last = this;

            buf = new byte[Pagesize];

            if (for_write)
            {
                UInt32 fnumblocks = (UInt32)fs.Length / Pagesize;
                if (fnumblocks <= Numblock)
                {
                    Array.Clear(buf, 0, (int)Pagesize);
                    fs.Seek(Numblock * Pagesize, SeekOrigin.Begin);
                    fs.Write(buf, 0, (int)Pagesize);
                    fs.Seek(12, SeekOrigin.Begin);
                    //fs->WriteBuffer(&numblock, 4);
                    BinaryReader br = new BinaryReader(fs);
                    Numblock = br.ReadUInt32();
                }
                else
                {
                    if (read)
                    {
                        fs.Seek(Numblock * Pagesize, SeekOrigin.Begin);
                        fs.Read(buf, 0, (int)Pagesize);
                    }
                    else
                    {
                        Array.Clear(buf, 0, (int)Pagesize);
                    }
                }

            }
            else
            {
                fs.Seek(Numblock * Pagesize, SeekOrigin.Begin);
                fs.Read(buf, 0, (int)Pagesize);
            }

            Is_changed = for_write;
            File = fs;

            // регистрируем себя в массиве блоков
            memblocks[Numblock] = this;
        }

        public static void Garbage()
        {

        }

        public static byte[] Getblock(FileStream fs, UInt32 _numblock)
        {
            if (_numblock >= Numblocks)
                return null;
            if (memblocks[_numblock] != null)
            {
                V8MemBlock tmpV8Mem = new V8MemBlock(fs, _numblock, false, true);
            }
            return
                memblocks[_numblock].Getblock(false);
        }

        public static byte[] Getblock_for_write(FileStream fs, UInt32 _numblock, bool read)
        {
            if (_numblock > Numblocks)
                return null;
            if (_numblock == Numblocks)
                Add_block();
            if (memblocks[_numblock] != null)
            {
                V8MemBlock tmpV8Mem = new V8MemBlock(fs, _numblock, true, read);
            }
            else
                memblocks[_numblock].Is_changed = true;

            return
                memblocks[_numblock].Getblock(true);
        }

        public static void Create_memblocks(UInt32 _numblocks)
        {
            Numblocks = _numblocks;
            Array_numblocks = (Numblocks / Delta + 1) * Delta;
            memblocks = new V8MemBlock[Array_numblocks];
            //memset(memblocks, 0, array_numblocks * sizeof(MemBlock*));
        }

        public static void Delete_memblocks()
        {
            while (First != null)
                First = null;

            memblocks = null;

            Numblocks = 0;

            Array_numblocks = 0;
        }

        public static UInt64 Get_numblocks()
        {
            return Numblocks;
        }

        public static void Flush()
        {
            V8MemBlock cur;
            for (cur = First; cur != null; cur = cur.Next)
                if (cur.Is_changed)
                    cur.Write();
        }


        //char* buf; // указатель на блок в памяти
        byte[] buf;

        private FileStream file; // файл, которому принадлежит блок

        private static UInt32 pagesize = (UInt32)PAGE4K; // размер одной страницы (до версии 8.2.14 всегда 0x1000 (4K), начиная с версии 8.3.8 от 0x1000 (4K) до 0x10000 (64K))

        private UInt32 numblock;

        private V8MemBlock next;
        private V8MemBlock prev;

        private bool is_changed; // признак, что блок изменен (требует записи)

        private static V8MemBlock first;
        private static V8MemBlock last;

        private static UInt32 maxcount;    // максимальное количество кешированных блоков
        private static UInt32 numblocks;   // количество значащих элементов в массиве memblocks (равно количеству блоков в файле *.1CD)

        private static UInt32 array_numblocks;  // количество элементов в массиве memblocks (больше или равно количеству блоков в файле *.1CD)
        private static UInt32 delta = 128;      // шаг увеличения массива memblocks

        //static MemBlock** memblocks; // указатель на массив указателей MemBlock (количество равно количеству блоков в файле *.1CD)
        //public List<v8MemBlock> memblocks;
        public static V8MemBlock[] memblocks;


        private UInt32 lastdataget; // время (Windows time, в миллисекундах) последнего обращения к данным объекта (data)

        public static uint Count { get { return count; } set { count = value; } }

        public FileStream File { get { return file; } set { file = value; } }

        public static uint Pagesize { get { return pagesize; } set { pagesize = value; } }

        public uint Numblock { get { return numblock; } set { numblock = value; } }

        public V8MemBlock Next { get { return next; } set { next = value; } }
        public V8MemBlock Prev { get { return prev; } set { prev = value; } }

        public bool Is_changed { get { return is_changed; } set { is_changed = value; } }

        public static V8MemBlock First { get { return first; } set { first = value; } }
        public static V8MemBlock Last { get { return last; } set { last = value; } }
        public static uint Maxcount { get { return maxcount; } set { maxcount = value; } }
        public static uint Numblocks { get { return numblocks; } set { numblocks = value; } }
        public static uint Array_numblocks { get { return array_numblocks; } set { array_numblocks = value; } }
        public static uint Delta { get { return delta; } set { delta = value; } }
        public uint Lastdataget { get { return lastdataget; } set { lastdataget = value; } }

        //char* getblock(bool for_write); // получить блок для чтения или для записи
        public byte[] Getblock(bool for_write) // получить блок для чтения или для записи
        {
            // удаляем себя из текущего положения цепочки...
            if (Prev != null)
                Prev.Next = Next;
            else
                First = Next;

            if (Next != null)
                Next.Prev = Prev;
            else
                Last = Prev;

            // ... и записываем себя в конец цепочки
            Prev = Last;

            Next = null;
            if (Last != null)
                Last.Next = this;
            else
                First = this;

            Last = this;

            if (for_write) Is_changed = true;

            return buf;

            //return new byte[0x1000];
        }

        public static void Add_block()
        {

            if (Numblocks < Array_numblocks)
                memblocks[Numblocks++] = null;
            else
            {
                V8MemBlock[] mb = new V8MemBlock[Array_numblocks + Delta];
                for (uint i = 0; i < Array_numblocks; i++)
                    mb[i] = memblocks[i];

                for (uint i = Array_numblocks; i < Array_numblocks + Delta; i++)
                    mb[i] = null;

                Array_numblocks += Delta;

                memblocks = null;

                memblocks = mb;
            }

        }

        public void Write()
        {
            if (!Is_changed)
                return;

            File.Seek(Numblock * Pagesize, SeekOrigin.Begin);
            File.Write(buf, 0, (int)Pagesize);
            Is_changed = false;
        }

    }
}
