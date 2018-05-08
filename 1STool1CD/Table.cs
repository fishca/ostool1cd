using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using static _1STool1CD.Constants;
using static _1STool1CD.Utils1CD;

namespace _1STool1CD
{

    public enum type_fields
    {
        tf_binary,   // B // длина = length
        tf_bool,     // L // длина = 1
        tf_numeric,  // N // длина = (length + 2) / 2
        tf_char,     // NC // длина = length * 2
        tf_varchar,  // NVC // длина = length * 2 + 2
        tf_version,  // RV // 16, 8 версия создания и 8 версия модификации ? каждая версия int32_t(изменения) + int32_t(реструктуризация)
        tf_string,   // NT // 8 (unicode text)
        tf_text,     // T // 8 (ascii text)
        tf_image,    // I // 8 (image = bynary data)
        tf_datetime, // DT //7
        tf_version8, // 8, скрытое поле при recordlock == false и отсутствии поля типа tf_version
        tf_varbinary // VB // длина = length + 2
    }

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


    /// <summary>
    /// Класс таблиц
    /// </summary>
    public class V8Table
    {
        public static readonly UInt32 BLOB_RECORD_LEN = 256;
        public static readonly UInt32 BLOB_RECORD_DATA_LEN = 250;

        // структура изменной записи таблицы
        public class changed_rec
        {

            // владелец
            public V8Table parent;

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

            public changed_rec(V8Table _parent, changed_rec_type crt, UInt32 phys_numrecord)
            {
                parent = _parent;
                numrec = phys_numrecord;
                changed_type = crt;
                if (crt == changed_rec_type.deleted)
                {
                    fields = null;
                    rec = null;
                }
                else
                {
                    fields = new char[parent.num_fields];
                    //memset(fields, 0, parent.num_fields);
                    Array.Clear(fields, 0, fields.Length);
                    rec = new char[parent.recordlen];
                    //memset(rec, 0, parent.recordlen);
                    Array.Clear(rec, 0, rec.Length);
                }
                next = parent.ch_rec;
                parent.ch_rec = this;

            }

            public void clear()
            {
                Int32 i;
                v8Field f;
                type_fields tf;
                Stream b = null;

                if (rec != null && fields != null)
                    for (i = 0; i < parent.num_fields; i++)
                        if (!String.IsNullOrEmpty(fields[i].ToString()))
                        {
                            f = parent.fields[i];
                            tf = f.gettype();
                            if (tf == type_fields.tf_image || tf == type_fields.tf_string || tf == type_fields.tf_text)
                            {
                                //b = *(TStream**)(rec + f.getoffset() + (f.getnull_exists() ? 1 : 0));
                                b.Dispose();
                            }
                        }

            }
        }


        #region Поддержка динамического построения таблицы записей

        public UInt32[] recordsindex;      // массив индексов записей по номеру (только не пустые записи)
        public bool recordsindex_complete; // признак заполнености recordsindex
        public UInt32 numrecords_review;   // количество просмотренных записей всего в поиске не пустых
        public UInt32 numrecords_found;    // количество найденных непустых записей (текущий размер recordsindex)

        #endregion

        /// <summary>
        /// заполнить recordsindex не динамически
        /// </summary>
        public void fillrecordsindex()
        {
            UInt32 i;
            Int32 j;
            byte[] rec;

            if (recordsindex_complete)
                return;
            recordsindex = new UInt32[phys_numrecords];
            rec = new byte[recordlen];

            j = 0;
            for (i = 0; i < phys_numrecords; i++)
            {
                getrecord(i, rec);
                if (rec != null)
                    continue;
                recordsindex[j++] = i;
            }
            recordsindex_complete = true;
            numrecords_review = phys_numrecords;
            numrecords_found = (UInt32)j;
            log_numrecords = (UInt32)j;

            rec = null;

        }

        /// <summary>
        /// Конструктора
        /// </summary>
        public V8Table()
        {
            init();
        }

        public V8Table(T_1CD _base, Int32 block_descr)
        {
            base_ = _base;

            descr_table = new v8object(base_, block_descr);
            description = System.Text.Encoding.UTF8.GetString(descr_table.getdata(), 0, descr_table.getdata().Length);

            init(block_descr);
        }

        public V8Table(T_1CD _base, String _descr, Int32 block_descr = 0)
        {
            base_ = _base;

            descr_table = null;
            description = _descr;

            init(block_descr);
        }

        /// <summary>
        /// Инициализация без параметров
        /// </summary>
        public void init()
        {
            num_fields = 0;
            //fields.Clear();
            fields = new List<v8Field>();
            num_indexes = 0;
            indexes = null;
            recordlock = false;
            file_data = null;
            file_blob = null;
            file_index = null;
            lockinmemory = 0;

            recordsindex_complete = false;
            numrecords_review = 0;
            numrecords_found = 0;
            recordsindex = null;

            edit = false;
            ch_rec = null;
            added_numrecords = 0;

            phys_numrecords = 0;
            log_numrecords = 0;
            bad = true;

        }

        /// <summary>
        /// Инициализация по описанию
        /// </summary>
        /// <param name="block_descr"></param>
        public void init(Int32 block_descr)
        {
            tree t;
            tree f;
            tree in_;
            tree rt;
            Int32 i, j, k;
            UInt32 m;
            UInt64 s;
            v8Index ind;
            Int32 numrec;
            Int32[] blockfile = new Int32[3];
            v8Field fld;
            //UInt32[] buf = new UInt32[num_indexes + 1];
            //byte[] buf = new byte[num_indexes + 1];
            byte[] buf = new byte[PAGE8K];


            init();

            if ( String.IsNullOrEmpty(description))
                return;

            
            tree root = tree.parse_1Ctext(description, "Блок " + block_descr);

            if (root == null)
            {
                Console.WriteLine($"Ошибка разбора текста описания таблицы. Блок {block_descr}");
                init();
                return;
            }

            if (root.get_num_subnode() != 1)
            {
                Console.WriteLine($"Ошибка разбора текста описания таблицы. Количество узлов не равно 1. Блок {block_descr}, Узлов {root.get_num_subnode()}");
                init();
                root = null;
                return;
            }

            rt = root.get_first();

            if (rt.get_num_subnode() != 6)
            {
                Console.WriteLine($"Ошибка разбора текста описания таблицы. Количество узлов не равно 6. Блок {block_descr}, Узлов {rt.get_num_subnode()}");
                init();
                root = null;
                return;
            }

            t = rt.get_first();
            if (t.get_type() != node_type.nd_string)
            {
                Console.WriteLine($"Ошибка получения имени таблицы. Узел не является строкой. Блок, {block_descr}");
                init();
                root = null;
                return;
            }
            name = t.get_value();

            issystem =
                name[1] != '_' ||
                name.Substring(name.Length - 6, 7).Contains("STORAGE") ||
                name.Contains("_SYSTEMSETTINGS") ||
                name.Contains("_COMMONSETTINGS") ||
                name.Contains("_REPSETTINGS") ||
                name.Contains("_REPVARSETTINGS") ||
                name.Contains("_FRMDTSETTINGS") ||
                name.Contains("_SCHEDULEDJOBS");

            t = t.get_next();
            // пропускаем узел, так как там всегда содержится "0", и что это такое, неизвестно (версия формата описания таблиц?)
            t = t.get_next();
            if (t.get_type() != node_type.nd_list)
            {
                Console.WriteLine($"Ошибка получения полей таблицы. Узел не является деревом. Блок, {block_descr}, Таблица {name}");
                init();
                root = null;
                return;
            }
            if (t.get_num_subnode() < 2)
            {
                Console.WriteLine($"Ошибка получения полей таблицы. Нет узлов описания полей. Блок, {block_descr}, Таблица {name}");
                init();
                root = null;
                return;
            }

            num_fields = t.get_num_subnode() - 1;
            //fields.resize(num_fields);
            //fields.Capacity = num_fields;
            bool has_version = false; // признак наличия поля версии

            f = t.get_first();
            if (f.get_type() != node_type.nd_string)
            {
                Console.WriteLine($"Ошибка получения полей таблицы. Ожидаемый узел Fields не является строкой. Блок, {block_descr}, Таблица {name}");
                deletefields();
                init();
                root = null;
                return;
            }

            if (f.get_value() != "Fields")
            {
                Console.WriteLine($"Ошибка получения полей таблицы. Узел не Fields. Блок, {block_descr}, Таблица {name}, Узел {f.get_value()}");
                deletefields();
                init();
                root = null;
                return;
            }

            for (i = 0; i < num_fields; i++)
            {
                f = f.get_next();
                if (f.get_num_subnode() != 6)
                {
                    Console.WriteLine($"Ошибка получения узла очередного поля таблицы. Количество узлов поля не равно 6. Блок, {block_descr}, Таблица {name}, Номер поля {i + 1}, Узел {f.get_num_subnode()}");
                    deletefields();
                    init();
                    root = null;
                    return;
                }

                tree field_tree = f.get_first();
                try
                {

                    fields.Add(v8Field.field_from_tree(field_tree, ref has_version, this));


                    //fields[i] = v8Field.field_from_tree(field_tree, ref has_version, this);

                }
                catch
                {
                    deletefields();
                    init();
                    root = null;
                    Console.WriteLine($"Блок, {block_descr}, Таблица {name}, Номер поля {i + 1}");
                    return;
                }
            }
            t = t.get_next();
            if (t.get_type() != node_type.nd_list)
            {
                Console.WriteLine($"Ошибка получения индексов таблицы. Узел не является деревом. Блок, {block_descr}, Таблица {name}");
                deletefields();
                init();
                root = null;
                return;
            }
            if (t.get_num_subnode() < 1)
            {
                Console.WriteLine($"Ошибка получения индексов таблицы. Нет узлов описания индексов. Блок, {block_descr}, Таблица {name}");
                deletefields();
                init();
                root = null;
                return;
            }

            num_indexes = t.get_num_subnode() - 1;

            if (num_indexes != 0)
            {
                indexes = new v8Index[num_indexes];
                for (i = 0; i < num_indexes; i++)
                    indexes[i] = new v8Index(this);

                f = t.get_first();
                if (f.get_type() != node_type.nd_string)
                {
                    Console.WriteLine($"Ошибка получения индексов таблицы. Ожидаемый узел Indexes не является строкой. Блок, {block_descr}, Таблица {name}");
                    deletefields();
                    deleteindexes();
                    init();
                    root = null;
                    return;
                }

                if (f.get_value() != "Indexes")
                {
                    Console.WriteLine($"Ошибка получения индексов таблицы. Узел не Indexes. Блок, {block_descr}, Таблица {name}, Узел {f.get_value()}");
                    deletefields();
                    deleteindexes();
                    init();
                    root = null;
                    return;
                }
                for (i = 0; i < num_indexes; i++)
                {
                    f = f.get_next();
                    numrec = f.get_num_subnode() - 2;
                    if (numrec < 1)
                    {
                        Console.WriteLine($"Ошибка получения очередного индекса таблицы. Нет узлов описаня полей индекса. Блок, {block_descr}, Таблица {name}, Номер индекса {i+1}");
                        deletefields();
                        deleteindexes();
                        init();
                        root = null;
                        return;
                    }
                    ind = indexes[i];
                    ind.num_records = numrec;

                    if (f.get_type() != node_type.nd_list)
                    {
                        Console.WriteLine($"Ошибка получения очередного индекса таблицы. Узел не является деревом. Блок, {block_descr}, Таблица {name}, Номер индекса {i + 1}");
                        deletefields();
                        deleteindexes();
                        init();
                        root = null;
                        return;
                    }
                    tree index_tree = f.get_first();
                    if (index_tree.get_type() != node_type.nd_string)
                    {
                        Console.WriteLine($"Ошибка получения очередного индекса таблицы. Узел не является строкой. Блок, {block_descr}, Таблица {name}, Номер индекса {i + 1}");
                        deletefields();
                        deleteindexes();
                        init();
                        root = null;
                        return;
                    }
                    ind.name = index_tree.get_value();

                    index_tree = index_tree.get_next();
                    if (index_tree.get_type() != node_type.nd_number)
                    {
                        Console.WriteLine($"Ошибка получения очередного индекса таблицы. Узел не является строкой. Блок, {block_descr}, Таблица {name}, Номер индекса {ind.name}");
                        deletefields();
                        deleteindexes();
                        init();
                        root = null;
                        return;
                    }

                    String sIsPrimaryIndex = index_tree.get_value();
                    if (sIsPrimaryIndex == "0")
                        ind.is_primary = false;
                    else if (sIsPrimaryIndex == "1")
                        ind.is_primary = true;
                    else
                    {
                        Console.WriteLine($"Неизвестный тип индекса таблицы. Блок, {block_descr}, Таблица {name}, Индекс {ind.name}, Тип индекса {sIsPrimaryIndex}");
                        deletefields();
                        deleteindexes();
                        init();
                        root = null;
                        return;
                    }

                    ind.records = new index_record[numrec];
                    for (j = 0; j < numrec; j++)
                    {
                        index_tree = index_tree.get_next();
                        if (index_tree.get_num_subnode() != 2)
                        {
                            Console.WriteLine($"Ошибка получения очередного поля индекса таблицы. Количество узлов поля не равно 2. Блок, {block_descr}, Таблица {name}, Индекс {ind.name}, Номер поля индекса {j + 1}, Узлов {index_tree.get_num_subnode()}");
                            deletefields();
                            deleteindexes();
                            init();
                            root = null;
                            return;
                        }

				        in_ = index_tree.get_first();
                        if (in_.get_type() != node_type.nd_string)
				        {
                            Console.WriteLine($"Ошибка получения имени поля индекса таблицы. Узел не является строкой. Блок, {block_descr}, Таблица {name}, Индекс {ind.name}, Номер поля индекса {j + 1}");
                            deletefields();
                            deleteindexes();
                            init();
                            root = null;
                            return;
                        }
                        String field_name = in_.get_value();
                        for (k = 0; k < num_fields; k++)
                        {
                            if (fields[k].name == field_name)
                            {
                                ind.records[j].field = fields[k];
                                break;
                            }
                        }

                        if (k >= num_fields)
                        {
                            Console.WriteLine($"Ошибка получения индекса таблицы. Не найдено поле таблицы по имени поля индекса. Блок, {block_descr}, Таблица {name}, Индекс {ind.name}, Поле индекса {field_name}");
                            deletefields();
                            deleteindexes();
                            init();
                            root = null;
                            return;
                        }

				        in_ = in_.get_next();

                        if (in_.get_type() != node_type.nd_number)
                        {
                            Console.WriteLine($"Ошибка получения длины поля индекса таблицы. Узел не является числом. Блок, {block_descr}, Таблица {name}, Индекс {ind.name}, Поле индекса {field_name}");
                            deletefields();
                            deleteindexes();
                            init();
                            root = null;
                            return;
                        }
                        ind.records[j].len = Convert.ToInt32( in_.get_value(), 10);




                    }

                }

            }
            else
            {
                indexes = null;
            }

            t = t.get_next();
            if (t.get_num_subnode() != 2)
            {
                Console.WriteLine($"Ошибка получения типа блокировки таблицы. Количество узлов не равно 2. Блок, {block_descr}, Таблица {name}");
                deletefields();
                deleteindexes();
                init();
                root = null;
                return;
            }

            f = t.get_first();
            if (f.get_type() != node_type.nd_string)
            {
                Console.WriteLine($"Ошибка получения типа блокировки таблицы. Ожидаемый узел Recordlock не является строкой. Блок, {block_descr}, Таблица {name}");
                deletefields();
                deleteindexes();
                init();
                root = null;
                return;
            }

            if (f.get_value() != "Recordlock")
            {
                Console.WriteLine($"Ошибка получения типа блокировки таблицы. Узел не Recordlock. Блок, {block_descr}, Таблица {name}, Узел {f.get_value()}");
                deletefields();
                deleteindexes();
                init();
                root = null;
                return;
            }

            f = f.get_next();
            if (f.get_type() != node_type.nd_string)
            {
                Console.WriteLine($"Ошибка получения типа блокировки таблицы. Узел не является строкой. Блок, {block_descr}, Таблица {name}");
                deletefields();
                deleteindexes();
                init();
                root = null;
                return;
            }
            String sTableLock = f.get_value();
            if (sTableLock == "0")
                recordlock = false;
            else if (sTableLock == "1")
                recordlock = true;
            else
            {
                Console.WriteLine($"Неизвестное значение типа блокировки таблицы. Блок, {block_descr}, Таблица {name}, Тип блокировки {sTableLock}");
                deletefields();
                deleteindexes();
                init();
                root = null;
                return;
            }

            if (recordlock && !has_version)
            {// добавляем скрытое поле версии
                fld = new v8Field(this);
                fld.name = "VERSION";
                fld.type_manager = FieldType.Version8();
                //fields.push_back(fld);
                fields.Add(fld);
            }

            t = t.get_next();
            if (t.get_num_subnode() != 4)
            {
                Console.WriteLine($"Ошибка получения файлов таблицы. Количество узлов не равно 4. Блок, {block_descr}, Таблица {name}");
                deletefields();
                deleteindexes();
                init();
                root = null;
                return;
            }

            f = t.get_first();
            if (f.get_type() != node_type.nd_string)
            {
                Console.WriteLine($"Ошибка получения файлов таблицы. Ожидаемый узел Files не является строкой. Блок, {block_descr}, Таблица {name}");
                deletefields();
                deleteindexes();
                init();
                root = null;
                return;
            }

            if (f.get_value() != "Files")
            {
                Console.WriteLine($"Ошибка получения файлов таблицы. Узел не Files. Блок, {block_descr}, Таблица {name}, Узел {f.get_value()}");
                deletefields();
                deleteindexes();
                init();
                root = null;
                return;
            }

            for (i = 0; i < 3; i++)
            {
                f = f.get_next();
                if (f.get_type() != node_type.nd_number)
                {
                    Console.WriteLine($"Ошибка получения файлов таблицы. Узел не является числом. Блок, {block_descr}, Таблица {name}, Номер файла {i + 1}");
                    deletefields();
                    deleteindexes();
                    init();
                    root = null;
                    return;
                }
                blockfile[i] = Convert.ToInt32(f.get_value());
            }

            root = null;


            file_data  = (blockfile[0] != 0) ? new v8object(base_, blockfile[0]) : null;
            file_blob  = (blockfile[1] != 0) ? new v8object(base_, blockfile[1]) : null;
            file_index = (blockfile[2] != 0) ? new v8object(base_, blockfile[2]) : null;

            if (num_indexes !=0  && file_index == null)
            {
                Console.WriteLine($"В таблице есть индексы, однако файл индексов отсутствует. Блок, {block_descr}, Таблица {name}, Количество индексов {num_indexes}");
            }
            else if (num_indexes == 0 && file_index != null)
            {
                Console.WriteLine($"В таблице нет индексов, однако присутствует файл индексов. Блок, {block_descr}, Таблица {name}, Блок индексов {blockfile[2]}");
            }
            else if (file_index != null)
            {
                m = (UInt32)file_index.getlen() / base_.pagesize;

                if (file_index.getlen() != m * base_.pagesize)
                {
                    Console.WriteLine($"Ошибка чтения индексов. Длина файла индексов не кратна размеру страницы. Таблица {name}, Длина файла индексов {file_index.getlen()}");
                }
                else
                {
                    Int32 buflen = num_indexes * 4 + 4;
                    //buf = new UInt32[num_indexes + 1];
                    file_index.getdata(buf, 0, (UInt32)buflen);

                    //			// Временно, для отладки >>
                    //			if(buf[0]) msreg_g.AddMessage_("Существуют свободные страницы в файле индексов", MessageState::Hint,
                    //					"Таблица", name,
                    //					"Индекс свободной страницы", to_hex_string(buf[0]));
                    //			// Временно, для олтладки <<

                    if (buf[0] * base_.pagesize >= file_index.getlen())
                    {
                        Console.WriteLine($"Ошибка чтения индексов. Индекс первого свободного блока за пределами файла индексов. Таблица {name}, Длина файла индексов {file_index.getlen()}, Индекс свободной страницы {buf[0]}");
                    }
                    else
                    {
                        for (i = 1; i <= num_indexes; i++)
                        {
                            if ((int)base_.version < (int)db_ver.ver8_3_8_0)
                            {
                                if (buf[i] >= file_index.getlen())
                                {
                                    Console.WriteLine($"Ошибка чтения индексов. Указанное смещение индекса за пределами файла индексов. Таблица {name}, Длина файла индексов {file_index.getlen()}, Номер индекса { i }, Смещение индекса {buf[i]}");
                                }
                                else if ((buf[i] & 0xfff) != 0)
                                {
                                    Console.WriteLine($"Ошибка чтения индексов. Указанное смещение индекса не кратно 4 Кб. Таблица {name}, Длина файла индексов {file_index.getlen()}, Номер индекса { i }, Смещение индекса {buf[i]}");
                                }
                                else
                                    indexes[i - 1].start = buf[i];
                            }
                            else
                            {
                                s = buf[i];
                                s *= base_.pagesize;
                                if (s >= file_index.getlen())
                                {
                                    Console.WriteLine($"Ошибка чтения индексов. Указанное смещение индекса за пределами файла индексов. Таблица {name}, Длина файла индексов {file_index.getlen()}, Номер индекса { i }, Смещение индекса { s }");
                                }
                                else
                                    indexes[i - 1].start = s;
                            }
                        }
                    }

                    buf = null;

                }

            }

            // вычисляем длину записи таблицы как сумму длинн полей и проставим смещения полей в записи
            recordlen = 1; // первый байт записи - признак удаленности
                           // сначала идут поля (поле) с типом "версия"
            for (i = 0; i < num_fields; i++)
            {
                if (fields[i].type_manager.gettype() == type_fields.tf_version || fields[i].type_manager.gettype() == type_fields.tf_version8)
                {
                    fields[i].offset = recordlen;
                    recordlen += fields[i].getlen();
                }
            }

            // затем идут все остальные поля
            for (i = 0; i < num_fields; i++)
            {
                if (fields[i].type_manager.gettype() != type_fields.tf_version && fields[i].type_manager.gettype() != type_fields.tf_version8)
                {
                    fields[i].offset = recordlen;
                    recordlen += fields[i].getlen();
                }
            }
            if (recordlen < 5) recordlen = 5; // Длина одной записи не может быть меньше 5 байт (1 байт признак, что запись свободна, 4 байт - индекс следующей следующей свободной записи)

            if (recordlen == 0  || file_data == null)
                phys_numrecords = 0;
            else
                phys_numrecords = (UInt32)file_data.getlen() / (UInt32)recordlen; ;

            if (file_data != null )
            {
                if (phys_numrecords * (UInt32)recordlen != file_data.getlen())
                {
                    Console.WriteLine($"Длина таблицы не кратна длине записи. Блок {block_descr} Таблица {name}, Длина таблицы {file_data.getlen()}, Длина записи { recordlen }");
                }
            }
            else
            {
                Console.WriteLine($"Отсутствует файл данных таблицы. Блок {block_descr} Таблица {name}");
                return;
            }

            // Инициализация данных индекса
            for (i = 0; i < num_indexes; i++)
                indexes[i].get_length();

            Console.WriteLine($"Создана таблица. Таблица {name} Длина таблицы {file_data.getlen()}, длина записи {recordlen}");

            bad = false;

        }// окончание процедуры

        /// <summary>
        /// Получить имя
        /// </summary>
        /// <returns></returns>
        public String getname()
        {
            return name;
        }

        public String getdescription()
        {
            return description;
        }

        public Int32 get_numfields()
        {
            return num_fields;
        }

        public Int32 get_numindexes()
        {
            return num_indexes;
        }

        public v8Field getfield(Int32 numfield)
        {
            if (numfield >= num_fields)
            {
                Console.WriteLine($"Попытка получения поля таблицы по номеру, превышающему количество полей. Таблица {name} Количество полей {num_fields} Номер поля {numfield + 1}");
                return null;
            }
            return fields[numfield];
        }

        public v8Index getindex(Int32 numindex)
        {
            if (numindex >= num_indexes)
            {
                Console.WriteLine($"Попытка получения индекса таблицы по номеру, превышающему количество индексов. Таблица {name} Количество индексов {num_indexes} Номер индекса {numindex + 1}");
                return null;
            }
            return indexes[numindex];
        }

        public bool get_issystem()
        {
            return issystem;
        }

        public Int32 get_recordlen()
        {
            return recordlen;
        }

        public bool get_recordlock()
        {
            return recordlock;
        }

        /// <summary>
        /// возвращает количество записей в таблице всего, вместе с удаленными
        /// </summary>
        /// <returns></returns>
        public UInt32 get_phys_numrecords()
        {
            return phys_numrecords;
        }

        /// <summary>
        /// возвращает количество записей в таблице всего, без удаленных
        /// </summary>
        /// <returns></returns>
        public UInt32 get_log_numrecords()
        {
            return log_numrecords;
        } 

        public void set_log_numrecords(UInt32 _log_numrecords)
        {
            log_numrecords = _log_numrecords;
        } //

        public UInt32 get_added_numrecords()
        {
            return added_numrecords;
        }

        /// <summary>
        /// возвращает указатель на запись, буфер принадлежит вызывающей процедуре
        /// </summary>
        /// <param name="phys_numrecord"></param>
        /// <param name="buf"></param>
        /// <returns></returns>
        public byte[] getrecord(UInt32 phys_numrecord, byte[] buf)
        {
            return file_data.getdata(buf, (UInt32)phys_numrecord * (UInt32)recordlen, (UInt32)recordlen);
        } 

        public Stream readBlob(Stream _str, UInt32 _startblock, UInt32 _length, bool rewrite = true)
        {
            UInt32 _curblock = 0;
            byte[] _curb;

            UInt16 _curlen;
            UInt32 _filelen = 0;
            UInt32 _numblock = 0;
            UInt32 startlen = 0;

            if (rewrite)
                _str.SetLength(0);

            startlen = (UInt32)_str.Position;
            if (_startblock == 0 && _length != 0)
            {
                Console.WriteLine($"Попытка чтения нулевого блока файла Blob. Таблица {name}");
                return _str;
            }

            if (file_blob != null)
            {
                _filelen = (UInt32)file_blob.getlen();
                _numblock = (UInt32)_filelen >> 8;
                if (_numblock << 8 != _filelen)
                {
                    Console.WriteLine($"Длина файла Blob не кратна 0x100. Таблица {name}. Длина файла {_filelen}");
                }

                _curb = new byte[0x100];
                _curblock = _startblock;

                while (_curblock != 0)
                {
                    if (_curblock >= _numblock)
                    {
                        Console.WriteLine($"Попытка чтения блока файла Blob за пределами файла. Таблица {name}. Всего блоков {_numblock}. Читаемый блок {_curblock}");
                        return _str;
                    }
                    file_blob.getdata(_curb, _curblock << 8, 0x100);

                    _curblock = (UInt32)_curb[0];

                    _curlen = _curb[4];

                    if (_curlen > 0xfa)
                    {
                        Console.WriteLine($"Попытка чтения из блока файла Blob более 0xfa байт. Таблица {name}. Индекс блока {_curblock}. Читаемый байт {_curlen}");
                        return _str;
                    }
                    _str.Write(_curb, 6, _curlen);

                    if (_str.Length - startlen > _length)
                        break; // аварийный выход из возможного ошибочного зацикливания

                }
                _curb = null;

                if (_str.Length - startlen != _length)
                {
                    Console.WriteLine($"Несовпадение длины Blob-поля, указанного в записи, с длиной практически прочитанных данных. Таблица {name}. Длина поля {_length}. Прочитано {_str.Length - startlen}");
                }
            }
            else
            {
                Console.WriteLine($"Попытка чтения Blob-поля при отстутствующем файле Blob. Таблица {name}. Длина поля {_length}");
            }

            return _str;
        }

        public UInt32 readBlob(byte[] buf, UInt32 _startblock, UInt32 _length)
        {
            UInt32 _curblock;
            //byte[] _curb = new byte[0x100]; ;
            byte[] _buf = new byte[buf.Length];
            UInt16 _curlen;
            UInt32 _filelen, _numblock;
            UInt32 readed;
            UInt32 destIndex = 0;

            if (_startblock == 0 && _length != 0)
            {
                Console.WriteLine($"Попытка чтения нулевого блока файла Blob. Таблица {name}");
                return 0;
            }
            
            Array.Copy(buf, 0, _buf, 0, buf.Length);
            readed = 0;
            if (file_blob != null)
            {
                _filelen = (UInt32)file_blob.getlen();
                _numblock = _filelen >> 8;
                if (_numblock << 8 != _filelen)
                {
                    Console.WriteLine($"Длина файла Blob не кратна 0x100. Таблица {name}. Длина файла {_filelen}");
                }

                byte[] _curb = new byte[0x100];
                _curblock = _startblock;

                while (_curblock != 0)
                {
                    if (_curblock >= _numblock)
                    {
                        Console.WriteLine($"Попытка чтения блока файла Blob за пределами файла. Таблица {name}. Всего блоков {_numblock}. Читаемый блок {_curblock}");
                        return readed;
                    }
                    file_blob.getdata(_curb, _curblock << 8, BLOB_RECORD_LEN);
                    _curblock = _curb[0];
                    _curlen = _curb[4];
                    if (_curlen > BLOB_RECORD_DATA_LEN)
                    {
                        Console.WriteLine($"Попытка чтения из блока файла Blob более 0xfa байт. Таблица {name}. Индекс блока {_curblock}. Читаемый байт {_curlen}");
                        return readed;
                    }
                    Array.Copy(_curb, 6, _buf, destIndex, _curlen);
                    destIndex += _curlen;
                    readed += _curlen;

                    if (readed > _length)
                        break; // аварийный выход из возможного ошибочного зацикливания
                }
                _curb = null;

                if (readed != _length)
                {
                    Console.WriteLine($"Несовпадение длины Blob-поля, указанного в записи, с длиной практически прочитанных данных. Таблица {name}. Длина поля {_length}. Прочитано {readed}");
                }


            }
            else
            {
                Console.WriteLine($"Попытка чтения Blob-поля при отстутствующем файле Blob. Таблица {name}. Длина поля {_length}");
            }

            return readed;
        }

        public void set_lockinmemory(bool _lock) { }

        public bool export_to_xml(String filename, bool blob_to_file, bool unpack) { return true; }

        public v8object get_file_data()
        {
            return file_data;
        }

        public v8object get_file_blob()
        {
            return file_blob;
        }

        public v8object get_file_index()
        {
            return file_index;
        }

        /// <summary>
        /// получить физическое смещение в файле записи по номеру
        /// </summary>
        /// <param name="phys_numrecord"></param>
        /// <returns></returns>
        public UInt64 get_fileoffset(UInt32 phys_numrecord)
        {
            return file_data.get_fileoffset(phys_numrecord * (UInt32)recordlen);
        }

        /// <summary>
        /// Возвращает указатель на запись, буфер принадлежит вызывающей процедуре
        /// </summary>
        /// <param name="phys_numrecord"></param>
        /// <param name="buf"></param>
        /// <returns></returns>
        public byte[] get_edit_record(UInt32 phys_numrecord, byte[] rec)
        {
            changed_rec cr;
            for (cr = ch_rec; cr != null; cr = cr.next)
                if (phys_numrecord == cr.numrec)
                {
                    if (cr.changed_type != changed_rec_type.deleted)
                    {
                        Array.Copy(cr.rec, rec, recordlen);
                        return rec;
                    }
                    break;
                }
            return getrecord(phys_numrecord, rec);
        } 

        public bool get_edit()
        {
            return edit;
        }

        /// <summary>
        /// Получить физический индекс записи по номеру строки по указанному индексу
        /// </summary>
        /// <param name="ARow"></param>
        /// <param name="cur_index"></param>
        /// <returns></returns>
        public UInt32 get_phys_numrec(Int32 ARow, v8Index cur_index)
        {

            UInt32 numrec;

            if (ARow == 0)
            {
                Console.WriteLine($"Попытка получения номера физической записи по нулевому номеру строки. Таблица {name}");
                return 0;
            }

            if (edit)
            {
                if (ARow > log_numrecords + added_numrecords)
                {
                    Console.WriteLine($"Попытка получения номера физической записи по номеру строки, превышающему количество записей. Таблица {name}" +
                        $"Количество логических записей {log_numrecords}, Количество добавленных записей {added_numrecords}, Номер строки {ARow}");
                    return 0;
                }
                if (ARow > log_numrecords)
                    return (UInt32)ARow - 1 - log_numrecords + phys_numrecords;
            }

            if (ARow > log_numrecords)
            {
                Console.WriteLine($"Попытка получения номера физической записи по номеру строки, превышающему количество записей. Таблица {name}, Количество логических записей {log_numrecords}, Номер строки {ARow}");
                return 0;
            }
            if (cur_index != null)
                numrec = cur_index.get_numrec((UInt32)ARow - 1);
            else
            {
                /* для чего-то это нужно
                # ifndef getcfname
                    tr_syn->BeginRead();
                #endif
                */
                numrec = recordsindex[ARow - 1];
                /* для чего-то это нужно
                # ifndef getcfname
                    tr_syn->EndRead();
                #endif
                */
            }

            return numrec;
        }

        /// <summary>
        /// получить имя файла по-умолчанию конкретного поля конкретной записи
        /// </summary>
        /// <param name="num_field"></param>
        /// <param name="rec"></param>
        /// <param name="numrec"></param>
        /// <returns></returns>
        public String get_file_name_for_field(Int32 num_field, byte[] rec, UInt32 numrec = 0)
        {
            String s = "";
            Int32 i;
            v8Index ind;

            if (num_indexes != 0)
            {
                ind = indexes[0];
                for (i = 0; i < num_indexes; i++)
                    if (indexes[i].is_primary)
                    {
                        ind = indexes[i];
                        break;
                    }
                for (i = 0; i < ind.num_records; i++)
                {
                    if (s.Length != 0)
                        s += "_";
                    s += ind.records[i].field.get_XML_presentation(rec);
                }
                if (!ind.is_primary && numrec != 0)
                {
                    s += "_";
                    s += numrec;
                }
            }

            if (!issystem || String.Compare(name, "CONFIG") == 0 || String.Compare(name, "CONFIGSAVE") == 0 || String.Compare(name, "FILES") == 0 || String.Compare(name, "PARAMS") == 0)
            {
                if (s.Length != 0)
                    s += "_";
                s += fields[num_field].getname();
            }
            return s;

            
        } 

        /// <summary>
        /// получить имя файла по-умолчанию конкретной записи
        /// </summary>
        /// <param name="rec"></param>
        /// <returns></returns>
        public String get_file_name_for_record(byte[] rec)
        {
            String s = "";

            Int32 i;
            Int32 num_rec;

            v8Index ind;

            if (num_indexes != 0)
            {
                ind = indexes[0];
                for (i = 0; i < num_indexes; i++)
                {

                    if (indexes[i].is_primary)
                    {
                        ind = indexes[i];
                        break;
                    }
                }
                num_rec = ind.num_records;

                for (i = 0; i < num_rec; i++)
                {
                    if (s.Length != 0)
                    {
                        s += "_";
                    }
                    v8Field tmp_field = ind.records[i].field;
                    String tmp_str = tmp_field.get_XML_presentation(rec);

                    s += tmp_str;

                }
            }

            return s;
        } 

        public T_1CD getbase() { return base_; }

        public void begin_edit() { } // переводит таблицу в режим редактирования

        public void cancel_edit() { } // переводит таблицу в режим просмотра и отменяет все изменения

        public void end_edit() { } // переводит таблицу в режим просмотра и сохраняет все изменения

        public changed_rec_type get_rec_type(UInt32 phys_numrecord)
        {
            changed_rec cr;
            if (!edit)
            {
                return changed_rec_type.not_changed;
            }
            cr = ch_rec;
            while (cr != null)
            {
                if (cr.numrec == phys_numrecord)
                    return cr.changed_type;
                cr = cr.next;
            }
            return changed_rec_type.not_changed;
        }

        public changed_rec_type get_rec_type(UInt32 phys_numrecord, Int32 numfield)
        {
            changed_rec cr;
            if (!edit)
            {
                return changed_rec_type.not_changed;
            }
            cr = ch_rec;
            while (cr != null)
            {
                if (cr.numrec == phys_numrecord)
                {
                    if (cr.changed_type == changed_rec_type.changed)
                    {
                        return cr.fields[numfield] != '0' ? changed_rec_type.changed : changed_rec_type.not_changed;
                    }
                    return cr.changed_type;
                }
                cr = cr.next;
            }
            return changed_rec_type.not_changed;
        }

        public void set_edit_value(UInt32 phys_numrecord, Int32 numfield, bool Null, String value, Stream st = null) { }

        public void restore_edit_value(UInt32 phys_numrecord, Int32 numfield) { }

        public void set_rec_type(UInt32 phys_numrecord, changed_rec_type crt) { }

        public void export_table(String path) { }

        public void import_table(String path) { }

        public void delete_record(UInt32 phys_numrecord) { } // удаление записи

        public void insert_record(char[] rec) { } // добавление записи

        public void update_record(UInt32 phys_numrecord, char[] rec, char[] changed_fields) { } // изменение записи

        public byte[] get_record_template_test()
        {
            Int32 len;
            byte[] res;
            byte[] curp;
            Int32 i, j, l;
            v8Field f;
            bool required;

            len = recordlen << 8;
            res = new byte[len];
            //memset(res, 0, len);
            Array.Clear(res, 0, len);
            curp = new byte[len];

            for (i = 0; i < num_fields; i++)
            {
                required = false;
                f = fields[i];
                //curp = res + (f.getoffset() << 8);
                Array.Copy(res, f.getoffset() << 8, curp, 0, res.Length);

                if (f.getnull_exists())
                {
                    curp[0] = 1;
                    curp[1] = 1;
                    // похоже пробегаем весь массив с шагом BLOB_RECORD_LEN
                    //curp += BLOB_RECORD_LEN; 
                }

                l = f.getlength();
                switch (f.gettype())
                {
                    case type_fields.tf_binary: // B // длина = length
                        //memset(curp, 1, BLOB_RECORD_LEN * l);
                        for (int ii = 0; ii < curp.Length; ii++)
                        {
                            curp[ii] = 1;
                        }
                        break;
                    case type_fields.tf_bool: // L // длина = 1
                        curp[0] = 1;
                        curp[1] = 1;
                        break;
                    case type_fields.tf_numeric: // N // длина = (length + 2) / 2
                        j = (l + 2) / 2;
                        for (; j > 0; --j)
                        {
                            /* пока не понятна
                            memcpy(curp, NUM_TEST_TEMPLATE, BLOB_RECORD_LEN);
                            curp += BLOB_RECORD_LEN;
                            */
                        }
                        break;
                    case type_fields.tf_char: // NC // длина = length * 2
                        //memset(curp, 1, BLOB_RECORD_LEN * 2 * l);
                        for (int ii = 0; ii < curp.Length; ii++)
                        {
                            curp[ii] = 1;
                        }
                        break;
                    case type_fields.tf_varchar: // NVC // длина = length * 2 + 2
                        if (l > 255)
                            j = (Int32)BLOB_RECORD_LEN;
                        else
                            j = l + 1;
                        /* пока непонятно
                        memset(curp, 1, j);
                        //curp[0x20] = 1;
                        curp += BLOB_RECORD_LEN;
                        j = (l >> 8) + 1;
                        memset(curp, 1, j);
                        curp += BLOB_RECORD_LEN;
                        memset(curp, 1, BLOB_RECORD_LEN * 2 * l);
                        */
                        break;
                    case type_fields.tf_version: // RV // 16, 8 версия создания и 8 версия модификации ? каждая версия int32_t(изменения) + int32_t(реструктуризация)
                        //memset(curp, 1, BLOB_RECORD_LEN * 16);
                        break;
                    case type_fields.tf_string: // NT // 8 (unicode text)
                        //memset(curp, 1, BLOB_RECORD_LEN * 8);
                        break;
                    case type_fields.tf_text: // T // 8 (ascii text)
                        //memset(curp, 1, BLOB_RECORD_LEN * 8);
                        break;
                    case type_fields.tf_image: // I // 8 (image = bynary data)
                        //memset(curp, 1, BLOB_RECORD_LEN * 8);
                        break;
                    case type_fields.tf_datetime: // DT //7
                        if (String.Compare(f.getname(), "_DATE_TIME") == 0)
                            required = true;
                        else if (String.Compare(f.getname(), "_NUMBERPREFIX") == 0)
                            required = true;
                        /*
                        memcpy(curp, DATE1_TEST_TEMPLATE, BLOB_RECORD_LEN);
                        curp += BLOB_RECORD_LEN;
                        memcpy(curp, NUM_TEST_TEMPLATE, BLOB_RECORD_LEN);
                        curp += BLOB_RECORD_LEN;
                        memcpy(curp, DATE3_TEST_TEMPLATE, BLOB_RECORD_LEN);
                        if (required) curp[0] = 0;
                        curp += BLOB_RECORD_LEN;
                        memcpy(curp, DATE4_TEST_TEMPLATE, BLOB_RECORD_LEN);
                        if (required) curp[0] = 0;
                        curp += BLOB_RECORD_LEN;
                        memcpy(curp, DATE5_TEST_TEMPLATE, BLOB_RECORD_LEN);
                        curp += BLOB_RECORD_LEN;
                        memcpy(curp, DATE67_TEST_TEMPLATE, BLOB_RECORD_LEN);
                        curp += BLOB_RECORD_LEN;
                        memcpy(curp, DATE67_TEST_TEMPLATE, BLOB_RECORD_LEN);
                        */
                        break;
                    case type_fields.tf_version8: // 8, скрытое поле при recordlock == false и отсутствии поля типа tf_version
                        //memset(curp, 1, BLOB_RECORD_LEN * 8);
                        break;
                    case type_fields.tf_varbinary: // VB // длина = length + 2
                        if (l > 255)
                            j = (Int32)BLOB_RECORD_LEN;
                        else
                            j = l + 1;
                        /*
                        memset(curp, 1, j);
                        curp += BLOB_RECORD_LEN;
                        j = (l >> 8) + 1;
                        memset(curp, 1, j);
                        curp += BLOB_RECORD_LEN;
                        memset(curp, 1, BLOB_RECORD_LEN * l);
                        */
                        break;
                }
            }

            res[0] = 1;

            return res;
        }

        public v8Field get_field(String fieldname)
        {
            v8Field fld = null;

            for (Int32 j = 0; j < num_fields; j++)
            {
                fld = fields[j];
                
                if (String.Compare(fld.getname(), fieldname) == 0)
                {
                    return fld;
                }
            }

            String s = "В таблице ";
            s += name;
            s += " не найдено поле ";
            s += fieldname;
            s += ".";
            Console.WriteLine(s);

            return fld;
            
        }

        public v8Index get_index(String indexname)
        {
            v8Index ind = null;

            for (Int32 j = 0; j < num_indexes; j++)
            {
                ind = indexes[j];
                if (String.Compare(ind.getname(), indexname) == 0)
                {
                    return ind;
                }
            }

            String s = "В таблице ";
            s += name;
            s += " не найден индекс ";
            s += indexname;
            s += ".";
            Console.WriteLine(s);

            return ind;

        }

        #region private
        public T_1CD base_;

        private v8object descr_table; // объект с описанием структуры таблицы (только для версий с 8.0 до 8.2.14)
        private String description;
        public String name;
        private Int32 num_fields;
        private List<v8Field> fields;


        private Int32 num_indexes;
        private v8Index[] indexes;
        private bool recordlock;
        private v8object file_data;
        private v8object file_blob;
        public v8object file_index;
        private Int32 recordlen; // длина записи (в байтах)
        private bool issystem; // Признак системной таблицы (имя таблицы не начинается с подчеркивания)
        private Int32 lockinmemory; // счетчик блокировок в памяти

        /// <summary>
        /// Удаление всех полей
        /// </summary>
        private void deletefields()
        {
            if (fields != null)
                fields.Clear();
        }

        /// <summary>
        /// Удаление всех индексов
        /// </summary>
        private void deleteindexes()
        {
            Int32 i;
            if (indexes != null)
            {
                for (i = 0; i < num_indexes; i++)
                    indexes[i] = null;
                indexes = null;
            }
        }

        private changed_rec ch_rec; // первая измененная запись в списке измененных записей
        private UInt32 added_numrecords; // количество добавленных записей в режиме редактирования

        private UInt32 phys_numrecords; // физическое количество записей (вместе с удаленными)
        public UInt32 log_numrecords; // логическое количество записей (только не удаленные)

        /// <summary>
        /// создание файла file_data
        /// </summary>
        private void create_file_data()
        {
            if (file_data == null) return;
            file_data = new v8object(base_);
            refresh_descr_table();
        }

        /// <summary>
        /// создание файла file_blob
        /// </summary>
        private void create_file_blob()
        {
            if (file_blob == null) return;
            file_blob = new v8object(base_);
            refresh_descr_table();
        }

        /// <summary>
        /// создание файла file_index
        /// </summary>
        private void create_file_index()
        {
            if (file_index == null) return;
            file_index = new v8object(base_);
            refresh_descr_table();
        }

        /// <summary>
        /// создание и запись файла описания таблицы
        /// </summary>
        private void refresh_descr_table()
        {
            Console.WriteLine($"Попытка обновления файла описания таблицы. Таблица {name}");
            return;
        } 

        private bool edit; // признак, что таблица находится в режиме редактирования

        /// <summary>
        /// Удаление записи из файла data
        /// </summary>
        /// <param name="phys_numrecord"></param>
        private void delete_data_record(UInt32 phys_numrecord)
        {
            
            Int32 first_empty_rec = 0;

            if (!edit)
            {
                Console.WriteLine($"Попытка удаления записи из файла file_data не в режиме редактирования. Таблица {name}, Индекс удаляемой записи {phys_numrecord}");
                return;
            }

            if (file_data == null)
            {
                Console.WriteLine($"Попытка удаления записи из несуществующего файла file_data. Таблица {name}, Индекс удаляемой записи {phys_numrecord}");
                return;
            }

            if (phys_numrecord >= phys_numrecords)
            {
                Console.WriteLine($"Попытка удаления записи в файле file_data за пределами файла. Таблица {name}, Всего записей {phys_numrecords} Индекс удаляемой записи {phys_numrecord}");
                return;
            }

            if (phys_numrecord == 0)
            {
                Console.WriteLine($"Попытка удаления нулевой записи в файле file_data. Таблица {name}, Всего записей {phys_numrecords}");
                return;
            }

            if (phys_numrecord == phys_numrecords - 1)
            {
                file_data.set_len(phys_numrecord * (UInt32)recordlen);
                phys_numrecords--;
            }
            else
            {
                byte[] rec = new byte[recordlen];
                //memset(rec, 0, recordlen);
                Array.Clear(rec, 0, rec.Length);
                //file_data.getdata(first_empty_rec, 0, 4);
                file_data.getdata(rec, 0, 4);

                //*((int32_t*)rec) = first_empty_rec;

                //file_data->setdata(&first_empty_rec, 0, 4);
                file_data.setdata(rec, 0, 4);

                write_data_record(phys_numrecord, rec);

                rec = null;
            }


        }

        /// <summary>
        /// удаление записи из файла blob
        /// </summary>
        /// <param name="blob_numrecord"></param>
        private void delete_blob_record(UInt32 blob_numrecord)
        {
            //Int32 prev_free_first;
            byte[] prev_free_first;
            Int32 i, j;

            if (!edit)
            {
                Console.WriteLine($"Попытка удаления записи из файла file_blob не в режиме редактирования. Таблица {name}, Смещение удаляемой записи {blob_numrecord << 8}");
                return;
            }

            if (file_blob == null)
            {
                Console.WriteLine($"Попытка удаления записи из несуществующего файла file_blob. Таблица {name}, Смещение удаляемой записи {blob_numrecord << 8}");
                return;
            }

            if (blob_numrecord << 8 >= file_blob.getlen())
            {
                Console.WriteLine($"Попытка удаления записи в файле file_blob за пределами файла. Таблица {name}, Смещение удаляемой записи {blob_numrecord << 8}, Длина файла {file_blob.getlen()}");
                return;
            }

            if (blob_numrecord == 0)
            {
                Console.WriteLine($"Попытка удаления нулевой записи в файле file_blob. Таблица {name}, Длина файла {file_blob.getlen()}");
                return;
            }

            prev_free_first = new byte[4];

            file_blob.getdata(prev_free_first, 0, 4); // читаем предыдущее начало свободных блоков

            /* ЖУТЬ !!!!!!!!!!!!!

            // ищем последний блок в цепочке удаляемых
            for (i = (Int32)blob_numrecord; i != 0; file_blob.getdata(&i, i << 8, 4) )
                j = i; 
            file_blob.setdata(prev_free_first, j << 8, 4); // устанавливаем в конец цепочки удаляемых блоков старое начало цепочки свободных блоков
            file_blob.setdata(blob_numrecord, 0, 4); // устанавливаем новое начало цепочки свободных блоков на начало удаляемых блоков
            */

        }

        /// <summary>
        /// удаление всех индексов записи из файла index
        /// </summary>
        /// <param name="phys_numrecord"></param>
        private void delete_index_record(UInt32 phys_numrecord)
        {
            byte[] rec;

            rec = new byte[recordlen];
            getrecord(phys_numrecord, rec);
            delete_index_record(phys_numrecord, rec);
            rec = null;

        }

        /// <summary>
        /// удаление всех индексов записи из файла index
        /// </summary>
        /// <param name="phys_numrecord"></param>
        /// <param name="rec"></param>
        private void delete_index_record(UInt32 phys_numrecord, byte[] rec)
        {
            /* не понятно пока...
            if (*rec)
            {
                msreg_g.AddError("Попытка удаления индекса удаленной записи.",
                    "Таблица", name,
                    "Физический номер записи", phys_numrecord);
                return;
            }
            */
            for (Int32 i = 0; i < num_indexes; i++)
                indexes[i].delete_index(rec, phys_numrecord);

        }

        /// <summary>
        /// запись одной записи в файл data
        /// </summary>
        /// <param name="phys_numrecord"></param>
        /// <param name="rec"></param>
        private void write_data_record(UInt32 phys_numrecord, byte[] rec)
        {

            if (!edit)
            {
                Console.WriteLine($"Попытка записи в файл file_data не в режиме редактирования. Таблица {name} Индекс записываемой записи {phys_numrecord}");
                return;
            }

            if (file_data == null)
                create_file_data();

            if (phys_numrecord > phys_numrecords && !(phys_numrecord == 1 && phys_numrecords == 0))
            {
                Console.WriteLine($"Попытка записи в файл file_data за пределами файла. Таблица {name} Всего записей {phys_numrecords} Индекс записываемой записи {phys_numrecord}");
                return;
            }

            if (phys_numrecord == 0)
            {
                Console.WriteLine($"Попытка записи нулевой записи в файл file_data. Всего записей {phys_numrecords}");
                return;
            }

            if (phys_numrecords == 0)
            {
                byte[] b = new byte[recordlen];
                //memset(b, 0, recordlen);
                Array.Clear(b, 0, b.Length);
                b[0] = 1;
                file_data.setdata(b, 0, (UInt32)recordlen);
                b = null;
            }
            file_data.setdata(rec, phys_numrecord * (UInt32)recordlen, (UInt32)recordlen);
        }

        /// <summary>
        /// записывает НОВУЮ запись в файл blob, возвращает индекс новой записи
        /// </summary>
        /// <param name="blob_record"></param>
        /// <param name="blob_len"></param>
        /// <returns></returns>
        private UInt32 write_blob_record(char[] blob_record, UInt32 blob_len)
        {
            UInt32 cur_block, cur_offset, prev_offset, first_block = 0, next_block;
            UInt32 zero = 0;
            UInt16 cur_len;

            if (!edit)
            {
                Console.WriteLine($"Попытка записи в файл file_blob не в режиме редактирования. Таблица {name}");
                return 0;
            }

            if (blob_len == 0) return 0;

            if (file_blob == null)
            {
                create_file_blob();
                byte[] b = new byte[BLOB_RECORD_LEN];
                //memset(b, 0, BLOB_RECORD_LEN);
                Array.Clear(b, 0, (Int32)BLOB_RECORD_LEN);
                file_blob.setdata(b, 0, BLOB_RECORD_LEN);
                b = null;
            }

            /* пока так
            file_blob.getdata(&first_block, 0, 4);
            if (!first_block) first_block = file_blob->getlen() >> 8;
            prev_offset = 0;

            for (cur_block = first_block; blob_len; blob_len -= cur_len, cur_block = next_block, blob_record += cur_len)
            {
                cur_len = std::min(blob_len, BLOB_RECORD_DATA_LEN);
                if (cur_len < BLOB_RECORD_DATA_LEN) memset(blob_record, 0, BLOB_RECORD_DATA_LEN);

                if (prev_offset) file_blob->setdata(&cur_block, prev_offset, 4);

                cur_offset = cur_block << 8;
                next_block = 0;
                if (cur_offset < file_blob->getlen()) file_blob->getdata(&next_block, cur_offset, 4);

                file_blob->setdata(&zero, cur_offset, 4);
                file_blob->setdata(&cur_len, cur_offset + 4, 2);
                file_blob->setdata(blob_record, cur_offset + 6, BLOB_RECORD_DATA_LEN);

                if (!next_block) next_block = file_blob->getlen() >> 8;
                prev_offset = cur_offset;
            }
            if (next_block << 8 < file_blob->getlen()) file_blob->setdata(&next_block, 0, 4);
            else file_blob->setdata(&zero, 0, 4);

            */
            return first_block;
        }

        /// <summary>
        ///  // записывает НОВУЮ запись в файл blob, возвращает индекс новой записи
        /// </summary>
        /// <param name="bstr"></param>
        /// <returns></returns>
        private UInt32 write_blob_record(Stream bstr)
        {
            return 0;
        }

        /// <summary>
        /// запись индексов записи в файл index
        /// </summary>
        /// <param name="phys_numrecord"></param>
        /// <param name="rec"></param>
        private void write_index_record(UInt32 phys_numrecord, char[] rec)
        {

        } 

        public bool bad; // признак битой таблицы

        #endregion

    }
}
