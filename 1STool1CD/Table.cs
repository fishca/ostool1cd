using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace _1STool1CD
{
    public enum table_info
    {
        ti_description,
        ti_fields,
        ti_indexes,
        ti_physical_view,
        ti_logical_view
    }

    // типы измененных записей
    public enum changed_rec_type
    {
        not_changed,
	    changed,
	    inserted,
	    deleted
    }


    // структура одного блока в файле file_blob
    public struct blob_block
    {
        public UInt32 nextblock;
        public Int16 length;
        //public char[] data[BLOB_RECORD_DATA_LEN];
        public char[] data;
    };

    // структура root файла экспорта/импорта таблиц
    struct export_import_table_root
    {
        public bool has_data;
        public bool has_blob;
        public bool has_index;
        public bool has_descr;
        public Int32 data_version_1; // версия реструктуризации
        public Int32 data_version_2; // версия изменения
        public Int32 blob_version_1; // версия реструктуризации
        public Int32 blob_version_2; // версия изменения
        public Int32 index_version_1; // версия реструктуризации
        public Int32 index_version_2; // версия изменения
        public Int32 descr_version_1; // версия реструктуризации
        public Int32 descr_version_2; // версия изменения
    };



    public class Table
    {
        public static readonly UInt32 BLOB_RECORD_LEN = 256;
        public static readonly UInt32 BLOB_RECORD_DATA_LEN = 250;

        // структура изменной записи таблицы
        public class changed_rec
        {

            // владелец
            public Table parent;

            // физический номер записи (для добавленных записей нумерация начинается с phys_numrecords)
            public UInt32 numrec;

            // тип изменения записи (изменена, добавлена, удалена)
            public changed_rec_type changed_type;

            // следующая измененная запись в списке измененных записей
            public changed_rec next;

            // массив признаков изменения поля (по одному байту на каждое поле, всего num_fields байт)
            public char[] fields;

            // измененная запись. Для типов полей tf_text (TEXT), tf_string (MEMO) и tf_image (BLOB), если соответствующий признак в fields установлен,
            // содержит указатель на TStream с содержимым поля (или NULL)
            public char[] rec;

            public changed_rec(Table _parent, changed_rec_type crt, UInt32 phys_numrecord) { }

            public void clear() { }
        }

        //--> поддержка динамического построения таблицы записей
        public UInt32[] recordsindex; // массив индексов записей по номеру (только не пустые записи)
        public bool recordsindex_complete; // признак заполнености recordsindex
        public UInt32 numrecords_review; // количество просмотренных записей всего в поиске не пустых
        public UInt32 numrecords_found; // количество найденных непустых записей (текущий размер recordsindex)
                                        //<-- поддержка динамического построения таблицы записей
        public void fillrecordsindex() { } // заполнить recordsindex не динамически

        /// <summary>
        /// Конструктора
        /// </summary>
        public Table() { }
        public Table(T_1CD _base, Int32 block_descr) { }
        public Table(T_1CD _base, String _descr, Int32 block_descr = 0) { }

        public void init() { }
        public void init(Int32 block_descr) { }

        public String getname() { return " "; }
        public String getdescription() { return " "; }
        public Int32 get_numfields() { return 0; }
        public Int32 get_numindexes() { return 0; }
        public Field getfield(Int32 numfield) { return null; }
        public Index getindex(Int32 numindex) { return null; }
        public bool get_issystem() { return true; }
        public Int32 get_recordlen() { return 0; }
        public bool get_recordlock() { return true; }

        public UInt32 get_phys_numrecords() { return 0; } // возвращает количество записей в таблице всего, вместе с удаленными
        public UInt32 get_log_numrecords() { return 0; } // возвращает количество записей в таблице всего, без удаленных
        public void set_log_numrecords(UInt32 _log_numrecords) { } //
        public UInt32 get_added_numrecords() { return 0; }

        public char[] getrecord(UInt32 phys_numrecord, char[] buf) { return null; } // возвращает указатель на запись, буфер принадлежит вызывающей процедуре
        public Stream readBlob(Stream _str, UInt32 _startblock, UInt32 _length, bool rewrite = true) { return null; }
        public UInt32 readBlob(byte[] _buf, UInt32 _startblock, UInt32 _length) { return 0; }
        public void set_lockinmemory(bool _lock) { }
        public bool export_to_xml(String filename, bool blob_to_file, bool unpack) { return true; }

        public v8object get_file_data() { return null; }
        public v8object get_file_blob() { return null; }
        public v8object get_file_index() { return null; }

        public UInt64 get_fileoffset(UInt32 phys_numrecord) { return 0; } // получить физическое смещение в файле записи по номеру

        public char[] get_edit_record(UInt32 phys_numrecord, char[] buf) { return null; } // возвращает указатель на запись, буфер принадлежит вызывающей процедуре
        public bool get_edit() { return true; }

        public UInt32 get_phys_numrec(Int32 ARow, Index cur_index) { return 0; } // получить физический индекс записи по номеру строки по указанному индексу
        public String get_file_name_for_field(Int32 num_field, char[] rec, UInt32 numrec = 0) { return " "; } // получить имя файла по-умолчанию конкретного поля конкретной записи
        public String get_file_name_for_record(char[] rec) { return " "; } // получить имя файла по-умолчанию конкретной записи
        public T_1CD getbase() { return base_; }

        public void begin_edit() { } // переводит таблицу в режим редактирования
        public void cancel_edit() { } // переводит таблицу в режим просмотра и отменяет все изменения
        public void end_edit() { } // переводит таблицу в режим просмотра и сохраняет все изменения
        public changed_rec_type get_rec_type(UInt32 phys_numrecord) { return changed_rec_type.not_changed; }
        public changed_rec_type get_rec_type(UInt32 phys_numrecord, Int32 numfield) { return changed_rec_type.not_changed; }
        public void set_edit_value(UInt32 phys_numrecord, Int32 numfield, bool Null, String value, Stream st = null) { }
        public void restore_edit_value(UInt32 phys_numrecord, Int32 numfield) { }
        public void set_rec_type(UInt32 phys_numrecord, changed_rec_type crt) { }

        public void export_table(String path) { }
        public void import_table(String path) { }

        public void delete_record(UInt32 phys_numrecord) { } // удаление записи
        public void insert_record(char[] rec) { } // добавление записи
        public void update_record(UInt32 phys_numrecord, char[] rec, char[] changed_fields) { } // изменение записи
        public char[] get_record_template_test() { return null; }

        public Field get_field(String fieldname) { return null; }
        public Index get_index(String indexname) { return null; }


        #region private
        public T_1CD base_;

        private v8object descr_table; // объект с описанием структуры таблицы (только для версий с 8.0 до 8.2.14)
        private String description;
        private String name;
        private Int32 num_fields;
        private List<Field> fields;


        private Int32 num_indexes;
        private Index[] indexes;
        private bool recordlock;
        private v8object file_data;
        private v8object file_blob;
        public v8object file_index;
        private Int32 recordlen; // длина записи (в байтах)
        private bool issystem; // Признак системной таблицы (имя таблицы не начинается с подчеркивания)
        private Int32 lockinmemory; // счетчик блокировок в памяти

        private void deletefields() { }
        private void deleteindexes() { }

        private changed_rec ch_rec; // первая измененная запись в списке измененных записей
        private UInt32 added_numrecords; // количество добавленных записей в режиме редактирования

        private UInt32 phys_numrecords; // физическое количество записей (вместе с удаленными)
        public UInt32 log_numrecords; // логическое количество записей (только не удаленные)

        private void create_file_data() { } // создание файла file_data
        private void create_file_blob() { } // создание файла file_blob
        private void create_file_index() { } // создание файла file_index
        private void refresh_descr_table() { } // создание и запись файла описания таблицы

        private bool edit; // признак, что таблица находится в режиме редактирования

        private void delete_data_record(UInt32 phys_numrecord) { } // удаление записи из файла data
        private void delete_blob_record(UInt32 blob_numrecord) { } // удаление записи из файла blob
        private void delete_index_record(UInt32 phys_numrecord) { } // удаление всех индексов записи из файла index
        private void delete_index_record(UInt32 phys_numrecord, char[] rec) { } // удаление всех индексов записи из файла index
        private void write_data_record(UInt32 phys_numrecord, char[] rec) { } // запись одной записи в файл data
        private UInt32 write_blob_record(char[] blob_record, UInt32 blob_len) { return 0; } // записывает НОВУЮ запись в файл blob, возвращает индекс новой записи
        private UInt32 write_blob_record(Stream bstr) { return 0; } //  // записывает НОВУЮ запись в файл blob, возвращает индекс новой записи
        private void write_index_record(UInt32 phys_numrecord, char[] rec){} // запись индексов записи в файл index

        private bool bad; // признак битой таблицы

        #endregion

    }
}
