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

            LastDataGet = 0;

            if (Count >= MaxCount)
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

            IsChanged = for_write;
            File = fs;

            // регистрируем себя в массиве блоков
            Memblocks[Numblock] = this;
        }

        public static void Garbage()
        {

        }

        public static byte[] GetBlock(FileStream fs, UInt32 _numblock)
        {
            if (_numblock >= NumBlocks)
                return null;
            if (Memblocks[_numblock] != null)
            {
                V8MemBlock tmpV8Mem = new V8MemBlock(fs, _numblock, false, true);
            }
            return
                Memblocks[_numblock].GetBlock(false);
        }

        public static byte[] GetBlockForWrite(FileStream fs, UInt32 _numblock, bool read)
        {
            if (_numblock > NumBlocks)
                return null;
            if (_numblock == NumBlocks)
                AddBlock();
            if (Memblocks[_numblock] != null)
            {
                V8MemBlock tmpV8Mem = new V8MemBlock(fs, _numblock, true, read);
            }
            else
                Memblocks[_numblock].IsChanged = true;

            return
                Memblocks[_numblock].GetBlock(true);
        }

        public static void CreateMemblocks(UInt32 _numblocks)
        {
            NumBlocks = _numblocks;
            ArrayNumBlocks = (NumBlocks / Delta + 1) * Delta;
            Memblocks = new V8MemBlock[ArrayNumBlocks];
            //memset(memblocks, 0, array_numblocks * sizeof(MemBlock*));
        }

        public static void DeleteMemblocks()
        {
            while (First != null)
                First = null;

            Memblocks = null;

            NumBlocks = 0;

            ArrayNumBlocks = 0;
        }

        public static UInt64 GetNumBlocks()
        {
            return NumBlocks;
        }

        public static void Flush()
        {
            V8MemBlock cur;
            for (cur = First; cur != null; cur = cur.Next)
                if (cur.IsChanged)
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
        private static V8MemBlock[] memblocks;


        private UInt32 lastdataget; // время (Windows time, в миллисекундах) последнего обращения к данным объекта (data)

        public static uint Count { get { return count; } set { count = value; } }

        public FileStream File { get { return file; } set { file = value; } }

        public static uint Pagesize { get { return pagesize; } set { pagesize = value; } }

        public uint Numblock { get { return numblock; } set { numblock = value; } }

        public V8MemBlock Next { get { return next; } set { next = value; } }
        public V8MemBlock Prev { get { return prev; } set { prev = value; } }

        public bool IsChanged { get { return is_changed; } set { is_changed = value; } }

        public static V8MemBlock First { get { return first; } set { first = value; } }
        public static V8MemBlock Last { get { return last; } set { last = value; } }
        public static uint MaxCount { get { return maxcount; } set { maxcount = value; } }
        public static uint NumBlocks { get { return numblocks; } set { numblocks = value; } }
        public static uint ArrayNumBlocks { get { return array_numblocks; } set { array_numblocks = value; } }
        public static uint Delta { get { return delta; } set { delta = value; } }
        public uint LastDataGet { get { return lastdataget; } set { lastdataget = value; } }

        public static V8MemBlock[] Memblocks { get { return memblocks; } set { memblocks = value; } }

        //char* getblock(bool for_write); // получить блок для чтения или для записи
        public byte[] GetBlock(bool for_write) // получить блок для чтения или для записи
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

            if (for_write) IsChanged = true;

            return buf;

            //return new byte[0x1000];
        }

        public static void AddBlock()
        {

            if (NumBlocks < ArrayNumBlocks)
                Memblocks[NumBlocks++] = null;
            else
            {
                V8MemBlock[] mb = new V8MemBlock[ArrayNumBlocks + Delta];
                for (uint i = 0; i < ArrayNumBlocks; i++)
                    mb[i] = Memblocks[i];

                for (uint i = ArrayNumBlocks; i < ArrayNumBlocks + Delta; i++)
                    mb[i] = null;

                ArrayNumBlocks += Delta;

                Memblocks = null;

                Memblocks = mb;
            }

        }

        public void Write()
        {
            if (!IsChanged)
                return;

            File.Seek(Numblock * Pagesize, SeekOrigin.Begin);
            File.Write(buf, 0, (int)Pagesize);
            IsChanged = false;
        }

    }
}
