using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _1STool1CD
{
    /// <summary>
    /// класс кешированного блока в памяти
    ///  первый в цепочке кешированный блок - тот, к которому наиболее давно обращались
    ///  последний в цепочке - с самым последним обращением
    /// </summary>
    public class MemBlock
    {
        public static UInt32 count; // текущее количество кешированных блоков

        public static void garbage() { }
        public static byte[] getblock(FileStream fs, UInt32 _numblock) { return new byte[100]; }
        public static byte[] getblock_for_write(FileStream fs, UInt32 _numblock, bool read) { return new byte[100]; }
        public static void create_memblocks(UInt64 _numblocks) { }

        public static void delete_memblocks() { }
        public static UInt64 get_numblocks() { return 0; }
        public static void flush() { }

        public MemBlock(FileStream fs, UInt32 _numblock, bool for_write, bool read) { }

        private byte[] buf; // указатель на блок в памяти
        private static UInt32 pagesize; // размер одной страницы (до версии 8.2.14 всегда 0x1000 (4K), начиная с версии 8.3.8 от 0x1000 (4K) до 0x10000 (64K))
        private UInt32 numblock;
        private MemBlock next;
        private MemBlock prev;
        private FileStream file; // файл, которому принадлежит блок
        private bool is_changed; // признак, что блок изменен (требует записи)

        private static MemBlock first;
        private static MemBlock last;
        private static UInt32 maxcount; // максимальное количество кешированных блоков
        private static UInt64 numblocks;   // количество значащих элементов в массиве memblocks (равно количеству блоков в файле *.1CD)

        private static UInt64 array_numblocks;   // количество элементов в массиве memblocks (больше или равно количеству блоков в файле *.1CD)
        private static UInt32 delta; // шаг увеличения массива memblocks
        private static MemBlock memblocks; // указатель на массив указателей MemBlock (количество равно количеству блоков в файле *.1CD)

        private UInt32 lastdataget; // время (Windows time, в миллисекундах) последнего обращения к данным объекта (data)
        private byte[] getblock(bool for_write) { return new byte[100]; } // получить блок для чтения или для записи


        private static void add_block() { }
        private void write() { }


    }
}
