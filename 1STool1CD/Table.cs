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

    public enum Type_fields
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

    public enum Table_info
    {
        ti_description,
        ti_fields,
        ti_indexes,
        ti_physical_view,
        ti_logical_view
    }

    // типы измененных записей
    public enum Changed_rec_type
    {
        not_changed,
	    changed,
	    inserted,
	    deleted
    }


    // структура одного блока в файле file_blob
    public struct Blob_block
    {
        private UInt32 nextblock;
        private Int16 length;
        //public char[] data[BLOB_RECORD_DATA_LEN];
        private char[] data;

        public uint Nextblock { get { return nextblock; } set { nextblock = value; } }

        public short Length { get { return length; } set { length = value; } }

        public char[] Data { get { return data; } set { data = value; } }
    };

    // структура root файла экспорта/импорта таблиц
    public struct Export_import_table_root
    {
        private bool has_data;
        private bool has_blob;
        private bool has_index;
        private bool has_descr;
        private Int32 data_version_1; // версия реструктуризации
        private Int32 data_version_2; // версия изменения
        private Int32 blob_version_1; // версия реструктуризации
        private Int32 blob_version_2; // версия изменения
        private Int32 index_version_1; // версия реструктуризации
        private Int32 index_version_2; // версия изменения
        private Int32 descr_version_1; // версия реструктуризации
        private Int32 descr_version_2; // версия изменения

        public bool Has_data { get { return has_data; } set { has_data = value; } }
        public bool Has_blob { get { return has_blob; } set { has_blob = value; } }

        public bool Has_index { get { return has_index; } set { has_index = value; } }

        public bool Has_descr { get { return has_descr; } set { has_descr = value; } }

        public int Data_version_1 { get { return data_version_1; } set { data_version_1 = value; } }
        public int Data_version_2 { get { return data_version_2; } set { data_version_2 = value; } }

        public int Blob_version_1 { get { return blob_version_1; } set { blob_version_1 = value; } }
        public int Blob_version_2 { get { return blob_version_2; } set { blob_version_2 = value; } }

        public int Index_version_1 { get { return index_version_1; } set { index_version_1 = value; } }
        public int Index_version_2 { get { return index_version_2; } set { index_version_2 = value; } }

        public int Descr_version_1 { get { return descr_version_1; } set { descr_version_1 = value; } }
        public int Descr_version_2 { get { return descr_version_2; } set { descr_version_2 = value; } }
    };


    /// <summary>
    /// Класс таблиц
    /// </summary>
    public class V8Table
    {
        public static readonly UInt32 BLOB_RECORD_LEN = 256;
        public static readonly UInt32 BLOB_RECORD_DATA_LEN = 250;

        // структура изменной записи таблицы
        public class Changed_rec
        {

            // владелец
            private V8Table parent;

            // физический номер записи (для добавленных записей нумерация начинается с phys_numrecords)
            private UInt32 numrec;

            // тип изменения записи (изменена, добавлена, удалена)
            private Changed_rec_type changed_type;

            // следующая измененная запись в списке измененных записей
            private Changed_rec next;

            // массив признаков изменения поля (по одному байту на каждое поле, всего num_fields байт)
            private char[] fields;

            // измененная запись. Для типов полей tf_text (TEXT), tf_string (MEMO) и tf_image (BLOB), если соответствующий признак в fields установлен,
            // содержит указатель на TStream с содержимым поля (или NULL)
            private char[] rec;

            public V8Table Parent { get { return parent; } set { parent = value; } }

            public uint Numrec { get { return numrec; } set { numrec = value; } }

            public Changed_rec_type Changed_type { get { return changed_type; } set { changed_type = value; } }

            public Changed_rec Next { get { return next; } set { next = value; } }

            public char[] Fields { get { return fields; } set { fields = value; } }

            public char[] Rec { get { return rec; } set { rec = value; } }

            public Changed_rec(V8Table _parent, Changed_rec_type crt, UInt32 phys_numrecord)
            {
                Parent = _parent;
                Numrec = phys_numrecord;
                Changed_type = crt;
                if (crt == Changed_rec_type.deleted)
                {
                    Fields = null;
                    Rec = null;
                }
                else
                {
                    Fields = new char[Parent.num_fields];
                    //memset(fields, 0, parent.num_fields);
                    Array.Clear(Fields, 0, Fields.Length);
                    Rec = new char[Parent.recordlen];
                    //memset(rec, 0, parent.recordlen);
                    Array.Clear(Rec, 0, Rec.Length);
                }
                Next = Parent.ch_rec;
                Parent.ch_rec = this;

            }

            public void Clear()
            {
                Int32 i;
                V8Field f;
                Type_fields tf;
                Stream b = null;

                if (Rec != null && Fields != null)
                    for (i = 0; i < Parent.num_fields; i++)
                        if (!String.IsNullOrEmpty(Fields[i].ToString()))
                        {
                            f = Parent.fields[i];
                            tf = f.Gettype();
                            if (tf == Type_fields.tf_image || tf == Type_fields.tf_string || tf == Type_fields.tf_text)
                            {
                                //b = *(TStream**)(rec + f.getoffset() + (f.getnull_exists() ? 1 : 0));
                                // b.Dispose(); - что-то непонятное здесь
                            }
                        }

            }
        }


        #region Поддержка динамического построения таблицы записей

        private UInt32[] recordsindex;      // массив индексов записей по номеру (только не пустые записи)
        private bool recordsindex_complete; // признак заполнености recordsindex
        private UInt32 numrecords_review;   // количество просмотренных записей всего в поиске не пустых
        private UInt32 numrecords_found;    // количество найденных непустых записей (текущий размер recordsindex)

        public uint[] Recordsindex { get { return recordsindex; } set { recordsindex = value; } }
        public bool Recordsindex_complete { get { return recordsindex_complete; } set { recordsindex_complete = value; } }
        public uint Numrecords_review { get { return numrecords_review; } set { numrecords_review = value; } }
        public uint Numrecords_found { get { return numrecords_found; } set { numrecords_found = value; } }

        public T_1CD Base_ { get { return base_; } set { base_ = value; } }

        public string Name { get { return name; } set { name = value; } }

        public V8object File_index { get { return file_index; } set { file_index = value; } }

        public uint Log_numrecords { get { return log_numrecords; } set { log_numrecords = value; } }

        public bool Bad { get { return bad; } set { bad = value; } }



        #endregion

        /// <summary>
        /// заполнить recordsindex не динамически
        /// </summary>
        public void Fillrecordsindex()
        {
            UInt32 i;
            Int32 j;
            byte[] rec;

            if (Recordsindex_complete)
                return;
            Recordsindex = new UInt32[phys_numrecords];
            rec = new byte[recordlen];

            j = 0;
            for (i = 0; i < phys_numrecords; i++)
            {
                Getrecord(i, rec);
                if (rec != null)
                    continue;
                Recordsindex[j++] = i;
            }
            Recordsindex_complete = true;
            Numrecords_review = phys_numrecords;
            Numrecords_found = (UInt32)j;
            Log_numrecords = (UInt32)j;

            rec = null;

        }

        /// <summary>
        /// Конструктора
        /// </summary>
        public V8Table()
        {
            Init();
        }

        public V8Table(T_1CD _base, Int32 block_descr)
        {
            Base_ = _base;

            descr_table = new V8object(Base_, block_descr);
            description = System.Text.Encoding.UTF8.GetString(descr_table.Getdata(), 0, descr_table.Getdata().Length);

            Init(block_descr);
        }

        public V8Table(T_1CD _base, String _descr, Int32 block_descr = 0)
        {
            Base_ = _base;

            descr_table = null;
            description = _descr;

            Init(block_descr);
        }

        /// <summary>
        /// Инициализация без параметров
        /// </summary>
        public void Init()
        {
            num_fields = 0;
            //fields.Clear();
            fields = new List<V8Field>();
            num_indexes = 0;
            indexes = null;
            recordlock = false;
            file_data = null;
            file_blob = null;
            File_index = null;
            lockinmemory = 0;

            Recordsindex_complete = false;
            Numrecords_review = 0;
            Numrecords_found = 0;
            Recordsindex = null;

            edit = false;
            ch_rec = null;
            added_numrecords = 0;

            phys_numrecords = 0;
            Log_numrecords = 0;
            Bad = true;

        }

        /// <summary>
        /// Инициализация по описанию
        /// </summary>
        /// <param name="block_descr"></param>
        public void Init(Int32 block_descr)
        {
            Tree t;
            Tree f;
            Tree in_;
            Tree rt;
            Int32 i, j, k;
            UInt32 m;
            UInt64 s;
            V8Index ind;
            Int32 numrec;
            Int32[] blockfile = new Int32[3];
            V8Field fld;
            //UInt32[] buf = new UInt32[num_indexes + 1];
            //byte[] buf = new byte[num_indexes + 1];
            byte[] buf = new byte[PAGE8K];


            Init();

            if ( String.IsNullOrEmpty(description))
                return;

            
            Tree root = Tree.Parse_1Ctext(description, "Блок " + block_descr);

            if (root == null)
            {
                Console.WriteLine($"Ошибка разбора текста описания таблицы. Блок {block_descr}");
                Init();
                return;
            }

            if (root.Get_num_subnode() != 1)
            {
                Console.WriteLine($"Ошибка разбора текста описания таблицы. Количество узлов не равно 1. Блок {block_descr}, Узлов {root.Get_num_subnode()}");
                Init();
                root = null;
                return;
            }

            rt = root.Get_first();

            if (rt.Get_num_subnode() != 6)
            {
                Console.WriteLine($"Ошибка разбора текста описания таблицы. Количество узлов не равно 6. Блок {block_descr}, Узлов {rt.Get_num_subnode()}");
                Init();
                root = null;
                return;
            }

            t = rt.Get_first();
            if (t.Get_type() != Node_type.nd_string)
            {
                Console.WriteLine($"Ошибка получения имени таблицы. Узел не является строкой. Блок, {block_descr}");
                Init();
                root = null;
                return;
            }
            Name = t.Get_value();

            issystem =
                Name[1] != '_' ||
                Name.Substring(Name.Length - 6, 7).Contains("STORAGE") ||
                Name.Contains("_SYSTEMSETTINGS") ||
                Name.Contains("_COMMONSETTINGS") ||
                Name.Contains("_REPSETTINGS") ||
                Name.Contains("_REPVARSETTINGS") ||
                Name.Contains("_FRMDTSETTINGS") ||
                Name.Contains("_SCHEDULEDJOBS");

            t = t.Get_next();
            // пропускаем узел, так как там всегда содержится "0", и что это такое, неизвестно (версия формата описания таблиц?)
            t = t.Get_next();
            if (t.Get_type() != Node_type.nd_list)
            {
                Console.WriteLine($"Ошибка получения полей таблицы. Узел не является деревом. Блок, {block_descr}, Таблица {Name}");
                Init();
                root = null;
                return;
            }
            if (t.Get_num_subnode() < 2)
            {
                Console.WriteLine($"Ошибка получения полей таблицы. Нет узлов описания полей. Блок, {block_descr}, Таблица {Name}");
                Init();
                root = null;
                return;
            }

            num_fields = t.Get_num_subnode() - 1;
            //fields.resize(num_fields);
            //fields.Capacity = num_fields;
            bool has_version = false; // признак наличия поля версии

            f = t.Get_first();
            if (f.Get_type() != Node_type.nd_string)
            {
                Console.WriteLine($"Ошибка получения полей таблицы. Ожидаемый узел Fields не является строкой. Блок, {block_descr}, Таблица {Name}");
                Deletefields();
                Init();
                root = null;
                return;
            }

            if (f.Get_value() != "Fields")
            {
                Console.WriteLine($"Ошибка получения полей таблицы. Узел не Fields. Блок, {block_descr}, Таблица {Name}, Узел {f.Get_value()}");
                Deletefields();
                Init();
                root = null;
                return;
            }

            for (i = 0; i < num_fields; i++)
            {
                f = f.Get_next();
                if (f.Get_num_subnode() != 6)
                {
                    Console.WriteLine($"Ошибка получения узла очередного поля таблицы. Количество узлов поля не равно 6. Блок, {block_descr}, Таблица {Name}, Номер поля {i + 1}, Узел {f.Get_num_subnode()}");
                    Deletefields();
                    Init();
                    root = null;
                    return;
                }

                Tree field_tree = f.Get_first();
                try
                {

                    fields.Add(V8Field.Field_from_tree(field_tree, ref has_version, this));


                    //fields[i] = v8Field.field_from_tree(field_tree, ref has_version, this);

                }
                catch
                {
                    Deletefields();
                    Init();
                    root = null;
                    Console.WriteLine($"Блок, {block_descr}, Таблица {Name}, Номер поля {i + 1}");
                    return;
                }
            }
            t = t.Get_next();
            if (t.Get_type() != Node_type.nd_list)
            {
                Console.WriteLine($"Ошибка получения индексов таблицы. Узел не является деревом. Блок, {block_descr}, Таблица {Name}");
                Deletefields();
                Init();
                root = null;
                return;
            }
            if (t.Get_num_subnode() < 1)
            {
                Console.WriteLine($"Ошибка получения индексов таблицы. Нет узлов описания индексов. Блок, {block_descr}, Таблица {Name}");
                Deletefields();
                Init();
                root = null;
                return;
            }

            num_indexes = t.Get_num_subnode() - 1;

            if (num_indexes != 0)
            {
                indexes = new V8Index[num_indexes];
                for (i = 0; i < num_indexes; i++)
                    indexes[i] = new V8Index(this);

                f = t.Get_first();
                if (f.Get_type() != Node_type.nd_string)
                {
                    Console.WriteLine($"Ошибка получения индексов таблицы. Ожидаемый узел Indexes не является строкой. Блок, {block_descr}, Таблица {Name}");
                    Deletefields();
                    Deleteindexes();
                    Init();
                    root = null;
                    return;
                }

                if (f.Get_value() != "Indexes")
                {
                    Console.WriteLine($"Ошибка получения индексов таблицы. Узел не Indexes. Блок, {block_descr}, Таблица {Name}, Узел {f.Get_value()}");
                    Deletefields();
                    Deleteindexes();
                    Init();
                    root = null;
                    return;
                }
                for (i = 0; i < num_indexes; i++)
                {
                    f = f.Get_next();
                    numrec = f.Get_num_subnode() - 2;
                    if (numrec < 1)
                    {
                        Console.WriteLine($"Ошибка получения очередного индекса таблицы. Нет узлов описаня полей индекса. Блок, {block_descr}, Таблица {Name}, Номер индекса {i+1}");
                        Deletefields();
                        Deleteindexes();
                        Init();
                        root = null;
                        return;
                    }
                    ind = indexes[i];
                    ind.Num_records = numrec;

                    if (f.Get_type() != Node_type.nd_list)
                    {
                        Console.WriteLine($"Ошибка получения очередного индекса таблицы. Узел не является деревом. Блок, {block_descr}, Таблица {Name}, Номер индекса {i + 1}");
                        Deletefields();
                        Deleteindexes();
                        Init();
                        root = null;
                        return;
                    }
                    Tree index_tree = f.Get_first();
                    if (index_tree.Get_type() != Node_type.nd_string)
                    {
                        Console.WriteLine($"Ошибка получения очередного индекса таблицы. Узел не является строкой. Блок, {block_descr}, Таблица {Name}, Номер индекса {i + 1}");
                        Deletefields();
                        Deleteindexes();
                        Init();
                        root = null;
                        return;
                    }
                    ind.Name = index_tree.Get_value();

                    index_tree = index_tree.Get_next();
                    if (index_tree.Get_type() != Node_type.nd_number)
                    {
                        Console.WriteLine($"Ошибка получения очередного индекса таблицы. Узел не является строкой. Блок, {block_descr}, Таблица {Name}, Номер индекса {ind.Name}");
                        Deletefields();
                        Deleteindexes();
                        Init();
                        root = null;
                        return;
                    }

                    String sIsPrimaryIndex = index_tree.Get_value();
                    if (sIsPrimaryIndex == "0")
                        ind.Is_primary = false;
                    else if (sIsPrimaryIndex == "1")
                        ind.Is_primary = true;
                    else
                    {
                        Console.WriteLine($"Неизвестный тип индекса таблицы. Блок, {block_descr}, Таблица {Name}, Индекс {ind.Name}, Тип индекса {sIsPrimaryIndex}");
                        Deletefields();
                        Deleteindexes();
                        Init();
                        root = null;
                        return;
                    }

                    ind.Records = new Index_record[numrec];
                    for (j = 0; j < numrec; j++)
                    {
                        index_tree = index_tree.Get_next();
                        if (index_tree.Get_num_subnode() != 2)
                        {
                            Console.WriteLine($"Ошибка получения очередного поля индекса таблицы. Количество узлов поля не равно 2. Блок, {block_descr}, Таблица {Name}, Индекс {ind.Name}, Номер поля индекса {j + 1}, Узлов {index_tree.Get_num_subnode()}");
                            Deletefields();
                            Deleteindexes();
                            Init();
                            root = null;
                            return;
                        }

				        in_ = index_tree.Get_first();
                        if (in_.Get_type() != Node_type.nd_string)
				        {
                            Console.WriteLine($"Ошибка получения имени поля индекса таблицы. Узел не является строкой. Блок, {block_descr}, Таблица {Name}, Индекс {ind.Name}, Номер поля индекса {j + 1}");
                            Deletefields();
                            Deleteindexes();
                            Init();
                            root = null;
                            return;
                        }
                        String field_name = in_.Get_value();
                        for (k = 0; k < num_fields; k++)
                        {
                            if (fields[k].Name == field_name)
                            {
                                ind.Records[j].Field = fields[k];
                                break;
                            }
                        }

                        if (k >= num_fields)
                        {
                            Console.WriteLine($"Ошибка получения индекса таблицы. Не найдено поле таблицы по имени поля индекса. Блок, {block_descr}, Таблица {Name}, Индекс {ind.Name}, Поле индекса {field_name}");
                            Deletefields();
                            Deleteindexes();
                            Init();
                            root = null;
                            return;
                        }

				        in_ = in_.Get_next();

                        if (in_.Get_type() != Node_type.nd_number)
                        {
                            Console.WriteLine($"Ошибка получения длины поля индекса таблицы. Узел не является числом. Блок, {block_descr}, Таблица {Name}, Индекс {ind.Name}, Поле индекса {field_name}");
                            Deletefields();
                            Deleteindexes();
                            Init();
                            root = null;
                            return;
                        }
                        ind.Records[j].Len = Convert.ToInt32( in_.Get_value(), 10);




                    }

                }

            }
            else
            {
                indexes = null;
            }

            t = t.Get_next();
            if (t.Get_num_subnode() != 2)
            {
                Console.WriteLine($"Ошибка получения типа блокировки таблицы. Количество узлов не равно 2. Блок, {block_descr}, Таблица {Name}");
                Deletefields();
                Deleteindexes();
                Init();
                root = null;
                return;
            }

            f = t.Get_first();
            if (f.Get_type() != Node_type.nd_string)
            {
                Console.WriteLine($"Ошибка получения типа блокировки таблицы. Ожидаемый узел Recordlock не является строкой. Блок, {block_descr}, Таблица {Name}");
                Deletefields();
                Deleteindexes();
                Init();
                root = null;
                return;
            }

            if (f.Get_value() != "Recordlock")
            {
                Console.WriteLine($"Ошибка получения типа блокировки таблицы. Узел не Recordlock. Блок, {block_descr}, Таблица {Name}, Узел {f.Get_value()}");
                Deletefields();
                Deleteindexes();
                Init();
                root = null;
                return;
            }

            f = f.Get_next();
            if (f.Get_type() != Node_type.nd_string)
            {
                Console.WriteLine($"Ошибка получения типа блокировки таблицы. Узел не является строкой. Блок, {block_descr}, Таблица {Name}");
                Deletefields();
                Deleteindexes();
                Init();
                root = null;
                return;
            }
            String sTableLock = f.Get_value();
            if (sTableLock == "0")
                recordlock = false;
            else if (sTableLock == "1")
                recordlock = true;
            else
            {
                Console.WriteLine($"Неизвестное значение типа блокировки таблицы. Блок, {block_descr}, Таблица {Name}, Тип блокировки {sTableLock}");
                Deletefields();
                Deleteindexes();
                Init();
                root = null;
                return;
            }

            if (recordlock && !has_version)
            {// добавляем скрытое поле версии
                fld = new V8Field(this);
                fld.Name = "VERSION";
                fld.Type_manager = FieldType.Version8();
                //fields.push_back(fld);
                fields.Add(fld);
            }

            t = t.Get_next();
            if (t.Get_num_subnode() != 4)
            {
                Console.WriteLine($"Ошибка получения файлов таблицы. Количество узлов не равно 4. Блок, {block_descr}, Таблица {Name}");
                Deletefields();
                Deleteindexes();
                Init();
                root = null;
                return;
            }

            f = t.Get_first();
            if (f.Get_type() != Node_type.nd_string)
            {
                Console.WriteLine($"Ошибка получения файлов таблицы. Ожидаемый узел Files не является строкой. Блок, {block_descr}, Таблица {Name}");
                Deletefields();
                Deleteindexes();
                Init();
                root = null;
                return;
            }

            if (f.Get_value() != "Files")
            {
                Console.WriteLine($"Ошибка получения файлов таблицы. Узел не Files. Блок, {block_descr}, Таблица {Name}, Узел {f.Get_value()}");
                Deletefields();
                Deleteindexes();
                Init();
                root = null;
                return;
            }

            for (i = 0; i < 3; i++)
            {
                f = f.Get_next();
                if (f.Get_type() != Node_type.nd_number)
                {
                    Console.WriteLine($"Ошибка получения файлов таблицы. Узел не является числом. Блок, {block_descr}, Таблица {Name}, Номер файла {i + 1}");
                    Deletefields();
                    Deleteindexes();
                    Init();
                    root = null;
                    return;
                }
                blockfile[i] = Convert.ToInt32(f.Get_value());
            }

            root = null;


            file_data  = (blockfile[0] != 0) ? new V8object(Base_, blockfile[0]) : null;
            file_blob  = (blockfile[1] != 0) ? new V8object(Base_, blockfile[1]) : null;
            File_index = (blockfile[2] != 0) ? new V8object(Base_, blockfile[2]) : null;

            if (num_indexes !=0  && File_index == null)
            {
                Console.WriteLine($"В таблице есть индексы, однако файл индексов отсутствует. Блок, {block_descr}, Таблица {Name}, Количество индексов {num_indexes}");
            }
            else if (num_indexes == 0 && File_index != null)
            {
                Console.WriteLine($"В таблице нет индексов, однако присутствует файл индексов. Блок, {block_descr}, Таблица {Name}, Блок индексов {blockfile[2]}");
            }
            else if (File_index != null)
            {
                m = (UInt32)File_index.Getlen() / Base_.Pagesize;

                if (File_index.Getlen() != m * Base_.Pagesize)
                {
                    Console.WriteLine($"Ошибка чтения индексов. Длина файла индексов не кратна размеру страницы. Таблица {Name}, Длина файла индексов {File_index.Getlen()}");
                }
                else
                {
                    Int32 buflen = num_indexes * 4 + 4;
                    //buf = new UInt32[num_indexes + 1];
                    File_index.Getdata(buf, 0, (UInt32)buflen);

                    //			// Временно, для отладки >>
                    //			if(buf[0]) msreg_g.AddMessage_("Существуют свободные страницы в файле индексов", MessageState::Hint,
                    //					"Таблица", name,
                    //					"Индекс свободной страницы", to_hex_string(buf[0]));
                    //			// Временно, для олтладки <<

                    if (buf[0] * Base_.Pagesize >= File_index.Getlen())
                    {
                        Console.WriteLine($"Ошибка чтения индексов. Индекс первого свободного блока за пределами файла индексов. Таблица {Name}, Длина файла индексов {File_index.Getlen()}, Индекс свободной страницы {buf[0]}");
                    }
                    else
                    {
                        for (i = 1; i <= num_indexes; i++)
                        {
                            if ((int)Base_.Version < (int)Db_ver.ver8_3_8_0)
                            {
                                if (buf[i] >= File_index.Getlen())
                                {
                                    Console.WriteLine($"Ошибка чтения индексов. Указанное смещение индекса за пределами файла индексов. Таблица {Name}, Длина файла индексов {File_index.Getlen()}, Номер индекса { i }, Смещение индекса {buf[i]}");
                                }
                                else if ((buf[i] & 0xfff) != 0)
                                {
                                    Console.WriteLine($"Ошибка чтения индексов. Указанное смещение индекса не кратно 4 Кб. Таблица {Name}, Длина файла индексов {File_index.Getlen()}, Номер индекса { i }, Смещение индекса {buf[i]}");
                                }
                                else
                                    indexes[i - 1].Start = buf[i];
                            }
                            else
                            {
                                s = buf[i];
                                s *= Base_.Pagesize;
                                if (s >= File_index.Getlen())
                                {
                                    Console.WriteLine($"Ошибка чтения индексов. Указанное смещение индекса за пределами файла индексов. Таблица {Name}, Длина файла индексов {File_index.Getlen()}, Номер индекса { i }, Смещение индекса { s }");
                                }
                                else
                                    indexes[i - 1].Start = s;
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
                if (fields[i].Type_manager.Gettype() == Type_fields.tf_version || fields[i].Type_manager.Gettype() == Type_fields.tf_version8)
                {
                    fields[i].Offset = recordlen;
                    recordlen += fields[i].Getlen();
                }
            }

            // затем идут все остальные поля
            for (i = 0; i < num_fields; i++)
            {
                if (fields[i].Type_manager.Gettype() != Type_fields.tf_version && fields[i].Type_manager.Gettype() != Type_fields.tf_version8)
                {
                    fields[i].Offset = recordlen;
                    recordlen += fields[i].Getlen();
                }
            }
            if (recordlen < 5) recordlen = 5; // Длина одной записи не может быть меньше 5 байт (1 байт признак, что запись свободна, 4 байт - индекс следующей следующей свободной записи)

            if (recordlen == 0  || file_data == null)
                phys_numrecords = 0;
            else
                phys_numrecords = (UInt32)file_data.Getlen() / (UInt32)recordlen; ;

            if (file_data != null )
            {
                if (phys_numrecords * (UInt32)recordlen != file_data.Getlen())
                {
                    Console.WriteLine($"Длина таблицы не кратна длине записи. Блок {block_descr} Таблица {Name}, Длина таблицы {file_data.Getlen()}, Длина записи { recordlen }");
                }
            }
            else
            {
                Console.WriteLine($"Отсутствует файл данных таблицы. Блок {block_descr} Таблица {Name}");
                return;
            }

            // Инициализация данных индекса
            for (i = 0; i < num_indexes; i++)
                indexes[i].Get_length();

            Console.WriteLine($"Создана таблица. Таблица {Name} Длина таблицы {file_data.Getlen()}, длина записи {recordlen}");

            Bad = false;

        }// окончание процедуры

        /// <summary>
        /// Получить имя
        /// </summary>
        /// <returns></returns>
        public String Getname()
        {
            return Name;
        }

        public String Getdescription()
        {
            return description;
        }

        public Int32 Get_numfields()
        {
            return num_fields;
        }

        public Int32 Get_numindexes()
        {
            return num_indexes;
        }

        public V8Field Getfield(Int32 numfield)
        {
            if (numfield >= num_fields)
            {
                Console.WriteLine($"Попытка получения поля таблицы по номеру, превышающему количество полей. Таблица {Name} Количество полей {num_fields} Номер поля {numfield + 1}");
                return null;
            }
            return fields[numfield];
        }

        public V8Index Getindex(Int32 numindex)
        {
            if (numindex >= num_indexes)
            {
                Console.WriteLine($"Попытка получения индекса таблицы по номеру, превышающему количество индексов. Таблица {Name} Количество индексов {num_indexes} Номер индекса {numindex + 1}");
                return null;
            }
            return indexes[numindex];
        }

        public bool Get_issystem()
        {
            return issystem;
        }

        public Int32 Get_recordlen()
        {
            return recordlen;
        }

        public bool Get_recordlock()
        {
            return recordlock;
        }

        /// <summary>
        /// возвращает количество записей в таблице всего, вместе с удаленными
        /// </summary>
        /// <returns></returns>
        public UInt32 Get_phys_numrecords()
        {
            return phys_numrecords;
        }

        /// <summary>
        /// возвращает количество записей в таблице всего, без удаленных
        /// </summary>
        /// <returns></returns>
        public UInt32 Get_log_numrecords()
        {
            return Log_numrecords;
        } 

        public void Set_log_numrecords(UInt32 _log_numrecords)
        {
            Log_numrecords = _log_numrecords;
        } //

        public UInt32 Get_added_numrecords()
        {
            return added_numrecords;
        }

        /// <summary>
        /// возвращает указатель на запись, буфер принадлежит вызывающей процедуре
        /// </summary>
        /// <param name="phys_numrecord"></param>
        /// <param name="buf"></param>
        /// <returns></returns>
        public byte[] Getrecord(UInt32 phys_numrecord, byte[] buf)
        {
            return file_data.Getdata(buf, (UInt32)phys_numrecord * (UInt32)recordlen, (UInt32)recordlen);
        } 

        public Stream ReadBlob(Stream _str, UInt32 _startblock, UInt32 _length, bool rewrite = true)
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
                Console.WriteLine($"Попытка чтения нулевого блока файла Blob. Таблица {Name}");
                return _str;
            }

            if (file_blob != null)
            {
                _filelen = (UInt32)file_blob.Getlen();
                _numblock = (UInt32)_filelen >> 8;
                if (_numblock << 8 != _filelen)
                {
                    Console.WriteLine($"Длина файла Blob не кратна 0x100. Таблица {Name}. Длина файла {_filelen}");
                }

                _curb = new byte[0x100];
                _curblock = _startblock;

                while (_curblock != 0)
                {
                    if (_curblock >= _numblock)
                    {
                        Console.WriteLine($"Попытка чтения блока файла Blob за пределами файла. Таблица {Name}. Всего блоков {_numblock}. Читаемый блок {_curblock}");
                        return _str;
                    }
                    file_blob.Getdata(_curb, _curblock << 8, 0x100);

                    _curblock = (UInt32)_curb[0];

                    _curlen = _curb[4];

                    if (_curlen > 0xfa)
                    {
                        Console.WriteLine($"Попытка чтения из блока файла Blob более 0xfa байт. Таблица {Name}. Индекс блока {_curblock}. Читаемый байт {_curlen}");
                        return _str;
                    }
                    _str.Write(_curb, 6, _curlen);

                    if (_str.Length - startlen > _length)
                        break; // аварийный выход из возможного ошибочного зацикливания

                }
                _curb = null;

                if (_str.Length - startlen != _length)
                {
                    Console.WriteLine($"Несовпадение длины Blob-поля, указанного в записи, с длиной практически прочитанных данных. Таблица {Name}. Длина поля {_length}. Прочитано {_str.Length - startlen}");
                }
            }
            else
            {
                Console.WriteLine($"Попытка чтения Blob-поля при отстутствующем файле Blob. Таблица {Name}. Длина поля {_length}");
            }

            return _str;
        }

        public UInt32 ReadBlob(byte[] buf, UInt32 _startblock, UInt32 _length)
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
                Console.WriteLine($"Попытка чтения нулевого блока файла Blob. Таблица {Name}");
                return 0;
            }
            
            Array.Copy(buf, 0, _buf, 0, buf.Length);
            readed = 0;
            if (file_blob != null)
            {
                _filelen = (UInt32)file_blob.Getlen();
                _numblock = _filelen >> 8;
                if (_numblock << 8 != _filelen)
                {
                    Console.WriteLine($"Длина файла Blob не кратна 0x100. Таблица {Name}. Длина файла {_filelen}");
                }

                byte[] _curb = new byte[0x100];
                _curblock = _startblock;

                while (_curblock != 0)
                {
                    if (_curblock >= _numblock)
                    {
                        Console.WriteLine($"Попытка чтения блока файла Blob за пределами файла. Таблица {Name}. Всего блоков {_numblock}. Читаемый блок {_curblock}");
                        return readed;
                    }
                    file_blob.Getdata(_curb, _curblock << 8, BLOB_RECORD_LEN);
                    _curblock = _curb[0];
                    _curlen = _curb[4];
                    if (_curlen > BLOB_RECORD_DATA_LEN)
                    {
                        Console.WriteLine($"Попытка чтения из блока файла Blob более 0xfa байт. Таблица {Name}. Индекс блока {_curblock}. Читаемый байт {_curlen}");
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
                    Console.WriteLine($"Несовпадение длины Blob-поля, указанного в записи, с длиной практически прочитанных данных. Таблица {Name}. Длина поля {_length}. Прочитано {readed}");
                }


            }
            else
            {
                Console.WriteLine($"Попытка чтения Blob-поля при отстутствующем файле Blob. Таблица {Name}. Длина поля {_length}");
            }

            return readed;
        }

        public void Set_lockinmemory(bool _lock) { }

        public bool Export_to_xml(String filename, bool blob_to_file, bool unpack) { return true; }

        public V8object Get_file_data()
        {
            return file_data;
        }

        public V8object Get_file_blob()
        {
            return file_blob;
        }

        public V8object Get_file_index()
        {
            return File_index;
        }

        /// <summary>
        /// получить физическое смещение в файле записи по номеру
        /// </summary>
        /// <param name="phys_numrecord"></param>
        /// <returns></returns>
        public UInt64 Get_fileoffset(UInt32 phys_numrecord)
        {
            return file_data.Get_fileoffset(phys_numrecord * (UInt32)recordlen);
        }

        /// <summary>
        /// Возвращает указатель на запись, буфер принадлежит вызывающей процедуре
        /// </summary>
        /// <param name="phys_numrecord"></param>
        /// <param name="buf"></param>
        /// <returns></returns>
        public byte[] Get_edit_record(UInt32 phys_numrecord, byte[] rec)
        {
            Changed_rec cr;
            for (cr = ch_rec; cr != null; cr = cr.Next)
                if (phys_numrecord == cr.Numrec)
                {
                    if (cr.Changed_type != Changed_rec_type.deleted)
                    {
                        Array.Copy(cr.Rec, rec, recordlen);
                        return rec;
                    }
                    break;
                }
            return Getrecord(phys_numrecord, rec);
        } 

        public bool Get_edit()
        {
            return edit;
        }

        /// <summary>
        /// Получить физический индекс записи по номеру строки по указанному индексу
        /// </summary>
        /// <param name="ARow"></param>
        /// <param name="cur_index"></param>
        /// <returns></returns>
        public UInt32 Get_phys_numrec(Int32 ARow, V8Index cur_index)
        {

            UInt32 numrec;

            if (ARow == 0)
            {
                Console.WriteLine($"Попытка получения номера физической записи по нулевому номеру строки. Таблица {Name}");
                return 0;
            }

            if (edit)
            {
                if (ARow > Log_numrecords + added_numrecords)
                {
                    Console.WriteLine($"Попытка получения номера физической записи по номеру строки, превышающему количество записей. Таблица {Name}" +
                        $"Количество логических записей {Log_numrecords}, Количество добавленных записей {added_numrecords}, Номер строки {ARow}");
                    return 0;
                }
                if (ARow > Log_numrecords)
                    return (UInt32)ARow - 1 - Log_numrecords + phys_numrecords;
            }

            if (ARow > Log_numrecords)
            {
                Console.WriteLine($"Попытка получения номера физической записи по номеру строки, превышающему количество записей. Таблица {Name}, Количество логических записей {Log_numrecords}, Номер строки {ARow}");
                return 0;
            }
            if (cur_index != null)
                numrec = cur_index.Get_numrec((UInt32)ARow - 1);
            else
            {
                /* для чего-то это нужно
                # ifndef getcfname
                    tr_syn->BeginRead();
                #endif
                */
                numrec = Recordsindex[ARow - 1];
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
        public String Get_file_name_for_field(Int32 num_field, byte[] rec, UInt32 numrec = 0)
        {
            String s = "";
            Int32 i;
            V8Index ind;

            if (num_indexes != 0)
            {
                ind = indexes[0];
                for (i = 0; i < num_indexes; i++)
                    if (indexes[i].Is_primary)
                    {
                        ind = indexes[i];
                        break;
                    }
                for (i = 0; i < ind.Num_records; i++)
                {
                    if (s.Length != 0)
                        s += "_";
                    s += ind.Records[i].Field.Get_XML_presentation(rec);
                }
                if (!ind.Is_primary && numrec != 0)
                {
                    s += "_";
                    s += numrec;
                }
            }

            if (!issystem || String.Compare(Name, "CONFIG") == 0 || String.Compare(Name, "CONFIGSAVE") == 0 || String.Compare(Name, "FILES") == 0 || String.Compare(Name, "PARAMS") == 0)
            {
                if (s.Length != 0)
                    s += "_";
                s += fields[num_field].Getname();
            }
            return s;

            
        } 

        /// <summary>
        /// получить имя файла по-умолчанию конкретной записи
        /// </summary>
        /// <param name="rec"></param>
        /// <returns></returns>
        public String Get_file_name_for_record(byte[] rec)
        {
            String s = "";

            Int32 i;
            Int32 num_rec;

            V8Index ind;

            if (num_indexes != 0)
            {
                ind = indexes[0];
                for (i = 0; i < num_indexes; i++)
                {

                    if (indexes[i].Is_primary)
                    {
                        ind = indexes[i];
                        break;
                    }
                }
                num_rec = ind.Num_records;

                for (i = 0; i < num_rec; i++)
                {
                    if (s.Length != 0)
                    {
                        s += "_";
                    }
                    V8Field tmp_field = ind.Records[i].Field;
                    String tmp_str = tmp_field.Get_XML_presentation(rec);

                    s += tmp_str;

                }
            }

            return s;
        } 

        public T_1CD Getbase() { return Base_; }

        public void Begin_edit() { } // переводит таблицу в режим редактирования

        public void Cancel_edit() { } // переводит таблицу в режим просмотра и отменяет все изменения

        public void End_edit() { } // переводит таблицу в режим просмотра и сохраняет все изменения

        public Changed_rec_type Get_rec_type(UInt32 phys_numrecord)
        {
            Changed_rec cr;
            if (!edit)
            {
                return Changed_rec_type.not_changed;
            }
            cr = ch_rec;
            while (cr != null)
            {
                if (cr.Numrec == phys_numrecord)
                    return cr.Changed_type;
                cr = cr.Next;
            }
            return Changed_rec_type.not_changed;
        }

        public Changed_rec_type Get_rec_type(UInt32 phys_numrecord, Int32 numfield)
        {
            Changed_rec cr;
            if (!edit)
            {
                return Changed_rec_type.not_changed;
            }
            cr = ch_rec;
            while (cr != null)
            {
                if (cr.Numrec == phys_numrecord)
                {
                    if (cr.Changed_type == Changed_rec_type.changed)
                    {
                        return cr.Fields[numfield] != '0' ? Changed_rec_type.changed : Changed_rec_type.not_changed;
                    }
                    return cr.Changed_type;
                }
                cr = cr.Next;
            }
            return Changed_rec_type.not_changed;
        }

        public void Set_edit_value(UInt32 phys_numrecord, Int32 numfield, bool Null, String value, Stream st = null) { }

        public void Restore_edit_value(UInt32 phys_numrecord, Int32 numfield) { }

        public void Set_rec_type(UInt32 phys_numrecord, Changed_rec_type crt) { }

        public void Export_table(String path) { }

        public void Import_table(String path) { }

        public void Delete_record(UInt32 phys_numrecord) { } // удаление записи

        public void Insert_record(char[] rec) { } // добавление записи

        public void Update_record(UInt32 phys_numrecord, char[] rec, char[] changed_fields) { } // изменение записи

        public byte[] Get_record_template_test()
        {
            Int32 len;
            byte[] res;
            byte[] curp;
            Int32 i, j, l;
            V8Field f;
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
                Array.Copy(res, f.Getoffset() << 8, curp, 0, res.Length);

                if (f.Getnull_exists())
                {
                    curp[0] = 1;
                    curp[1] = 1;
                    // похоже пробегаем весь массив с шагом BLOB_RECORD_LEN
                    //curp += BLOB_RECORD_LEN; 
                }

                l = f.Getlength();
                switch (f.Gettype())
                {
                    case Type_fields.tf_binary: // B // длина = length
                        //memset(curp, 1, BLOB_RECORD_LEN * l);
                        for (int ii = 0; ii < curp.Length; ii++)
                        {
                            curp[ii] = 1;
                        }
                        break;
                    case Type_fields.tf_bool: // L // длина = 1
                        curp[0] = 1;
                        curp[1] = 1;
                        break;
                    case Type_fields.tf_numeric: // N // длина = (length + 2) / 2
                        j = (l + 2) / 2;
                        for (; j > 0; --j)
                        {
                            /* пока не понятна
                            memcpy(curp, NUM_TEST_TEMPLATE, BLOB_RECORD_LEN);
                            curp += BLOB_RECORD_LEN;
                            */
                        }
                        break;
                    case Type_fields.tf_char: // NC // длина = length * 2
                        //memset(curp, 1, BLOB_RECORD_LEN * 2 * l);
                        for (int ii = 0; ii < curp.Length; ii++)
                        {
                            curp[ii] = 1;
                        }
                        break;
                    case Type_fields.tf_varchar: // NVC // длина = length * 2 + 2
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
                    case Type_fields.tf_version: // RV // 16, 8 версия создания и 8 версия модификации ? каждая версия int32_t(изменения) + int32_t(реструктуризация)
                        //memset(curp, 1, BLOB_RECORD_LEN * 16);
                        break;
                    case Type_fields.tf_string: // NT // 8 (unicode text)
                        //memset(curp, 1, BLOB_RECORD_LEN * 8);
                        break;
                    case Type_fields.tf_text: // T // 8 (ascii text)
                        //memset(curp, 1, BLOB_RECORD_LEN * 8);
                        break;
                    case Type_fields.tf_image: // I // 8 (image = bynary data)
                        //memset(curp, 1, BLOB_RECORD_LEN * 8);
                        break;
                    case Type_fields.tf_datetime: // DT //7
                        if (String.Compare(f.Getname(), "_DATE_TIME") == 0)
                            required = true;
                        else if (String.Compare(f.Getname(), "_NUMBERPREFIX") == 0)
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
                    case Type_fields.tf_version8: // 8, скрытое поле при recordlock == false и отсутствии поля типа tf_version
                        //memset(curp, 1, BLOB_RECORD_LEN * 8);
                        break;
                    case Type_fields.tf_varbinary: // VB // длина = length + 2
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

        public V8Field Get_field(String fieldname)
        {
            V8Field fld = null;

            for (Int32 j = 0; j < num_fields; j++)
            {
                fld = fields[j];
                
                if (String.Compare(fld.Getname(), fieldname) == 0)
                {
                    return fld;
                }
            }

            String s = "В таблице ";
            s += Name;
            s += " не найдено поле ";
            s += fieldname;
            s += ".";
            Console.WriteLine(s);

            return fld;
            
        }

        public V8Index Get_index(String indexname)
        {
            V8Index ind = null;

            for (Int32 j = 0; j < num_indexes; j++)
            {
                ind = indexes[j];
                if (String.Compare(ind.Getname(), indexname) == 0)
                {
                    return ind;
                }
            }

            String s = "В таблице ";
            s += Name;
            s += " не найден индекс ";
            s += indexname;
            s += ".";
            Console.WriteLine(s);

            return ind;

        }

        #region private
        private T_1CD base_;

        private V8object descr_table; // объект с описанием структуры таблицы (только для версий с 8.0 до 8.2.14)
        private String description;
        private String name;
        private Int32 num_fields;
        private List<V8Field> fields;


        private Int32 num_indexes;
        private V8Index[] indexes;
        private bool recordlock;
        private V8object file_data;
        private V8object file_blob;
        private V8object file_index;
        private Int32 recordlen; // длина записи (в байтах)
        private bool issystem; // Признак системной таблицы (имя таблицы не начинается с подчеркивания)
        private Int32 lockinmemory; // счетчик блокировок в памяти

        /// <summary>
        /// Удаление всех полей
        /// </summary>
        private void Deletefields()
        {
            if (fields != null)
                fields.Clear();
        }

        /// <summary>
        /// Удаление всех индексов
        /// </summary>
        private void Deleteindexes()
        {
            Int32 i;
            if (indexes != null)
            {
                for (i = 0; i < num_indexes; i++)
                    indexes[i] = null;
                indexes = null;
            }
        }

        private Changed_rec ch_rec; // первая измененная запись в списке измененных записей
        private UInt32 added_numrecords; // количество добавленных записей в режиме редактирования

        private UInt32 phys_numrecords; // физическое количество записей (вместе с удаленными)
        private UInt32 log_numrecords; // логическое количество записей (только не удаленные)

        /// <summary>
        /// создание файла file_data
        /// </summary>
        private void Create_file_data()
        {
            if (file_data == null) return;
            file_data = new V8object(Base_);
            Refresh_descr_table();
        }

        /// <summary>
        /// создание файла file_blob
        /// </summary>
        private void Create_file_blob()
        {
            if (file_blob == null) return;
            file_blob = new V8object(Base_);
            Refresh_descr_table();
        }

        /// <summary>
        /// создание файла file_index
        /// </summary>
        private void Create_file_index()
        {
            if (File_index == null) return;
            File_index = new V8object(Base_);
            Refresh_descr_table();
        }

        /// <summary>
        /// создание и запись файла описания таблицы
        /// </summary>
        private void Refresh_descr_table()
        {
            Console.WriteLine($"Попытка обновления файла описания таблицы. Таблица {Name}");
            return;
        } 

        private bool edit; // признак, что таблица находится в режиме редактирования

        /// <summary>
        /// Удаление записи из файла data
        /// </summary>
        /// <param name="phys_numrecord"></param>
        private void Delete_data_record(UInt32 phys_numrecord)
        {
            
            Int32 first_empty_rec = 0;

            if (!edit)
            {
                Console.WriteLine($"Попытка удаления записи из файла file_data не в режиме редактирования. Таблица {Name}, Индекс удаляемой записи {phys_numrecord}");
                return;
            }

            if (file_data == null)
            {
                Console.WriteLine($"Попытка удаления записи из несуществующего файла file_data. Таблица {Name}, Индекс удаляемой записи {phys_numrecord}");
                return;
            }

            if (phys_numrecord >= phys_numrecords)
            {
                Console.WriteLine($"Попытка удаления записи в файле file_data за пределами файла. Таблица {Name}, Всего записей {phys_numrecords} Индекс удаляемой записи {phys_numrecord}");
                return;
            }

            if (phys_numrecord == 0)
            {
                Console.WriteLine($"Попытка удаления нулевой записи в файле file_data. Таблица {Name}, Всего записей {phys_numrecords}");
                return;
            }

            if (phys_numrecord == phys_numrecords - 1)
            {
                file_data.Set_len(phys_numrecord * (UInt32)recordlen);
                phys_numrecords--;
            }
            else
            {
                byte[] rec = new byte[recordlen];
                //memset(rec, 0, recordlen);
                Array.Clear(rec, 0, rec.Length);
                //file_data.getdata(first_empty_rec, 0, 4);
                file_data.Getdata(rec, 0, 4);

                //*((int32_t*)rec) = first_empty_rec;

                //file_data->setdata(&first_empty_rec, 0, 4);
                file_data.Setdata(rec, 0, 4);

                Write_data_record(phys_numrecord, rec);

                rec = null;
            }


        }

        /// <summary>
        /// удаление записи из файла blob
        /// </summary>
        /// <param name="blob_numrecord"></param>
        private void Delete_blob_record(UInt32 blob_numrecord)
        {
            //Int32 prev_free_first;
            byte[] prev_free_first;
            Int32 i, j;

            if (!edit)
            {
                Console.WriteLine($"Попытка удаления записи из файла file_blob не в режиме редактирования. Таблица {Name}, Смещение удаляемой записи {blob_numrecord << 8}");
                return;
            }

            if (file_blob == null)
            {
                Console.WriteLine($"Попытка удаления записи из несуществующего файла file_blob. Таблица {Name}, Смещение удаляемой записи {blob_numrecord << 8}");
                return;
            }

            if (blob_numrecord << 8 >= file_blob.Getlen())
            {
                Console.WriteLine($"Попытка удаления записи в файле file_blob за пределами файла. Таблица {Name}, Смещение удаляемой записи {blob_numrecord << 8}, Длина файла {file_blob.Getlen()}");
                return;
            }

            if (blob_numrecord == 0)
            {
                Console.WriteLine($"Попытка удаления нулевой записи в файле file_blob. Таблица {Name}, Длина файла {file_blob.Getlen()}");
                return;
            }

            prev_free_first = new byte[4];

            file_blob.Getdata(prev_free_first, 0, 4); // читаем предыдущее начало свободных блоков

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
        private void Delete_index_record(UInt32 phys_numrecord)
        {
            byte[] rec;

            rec = new byte[recordlen];
            Getrecord(phys_numrecord, rec);
            Delete_index_record(phys_numrecord, rec);
            rec = null;

        }

        /// <summary>
        /// удаление всех индексов записи из файла index
        /// </summary>
        /// <param name="phys_numrecord"></param>
        /// <param name="rec"></param>
        private void Delete_index_record(UInt32 phys_numrecord, byte[] rec)
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
                indexes[i].Delete_index(rec, phys_numrecord);

        }

        /// <summary>
        /// запись одной записи в файл data
        /// </summary>
        /// <param name="phys_numrecord"></param>
        /// <param name="rec"></param>
        private void Write_data_record(UInt32 phys_numrecord, byte[] rec)
        {

            if (!edit)
            {
                Console.WriteLine($"Попытка записи в файл file_data не в режиме редактирования. Таблица {Name} Индекс записываемой записи {phys_numrecord}");
                return;
            }

            if (file_data == null)
                Create_file_data();

            if (phys_numrecord > phys_numrecords && !(phys_numrecord == 1 && phys_numrecords == 0))
            {
                Console.WriteLine($"Попытка записи в файл file_data за пределами файла. Таблица {Name} Всего записей {phys_numrecords} Индекс записываемой записи {phys_numrecord}");
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
                file_data.Setdata(b, 0, (UInt32)recordlen);
                b = null;
            }
            file_data.Setdata(rec, phys_numrecord * (UInt32)recordlen, (UInt32)recordlen);
        }

        /// <summary>
        /// записывает НОВУЮ запись в файл blob, возвращает индекс новой записи
        /// </summary>
        /// <param name="blob_record"></param>
        /// <param name="blob_len"></param>
        /// <returns></returns>
        private UInt32 Write_blob_record(char[] blob_record, UInt32 blob_len)
        {
            UInt32 cur_block, cur_offset, prev_offset, first_block = 0, next_block;
            UInt32 zero = 0;
            UInt16 cur_len;

            if (!edit)
            {
                Console.WriteLine($"Попытка записи в файл file_blob не в режиме редактирования. Таблица {Name}");
                return 0;
            }

            if (blob_len == 0) return 0;

            if (file_blob == null)
            {
                Create_file_blob();
                byte[] b = new byte[BLOB_RECORD_LEN];
                //memset(b, 0, BLOB_RECORD_LEN);
                Array.Clear(b, 0, (Int32)BLOB_RECORD_LEN);
                file_blob.Setdata(b, 0, BLOB_RECORD_LEN);
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
        private UInt32 Write_blob_record(Stream bstr)
        {
            return 0;
        }

        /// <summary>
        /// запись индексов записи в файл index
        /// </summary>
        /// <param name="phys_numrecord"></param>
        /// <param name="rec"></param>
        private void Write_index_record(UInt32 phys_numrecord, char[] rec)
        {

        }

        private bool bad; // признак битой таблицы


        #endregion

    }
}
