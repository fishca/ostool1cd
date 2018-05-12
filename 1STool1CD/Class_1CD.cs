using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using static _1STool1CD.Utils1CD;
using static _1STool1CD.APIcfBase;
using static _1STool1CD.Constants;


namespace _1STool1CD
{

    /// <summary>
    /// Cтруктура первой страницы контейнера
    /// </summary>
    public struct V8con
    {
        // восемь символов
        char[] sig; // сигнатура SIG_CON
        char ver1;
        char ver2;
        char ver3;
        char ver4;
        UInt32 length;
        UInt32 firstblock;
        UInt32 pagesize;

        public String getver()
        {
            String ss = ver1.ToString();
            ss += ".";
            ss += ver2;
            ss += ".";
            ss += ver3;
            ss += ".";
            ss += ver4;
            return ss;
        }
    };

    /// <summary>
    /// Структура страницы размещения уровня 1 версий от 8.0 до 8.2.14
    /// </summary>
    public struct Objtab
    {
        public Int32 numblocks;
        public UInt32[] blocks;
        public Objtab(Int32 _numblocks, UInt32[] _blocks)
        {
            numblocks = _numblocks;
            blocks = _blocks;
        }

    };
    
    public struct Root_80
    {
        public char[] lang; // 8
        public UInt32 numblocks;
        public UInt32[] blocks;
    };

    public struct Root_81
    {
        public char[] lang; //32
        public UInt32 numblocks;
        public UInt32[] blocks;
    };
    
    /// <summary>
    /// Типы страниц
    /// </summary>
    public enum Pagetype
    {
        lost,          // потерянная страница (не относится ни к одному объекту)
	    root,          // корневая страница (страница 0)
	    freeroot,      // корневая страница таблицы свободных блоков (страница 1)
	    freealloc,     // страница размещения таблицы свободных блоков
	    free,          // свободная страница
	    rootfileroot,  // корневая страница корневого файла (страница 2)
	    rootfilealloc, // страница размещения корневого файла
	    rootfile,      // страница данных корневого файла
	    descrroot,     // корневая страница файла descr таблицы
	    descralloc,    // страница размещения файла descr таблицы
	    descr,         // страница данных файла descr таблицы
	    dataroot,      // корневая страница файла data таблицы
	    dataalloc,     // страница размещения файла data таблицы
	    data,          // страница данных файла data таблицы
	    indexroot,     // корневая страница файла index таблицы
	    indexalloc,    // страница размещения файла index таблицы
	    index,         // страница данных файла index таблицы
	    blobroot,      // корневая страница файла blob таблицы
	    bloballoc,     // страница размещения файла blob таблицы
	    blob           // страница данных файла blob таблицы
    };

    /// <summary>
    /// Структура принадлежности страницы
    /// </summary>
    public struct Pagemaprec
    {
        public Int32 tab;     // Индекс в T_1CD::tables, -1 - страница не относится к таблицам
        public Pagetype type; // тип страницы
        public UInt32 number; // номер страницы в своем типе
        public Pagemaprec(Int32 _tab = -1, Pagetype _type = Pagetype.lost, UInt32 _number = 0)
        {
            tab = -1;
            type = _type;
            number = 0;
        }
    };

    /// <summary>
    /// Версии файлов shapshot
    /// </summary>
    public enum Snapshot_version
    {
        Ver1 = 1,
	    Ver2 = 2
    };

    /// <summary>
    /// Известные версии хранилища конфигурации
    /// </summary>
    public enum Depot_ver
    {
        UnknownVer = 0,
	    Ver3 = 3, // 0300000000000000
	    Ver5 = 5, // 0500000000000000
	    Ver6 = 6, // 0600000000000000
	    Ver7 = 7  // 0700000000000000
    };

    // класс конфигурации поставщика
    public class SupplierConfig
    {
	    public Table_file file;
        public String name;     // имя конфигурация поставщика
        public String supplier; // синоним конфигурация поставщика
        public String version;  // версия конфигурация поставщика
    };




    /****************************************************************************************************************
     * 
     * 
     * 
     * 
     * 
     * **************************************************************************************************************/

    /// <summary>
    /// Структура файла таблицы контейнера файлов
    /// </summary>
    public struct Table_file
    {
        public V8Table t;
        public String name; // Имя, как оно хранится в таблице
        UInt32 maxpartno;
        table_blob_file addr;
        public DateTime ft_create;
        public DateTime ft_modify;

        /*
        public table_file(Table _t, String _name, UInt32 _maxpartno)
        {
            t = _t;
            name = _name;
            maxpartno = _maxpartno;
        }
        */
        
    }

    /// <summary>
    /// Основной класс
    /// </summary>
    public class T_1CD
    {
        public static bool recoveryMode;
        public char[] locale;    // код языка базы
        public bool is_infobase; // признак информационной базы
        public bool is_depot;    // признак хранилища конфигурации

        public Stream data1CD;

        /// <summary>
        /// Свойство, показывающее в каком режиме открыт файл БД
        /// </summary>
        public bool ReadOnly
        {
            get
            {
                return Get_readonly();
            }
            set
            {
                _ReadOnly = value;
            }
        }


        public bool Get_readonly()
        {
            return _ReadOnly;
        }

        // Таблицы информационной базы
        public V8Table table_config;
        public V8Table table_configsave;
        public V8Table table_params;
        public V8Table table_files;
        public V8Table table_dbschema;
        public V8Table table_configcas;
        public V8Table table_configcassave;
        public V8Table table__extensionsinfo;

        // таблицы - хранилища файлов
        ConfigStorageTableConfig cs_config;
        ConfigStorageTableConfigSave cs_configsave;

        // Таблицы хранилища конфигураций
        public V8Table table_depot;
        public V8Table table_users;
        public V8Table table_objects;
        public V8Table table_versions;
        public V8Table table_labels;
        public V8Table table_history;
        public V8Table table_lastestversions;
        public V8Table table_externals;
        public V8Table table_selfrefs;
        public V8Table table_outrefs;

        public String ver;

        public List<SupplierConfig> supplier_configs; // конфигурации поставщика
        
        public bool supplier_configs_defined; // признак, что был произведен поиск конфигураций поставщика

        /// <summary>
        ///  Конструктор 
        /// </summary>
        public T_1CD()
        {
            
        }

        public static Tree Get_treeFromV8file(v8file f)
        {
            //TBytesStream* sb;
            
            Encoding enc;
            Byte[] bytes = new Byte[0x1000];
            Int32 offset;
            Tree rt;

            //MemoryStream sb = new MemoryStream(bytes);
            MemoryTributary sb = new MemoryTributary(bytes);

            //f.SaveToStream(sb);
            f.SaveToStream(sb);

            enc = null;
            /*
            //offset = Encoding::GetBufferEncoding(sb->GetBytes(), enc);
            offset = Encoding.GetEncoding()
            if (offset == 0)
            {
                msreg_g.AddError("Ошибка определения кодировки файла контейнера",
                    "Файл", f->GetFullName());
                delete sb;
                return nullptr;
            }
            bytes = TEncoding::Convert(enc, TEncoding::Unicode, sb->GetBytes(), offset, sb->GetSize() - offset);

            rt = parse_1Ctext(String((WCHART*)&bytes[0], bytes.size() / 2), f->GetFullName());
            delete sb;
            */
            return null;
        }
        
        /// <summary>
        /// Проверка открытия файла базы 1CD
        /// </summary>
        /// <returns></returns>
        public bool Is_open()
        {
            return fs != null;
        }

        /// <summary>
        /// Определение количества таблиц
        /// </summary>
        /// <returns></returns>
        UInt32 Get_numtables()
        {
            return (UInt32)num_tables;
        }

        V8Table Gettable(UInt32 numtable)
        {
            if (numtable >= num_tables)
            {
                /*
                msreg_m.AddError("Попытка получения таблицы по номеру, превышающему количество таблиц",
                    "Количество таблиц", num_tables,
                    "Номер таблицы", numtable + 1);
                */

                return null;
            }
            return tables[numtable];
        }

        Db_ver Getversion()
        {
            return Db_ver.ver8_2_14_0;
        }

        public bool Save_config(String filename)
        {
            if (cs_config != null)
                cs_config = new ConfigStorageTableConfig(Get_files_config());

            if (cs_config != null)
                if (cs_config.getready())
                    return false;
            if (cs_config != null)
            {
                return cs_config.save_config(filename);
            }
            else
            {
                return false;
            }


        }

        public bool Save_configsave(String filename)
        {
            if (cs_configsave != null)
                cs_configsave = new ConfigStorageTableConfigSave(Get_files_config(), Get_files_configsave());

            if (cs_configsave != null)
                if (cs_configsave.getready())
                    return false;
            if (cs_configsave != null)
            {
                return cs_configsave.save_config(filename);
            }
            else
            {
                return false;
            }
        }

        public void Find_supplier_configs()
        {
            /*
             	std::map<String,table_file*>::iterator p;

    	        for(p = get_files_configsave()->files().begin(); p != get_files_configsave()->files().end(); ++p)
	            {
		            if(p->first.GetLength() == 73) add_supplier_config(p->second);
	            }
	            for(p = get_files_config()->files().begin(); p != get_files_config()->files().end(); ++p)
	            {
	            	if(p->first.GetLength() == 73) add_supplier_config(p->second);
	            }
	            supplier_configs_defined = true;
            
             */
        }

        public bool Save_supplier_configs(UInt32 numcon, String filename)
        {
            FileStream _fs = null;
            container_file f;
            Table_file tf;

            if (numcon >= supplier_configs.Capacity)
                return false;

            tf = supplier_configs[(int)numcon].file;

            f = new container_file(tf, tf.name);
            if (!f.open())
            {
                //f = null;
                return false;
            }

            try
            {
                _fs = new FileStream(filename, FileMode.OpenOrCreate);
            }
            catch 
	{
                return false;
            }

            try
            {
                //Inflate((MemoryTributary)f.stream, out (MemoryTributary)_fs);
            }
            catch
	{
                /*
                msreg_m.AddError("Ошибка распаковки файла конфигурации поставщика",
                    "Имя файла", _filename);
                    */
                
                _fs.Dispose(); ;
                return false;
            }

            _fs.Dispose();
            
            return true;
            }

        public bool Save_depot_config(String _filename, UInt32 ver = 0)
        {
            return true;
        }

        public bool Save_part_depot_config(String _filename, Int32 ver_begin, Int32 ver_end) { return true; }

	    public Int32 Get_ver_depot_config(Int32 ver) { return 100; } // Получение номера версии конфигурации (0 - последняя, -1 - предпоследняя и т.д.)

        public bool Save_config_ext(String _filename, String uid, String hashname)
        {
            
            bool res;

            ConfigStorageTableConfigCasSave cs = new ConfigStorageTableConfigCasSave(Get_files_configcas(), Get_files_configcassave(), new Guid(uid), hashname);
            if (!cs.getready())
            {
                res = false;
            }
            else
            {
                res = cs.save_config(_filename);
            }
            cs = null;
            return res;
        }

        public bool Save_config_ext_db(String _filename, String hashname)
        {
            bool res;

            ConfigStorageTableConfigCas cs = new ConfigStorageTableConfigCas(Get_files_configcas(), hashname);
            if (!cs.getready())
                res = false;
            else
                res = cs.save_config(_filename);
            cs = null;
            return res;
        }

        /*
        public bool get_readonly()
        {
            return readonly_;
        }

        public void set_readonly(bool ro)
        {
            readonly_ = ro;
        }
        */

        public void Flush()
        {
            V8MemBlock.Flush();
        }

        public bool Test_stream_format() { return true; }
        public bool Test_list_of_tables() { return true; } // проверка списка таблиц (по DBNames)

        public void Find_lost_objects()
        {
            UInt32 i;
            Byte[] buf = new Byte[8];
            V8object v8obj;
            bool block_is_find;

            for (i = 1; i < length; i++)
            {
                Getblock(buf, i, 8);
                //if (buf.Contains(SIG_OBJ))
                if (Array.IndexOf(buf, SIG_OBJ) == 0)
                //if (memcmp(buf, SIG_OBJ, 8) == 0)
                {
                    block_is_find = false;
                    for (v8obj = V8object.Get_first(); v8obj != null; v8obj = v8obj.Get_next())
                    {
                        if (v8obj.Get_block_number() == i)
                        {
                            block_is_find = true;
                            break;
                        }
                    }
                    if (!block_is_find)
                    {
                        //msreg_m.AddMessage_("Найден потерянный объект", MessageState::Info, "Номер блока", to_hex_string(i));
                        Console.WriteLine("Найден потерянный объект");
                    }
                }
            }
            //msreg_m.AddMessage("Поиск потерянных объектов завершен", MessageState::Succesfull);
            Console.WriteLine("Поиск потерянных объектов завершен");

        }


        public void Find_and_save_lost_objects(String lost_objects) { }
        public bool Create_table(String path) { return true; } // создание таблицы из файлов импорта таблиц
        public bool Delete_table(V8Table tab) { return true; }
        public bool Delete_object(V8object ob) { return true; }
        public bool ReplaceTREF(String mapfile) { return true; } // замена значений полей ...TREF во всех таблицах базы
        public void Find_and_create_lost_tables() { }
        public void Restore_DATA_allocation_table(V8Table tab) { }
        public bool Test_block_by_template(UInt32 testblock, char tt, UInt32 num, Int32 rlen, Int32 len) { return true; }
        public String Getfilename() { return filename; }
        public UInt32 Getpagesize() { return pagesize; }

        private String filename;
        public UInt32 pagesize; // размер одной страницы (до версии 8.2.14 всегда 0x1000 (4K), начиная с версии 8.3.8 от 0x1000 (4K) до 0x10000 (64K))

        FileStream fs;
        
        public Db_ver version;

        private UInt32 length;        // длина базы в блоках

        private V8object free_blocks; // свободные блоки
        private V8object root_object; // корневой объект

        private Int32 num_tables;     // количество таблиц

        private V8Table[] tables;       // таблицы базы

        private bool _ReadOnly;

        private Pagemaprec[] pagemap;         // Массив длиной length

        private TableFiles _files_config;
        private TableFiles _files_configsave;
        private TableFiles _files_params;
        private TableFiles _files_files;
        private TableFiles _files_configcas;
        private TableFiles _files_configcassave;

        private TableFiles Get_files_config()
        {
            if (_files_config != null)
            {
                _files_config = new TableFiles(table_config);
            }
            return _files_config;
        }

        private TableFiles Get_files_configsave()
        {
            if (_files_configsave != null)
            {
                _files_configsave = new TableFiles(table_configsave);
            }
            return _files_configsave;
        }

        private TableFiles Get_files_params()
        {
            if (_files_params != null)
            {
                _files_params = new TableFiles(table_params);
            }
            return _files_params;
        }

        private TableFiles Get_files_files()
        {
            if (_files_files != null)
            {
                _files_files = new TableFiles(table_files);
            }
            return _files_files;
        }

        private TableFiles Get_files_configcas()
        {
            if (_files_configcas != null)
            {
                _files_configcas = new TableFiles(table_configcas);
            }
            return _files_configcas;
        }

        private TableFiles Get_files_configcassave()
        {
            if (_files_configcassave != null) 
            {
                _files_configcassave = new TableFiles(table_configcassave);
            }
            return _files_configcassave;
        }

        private void Init() { }

        public byte[] Getblock(UInt32 block_number)
        {
            
            if (data1CD == null)
                return null;
            if (block_number >= length)
            {
                Console.WriteLine($"Попытка чтения блока за пределами файла. Индекс блока {block_number}. Всего блоков {length}");
                return null;
            }

            V8MemBlock tmpV8MemBlock = new V8MemBlock((FileStream)data1CD, block_number, false, true);
            return V8MemBlock.Getblock((FileStream)data1CD, block_number);
        }

        public bool Getblock(ref byte[] buf, UInt32 block_number, Int32 blocklen = -1) // буфер принадлежит вызывающей процедуре
        {
            if (data1CD == null)
                return false;

            if (blocklen < 0)
                blocklen = (Int32)pagesize;

            if (block_number >= length)
            {
                Console.WriteLine($"Попытка чтения блока за пределами файла. Индекс блока {block_number}, всего блоков {length}");
                return false;
            }

            //memcpy(buf, MemBlock::getblock(fs, block_number), blocklen);


            V8MemBlock tmp_mem_block = new V8MemBlock((FileStream)data1CD, block_number, false, true);
            byte[] tmp_buf = V8MemBlock.Getblock((FileStream)data1CD, block_number);
            Array.Copy(tmp_buf, buf, blocklen);
            return true;

        }


        public bool Getblock(byte[] buf, UInt32 block_number, Int32 blocklen = -1) { return true; } // буфер принадлежит вызывающей процедуре

        //private char getblock(UInt32 block_number) { return ' '; }           // буфер не принадлежит вызывающей стороне (принадлежит memblock)
        public Byte[] Getblock_for_write(UInt32 block_number, bool read) { return new Byte[2]; } // буфер не принадлежит вызывающей стороне (принадлежит memblock)

        /// <summary>
        /// Пометить блок как свободный
        /// </summary>
        /// <param name="block_number"></param>
        public void Set_block_as_free(UInt32 block_number)
        {
            free_blocks.Set_block_as_free(block_number);
        }

        /// <summary>
        /// Получить номер свободного блока (и пометить как занятый)
        /// </summary>
        /// <returns></returns>
        public UInt32 Get_free_block()
        {
            return free_blocks.Get_free_block();
        } 

        private void Add_supplier_config(Table_file file) { }

        private bool Recursive_test_stream_format(V8Table t, UInt32 nrec) { return true; }
        private bool Recursive_test_stream_format2(V8Table t, UInt32 nrec) { return true; } // для DBSCHEMA
        private bool Recursive_test_stream_format(Stream str, String path, bool maybezipped2 = false) { return true; }
        private bool Recursive_test_stream_format(V8catalog cat, String path) { return true; }

        private void Pagemapfill()
        {
            if (pagemap != null)
            {
                //delete[] pagemap;
                pagemap = null;
            }
            pagemap = new Pagemaprec[length];

            pagemap[0].type = Pagetype.root;
            pagemap[1].type = Pagetype.freeroot;
            pagemap[2].type = Pagetype.rootfileroot;

        }

        private String Pagemaprec_presentation(Pagemaprec pmr)
        {
            switch (pmr.type)
            {
                case Pagetype.lost:          return ("потерянная страница");
                case Pagetype.root:          return ("корневая страница базы");
                case Pagetype.freeroot:      return ("корневая страница таблицы свободных блоков");
                case Pagetype.freealloc:     return ("страница размещения таблицы свободных блоков номер ") + pmr.number;
                case Pagetype.free:          return ("свободная страница номер ")                           + pmr.number;
                case Pagetype.rootfileroot:  return ("корневая страница корневого файла");                  
                case Pagetype.rootfilealloc: return ("страница размещения корневого файла номер ")          + pmr.number;
                case Pagetype.rootfile:      return ("страница данных корневого файла номер ")              + pmr.number;
                case Pagetype.descrroot:     return ("корневая страница файла descr таблицы ")              + tables[pmr.tab].Getname();
                case Pagetype.descralloc:    return ("страница размещения файла descr таблицы ")            + tables[pmr.tab].Getname() + " номер " + pmr.number;
                case Pagetype.descr:         return ("страница данных файла descr таблицы ")                + tables[pmr.tab].Getname() + " номер " + pmr.number;
                case Pagetype.dataroot:      return ("корневая страница файла data таблицы ")               + tables[pmr.tab].Getname();
                case Pagetype.dataalloc:     return ("страница размещения файла data таблицы ")             + tables[pmr.tab].Getname() + " номер " + pmr.number;
                case Pagetype.data:          return ("страница данных файла data таблицы ")                 + tables[pmr.tab].Getname() + " номер " + pmr.number;
                case Pagetype.indexroot:     return ("корневая страница файла index таблицы ")              + tables[pmr.tab].Getname();
                case Pagetype.indexalloc:    return ("страница размещения файла index таблицы ")            + tables[pmr.tab].Getname() + " номер " + pmr.number;
                case Pagetype.index:         return ("страница данных файла index таблицы ")                + tables[pmr.tab].Getname() + " номер " + pmr.number;
                case Pagetype.blobroot:      return ("корневая страница файла blob таблицы ")               + tables[pmr.tab].Getname();
                case Pagetype.bloballoc:     return ("страница размещения файла blob таблицы ")             + tables[pmr.tab].Getname() + " номер " + pmr.number;
                case Pagetype.blob:          return ("страница данных файла blob таблицы ")                 + tables[pmr.tab].Getname() + " номер " + pmr.number;

                default:
                    return ("??? неизвестный тип страницы ???");
            }
        }

        private Depot_ver Get_depot_version(byte[] record)
        {
            Depot_ver depotVer = Depot_ver.UnknownVer;

            V8Field fldd_depotver = table_depot.Get_field("DEPOTVER");

            if (fldd_depotver != null)
            {
                String Ver = fldd_depotver.Get_presentation(record, true);

                if (String.Compare(Ver, "0300000000000000") == 0)
                {
                    depotVer = Depot_ver.Ver3;
                }
                else if (String.Compare(Ver, "0500000000000000") == 0)
                {
                    depotVer = Depot_ver.Ver5;
                }
                else if (String.Compare(Ver, "0600000000000000") == 0)
                {
                    depotVer = Depot_ver.Ver6;
                }
                else if (String.Compare(Ver, "0700000000000000") == 0)
                {
                    depotVer = Depot_ver.Ver7;
                }
                else
                {
                    depotVer = Depot_ver.UnknownVer;

                    //msreg_m.AddMessage_("Неизвестная версия хранилища", MessageState::Error, "Версия хранилища", Ver);
                    Console.WriteLine("Неизвестная версия хранилища");
                }

                return depotVer;
            }

            return depotVer;

        }

    } // Окончание класса T_1CD

}
