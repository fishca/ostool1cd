﻿using System;
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
        public static UInt32 count; // текущее количество кешированных блоков

        public V8MemBlock(FileStream fs, UInt32 _numblock, bool for_write, bool read)
        {
            numblock = _numblock;

            lastdataget = 0;

            if (count >= maxcount)
                first = null;  // если количество кешированных блоков превышает максимальное, удаляем последний, к которому было обращение

            count++;

            prev = last;

            next = null;

            if (last != null)
                last.next = this;
            else
                first = this;

            last = this;

            buf = new byte[pagesize];

            if (for_write)
            {
                UInt32 fnumblocks = (UInt32)fs.Length / pagesize;
                if (fnumblocks <= numblock)
                {
                    Array.Clear(buf, 0, (int)pagesize);
                    fs.Seek(numblock * pagesize, SeekOrigin.Begin);
                    fs.Write(buf, 0, (int)pagesize);
                    fs.Seek(12, SeekOrigin.Begin);
                    //fs->WriteBuffer(&numblock, 4);
                    BinaryReader br = new BinaryReader(fs);
                    numblock = br.ReadUInt32();
                }
                else
                {
                    if (read)
                    {
                        fs.Seek(numblock * pagesize, SeekOrigin.Begin);
                        fs.Read(buf, 0, (int)pagesize);
                    }
                    else
                    {
                        Array.Clear(buf, 0, (int)pagesize);
                    }
                }

            }
            else
            {
                fs.Seek(numblock * pagesize, SeekOrigin.Begin);
                fs.Read(buf, 0, (int)pagesize);
            }

            is_changed = for_write;
            file = fs;

            // регистрируем себя в массиве блоков
            memblocks[numblock] = this;
        }

        public static void Garbage()
        {

        }

        public static byte[] Getblock(FileStream fs, UInt32 _numblock)
        {
            if (_numblock >= numblocks)
                return null;
            if (memblocks[_numblock] != null)
                new V8MemBlock(fs, _numblock, false, true);
            return
                memblocks[_numblock].Getblock(false);
        }

        public static byte[] Getblock_for_write(FileStream fs, UInt32 _numblock, bool read)
        {
            if (_numblock > numblocks)
                return null;
            if (_numblock == numblocks)
                Add_block();
            if (memblocks[_numblock] != null)
                new V8MemBlock(fs, _numblock, true, read);
            else
                memblocks[_numblock].is_changed = true;
            return
                memblocks[_numblock].Getblock(true);
        }

        public static void Create_memblocks(UInt32 _numblocks)
        {
            numblocks = _numblocks;
            array_numblocks = (numblocks / delta + 1) * delta;
            memblocks = new V8MemBlock[array_numblocks];
            //memset(memblocks, 0, array_numblocks * sizeof(MemBlock*));
        }

        public static void Delete_memblocks()
        {
            while (first != null)
                first = null;

            memblocks = null;

            numblocks = 0;

            array_numblocks = 0;
        }

        public static UInt64 Get_numblocks()
        {
            return numblocks;
        }

        public static void Flush()
        {
            V8MemBlock cur;
            for (cur = first; cur != null; cur = cur.next)
                if (cur.is_changed)
                    cur.Write();
        }


        //char* buf; // указатель на блок в памяти
        byte[] buf;

        public FileStream file; // файл, которому принадлежит блок

        public static UInt32 pagesize = (UInt32)PAGE4K; // размер одной страницы (до версии 8.2.14 всегда 0x1000 (4K), начиная с версии 8.3.8 от 0x1000 (4K) до 0x10000 (64K))

        public UInt32 numblock;

        public V8MemBlock next;
        public V8MemBlock prev;

        public bool is_changed; // признак, что блок изменен (требует записи)

        public static V8MemBlock first;
        public static V8MemBlock last;

        public static UInt32 maxcount;    // максимальное количество кешированных блоков
        public static UInt32 numblocks;   // количество значащих элементов в массиве memblocks (равно количеству блоков в файле *.1CD)

        public static UInt32 array_numblocks;  // количество элементов в массиве memblocks (больше или равно количеству блоков в файле *.1CD)
        public static UInt32 delta = 128;      // шаг увеличения массива memblocks

        //static MemBlock** memblocks; // указатель на массив указателей MemBlock (количество равно количеству блоков в файле *.1CD)
        //public List<v8MemBlock> memblocks;
        public static V8MemBlock[] memblocks;


        public UInt32 lastdataget; // время (Windows time, в миллисекундах) последнего обращения к данным объекта (data)

        //char* getblock(bool for_write); // получить блок для чтения или для записи
        public byte[] Getblock(bool for_write) // получить блок для чтения или для записи
        {
            // удаляем себя из текущего положения цепочки...
            if (prev != null)
                prev.next = next;
            else
                first = next;

            if (next != null)
                next.prev = prev;
            else
                last = prev;

            // ... и записываем себя в конец цепочки
            prev = last;

            next = null;
            if (last != null)
                last.next = this;
            else
                first = this;

            last = this;

            if (for_write) is_changed = true;

            return buf;

            //return new byte[0x1000];
        }

        public static void Add_block()
        {

            if (numblocks < array_numblocks)
                memblocks[numblocks++] = null;
            else
            {
                V8MemBlock[] mb = new V8MemBlock[array_numblocks + delta];
                for (uint i = 0; i < array_numblocks; i++)
                    mb[i] = memblocks[i];

                for (uint i = array_numblocks; i < array_numblocks + delta; i++)
                    mb[i] = null;

                array_numblocks += delta;

                memblocks = null;

                memblocks = mb;
            }

        }

        public void Write()
        {
            if (!is_changed)
                return;

            file.Seek(numblock * pagesize, SeekOrigin.Begin);
            file.Write(buf, 0, (int)pagesize);
            is_changed = false;
        }

    }
}
