using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _1STool1CD
{
    public struct index_record
    {
        public Field field;
        public Int32 len;
    }

    /// <summary>
    /// структура одной записи распакованного индекса страницы-листа
    /// </summary>
    public struct unpack_index_record
    {
        UInt32 _record_number; // номер (индекс) записи в таблице записей
        char[] _index; // значение индекса записи. Реальная длина значения определяется полем length класса index
    }

    /// <summary>
    /// структура заголовка страницы-ветки индексов
    /// </summary>
    public struct branch_page_header
    {
        public UInt16 flags; // offset 0
        public UInt16 number_indexes; // offset 2
        public UInt16 prev_page; // offset 4 // для 8.3.8 - это номер страницы (реальное смещение = prev_page * pagesize), до 8.3.8 - это реальное смещение
        public UInt16 next_page; // offset 8 // для 8.3.8 - это номер страницы (реальное смещение = next_page * pagesize), до 8.3.8 - это реальное смещение
    }

    /// <summary>
    /// структура заголовка страницы-листа индексов
    /// </summary>
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

    /// <summary>
    /// Вспомогательная структура для упаковки индексов на странице-листе
    /// </summary>
    public struct _pack_index_record
    {
        public UInt32 numrec;
        public UInt32 left;
        public UInt32 right;
    }


    public class Index
    {
        // Значения битовых флагов в заголовке страницы индекса
        public Int16 indexpage_is_root = 1; // Установленный флаг означает, что страница является корневой
        public Int16 indexpage_is_leaf = 2; // Установленный флаг означает, что страница является листом, иначе веткой

        public Index(Table _base) { }

        public String getname() { return " "; }
        public bool get_is_primary() { return true; }
        public Int32 get_num_records() { return 0; } // получить количество полей в индексе
        public index_record get_records() { return new index_record(); }

        public UInt32 get_numrecords() { return 0; } // получает количество записей, проиндексированных индексом
        public UInt32 get_numrec(UInt32 num_record) { return 0; } // получает физический индекс записи по порядковому индексу

        public void dump(String filename) { }
        public void calcRecordIndex(byte[] rec, char[] indexBuf) { } // вычислить индекс записи rec и поместить в indexBuf. Длина буфера indexBuf должна быть не меньше length

        public UInt32 get_rootblock() { return 0; }
        public UInt32 get_length() { return 0; }

        // распаковывает одну страницу-лист индексов
        // возвращает массив структур unpack_index_record. Количество элементов массива возвращается в number_indexes
        public byte[] unpack_leafpage(UInt64 page_offset, UInt32 number_indexes) { return new byte[10]; }

        // распаковывает одну страницу-лист индексов
        // возвращает массив структур unpack_index_record. Количество элементов массива возвращается в number_indexes
        public byte[] unpack_leafpage(byte[] page, UInt32 number_indexes) { return new byte[10]; }

        // упаковывает одну страницу-лист индексов.
        // возвращвет истина, если упаковка произведена, и ложь, если упаковка невозможна.
        public bool pack_leafpage(byte[] unpack_index, UInt32 number_indexes, byte[] page_buf) { return true; }

        #region private
        private Table tbase;
        private Utils1CD.db_ver version; // версия базы
        private UInt32 pagesize; // размер одной страницы (до версии 8.2.14 всегда 0x1000 (4K), начиная с версии 8.3.8 от 0x1000 (4K) до 0x10000 (64K))

        private String name;
        private bool is_primary;
        private Int32 num_records; // количество полей в индексе
        private index_record records;

        private UInt64 start; // Смещение в файле индексов блока описания индекса
        private UInt64 rootblock; // Смещение в файле индексов корневого блока индекса
        private UInt32 length; // длина в байтах одной распакованной записи индекса


        private List<UInt32> recordsindex; // динамический массив индексов записей по номеру (только не пустые записи)
        private bool recordsindex_complete; // признак заполнености recordsindex
        private void create_recordsindex() { }

        private void dump_recursive(v8object file_index, FileStream f, Int32 level, UInt64 curblock) { }
        private void delete_index(byte[] rec, UInt32 phys_numrec) { } // удаление индекса записи из файла index
        private void delete_index_record(byte[] index_buf, UInt32 phys_numrec) { } // удаление одного индекса из файла index
        private void delete_index_record(byte[] index_buf, UInt32 phys_numrec, UInt64 block, bool is_last_record, bool page_is_empty, byte[] new_last_index_buf, UInt32 new_last_phys_num) { } // рекурсивное удаление одного индекса из блока файла index
        private void write_index(UInt32 phys_numrecord, byte[] rec) { } // запись индекса записи
        private void write_index_record(UInt32 phys_numrecord, byte[] index_buf) { } // запись индекса
        private void write_index_record(UInt32 phys_numrecord, byte[] index_buf, UInt64 block, Int32 result, byte[] new_last_index_buf, UInt32 new_last_phys_num, byte[] new_last_index_buf2, UInt32 new_last_phys_num2, UInt64 new_last_block2) { } // рекурсивная запись индекса

        #endregion
    }

}
