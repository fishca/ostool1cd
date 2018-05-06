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
    public struct v8con
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
    public struct objtab
    {
        public UInt32 numblocks;
        public UInt32[] blocks;
    };
    
    public struct root_80
    {
        public char[] lang; // 8
        public UInt32 numblocks;
        public UInt32[] blocks;
    };

    public struct root_81
    {
        public char[] lang; //32
        public UInt32 numblocks;
        public UInt32[] blocks;
    };
    
    /// <summary>
    /// Типы страниц
    /// </summary>
    public enum pagetype
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
    public struct pagemaprec
    {
        public Int32 tab;     // Индекс в T_1CD::tables, -1 - страница не относится к таблицам
        public pagetype type; // тип страницы
        public UInt32 number; // номер страницы в своем типе
        public pagemaprec(Int32 _tab = -1, pagetype _type = pagetype.lost, UInt32 _number = 0)
        {
            tab = -1;
            type = _type;
            number = 0;
        }
    };

    /// <summary>
    /// Версии файлов shapshot
    /// </summary>
    public enum snapshot_version
    {
        Ver1 = 1,
	    Ver2 = 2
    };

    /// <summary>
    /// Известные версии хранилища конфигурации
    /// </summary>
    enum depot_ver
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
	    public table_file file;
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
    public struct table_file
    {
        public Table t;
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

        // Таблицы информационной базы
        public Table table_config;
        public Table table_configsave;
        public Table table_params;
        public Table table_files;
        public Table table_dbschema;
        public Table table_configcas;
        public Table table_configcassave;
        public Table table__extensionsinfo;

        // таблицы - хранилища файлов
        ConfigStorageTableConfig cs_config;
        ConfigStorageTableConfigSave cs_configsave;

        // Таблицы хранилища конфигураций
        public Table table_depot;
        public Table table_users;
        public Table table_objects;
        public Table table_versions;
        public Table table_labels;
        public Table table_history;
        public Table table_lastestversions;
        public Table table_externals;
        public Table table_selfrefs;
        public Table table_outrefs;

        public String ver;

        public List<SupplierConfig> supplier_configs; // конфигурации поставщика
        
        public bool supplier_configs_defined; // признак, что был произведен поиск конфигураций поставщика

        /// <summary>
        ///  Конструктор 
        /// </summary>
        public T_1CD()
        {
            
        }

        public static tree get_treeFromV8file(v8file f)
        {
            //TBytesStream* sb;
            
            Encoding enc;
            Byte[] bytes = new Byte[0x1000];
            Int32 offset;
            tree rt;

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
        public bool is_open()
        {
            return fs != null;
        }

        /// <summary>
        /// Определение количества таблиц
        /// </summary>
        /// <returns></returns>
        UInt32 get_numtables()
        {
            return (UInt32)num_tables;
        }

        Table gettable(UInt32 numtable)
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

        db_ver getversion()
        {
            return db_ver.ver8_2_14_0;
        }

        public bool save_config(String filename)
        {
            if (cs_config != null)
                cs_config = new ConfigStorageTableConfig(get_files_config());

            if (cs_config.getready())
                return false;

            return cs_config.save_config(filename);
        }

        public bool save_configsave(String filename)
        {
            if (cs_configsave != null)
                cs_configsave = new ConfigStorageTableConfigSave(get_files_config(), get_files_configsave());

            if (cs_configsave.getready())
                return false;

            return cs_configsave.save_config(filename);
        }

        public void find_supplier_configs()
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

        public bool save_supplier_configs(UInt32 numcon, String filename)
        {
            FileStream _fs = null;
            container_file f;
            table_file tf;

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

        public bool save_depot_config(String _filename, UInt32 ver = 0)
        {
            return true;
        }

        public bool save_part_depot_config(String _filename, Int32 ver_begin, Int32 ver_end) { return true; }

	    public Int32 get_ver_depot_config(Int32 ver) { return 100; } // Получение номера версии конфигурации (0 - последняя, -1 - предпоследняя и т.д.)

        public bool save_config_ext(String _filename, String uid, String hashname)
        {
            
            bool res;

            ConfigStorageTableConfigCasSave cs = new ConfigStorageTableConfigCasSave(get_files_configcas(), get_files_configcassave(), new Guid(uid), hashname);
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

        public bool save_config_ext_db(String _filename, String hashname)
        {
            bool res;

            ConfigStorageTableConfigCas cs = new ConfigStorageTableConfigCas(get_files_configcas(), hashname);
            if (!cs.getready())
                res = false;
            else
                res = cs.save_config(_filename);
            cs = null;
            return res;
        }

        public bool get_readonly()
        {
            return readonly_;
        }

        public void set_readonly(bool ro)
        {
            readonly_ = ro;
        }

        public void flush()
        {
            MemBlock.flush();
        }

        public bool test_stream_format() { return true; }
        public bool test_list_of_tables() { return true; } // проверка списка таблиц (по DBNames)

        public void find_lost_objects()
        {
            UInt32 i;
            Byte[] buf = new Byte[8];
            v8object v8obj;
            bool block_is_find;

            for (i = 1; i < length; i++)
            {
                getblock(buf, i, 8);
                //if (buf.Contains(SIG_OBJ))
                if (Array.IndexOf(buf, SIG_OBJ) == 0)
                //if (memcmp(buf, SIG_OBJ, 8) == 0)
                {
                    block_is_find = false;
                    for (v8obj = v8object.get_first(); v8obj != null; v8obj = v8obj.get_next())
                    {
                        if (v8obj.get_block_number() == i)
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


        public void find_and_save_lost_objects(String lost_objects) { }
        public bool create_table(String path) { return true; } // создание таблицы из файлов импорта таблиц
        public bool delete_table(Table tab) { return true; }
        public bool delete_object(v8object ob) { return true; }
        public bool replaceTREF(String mapfile) { return true; } // замена значений полей ...TREF во всех таблицах базы
        public void find_and_create_lost_tables() { }
        public void restore_DATA_allocation_table(Table tab) { }
        public bool test_block_by_template(UInt32 testblock, char tt, UInt32 num, Int32 rlen, Int32 len) { return true; }
        public String getfilename() { return filename; }
        public UInt32 getpagesize() { return pagesize; }

        private String filename;
        public UInt32 pagesize; // размер одной страницы (до версии 8.2.14 всегда 0x1000 (4K), начиная с версии 8.3.8 от 0x1000 (4K) до 0x10000 (64K))

        FileStream fs;
        
        public db_ver version;

        private UInt32 length;        // длина базы в блоках

        private v8object free_blocks; // свободные блоки
        private v8object root_object; // корневой объект

        private Int32 num_tables;     // количество таблиц

        private Table[] tables;       // таблицы базы

        private bool readonly_;

        private pagemaprec[] pagemap;         // Массив длиной length

        private TableFiles _files_config;
        private TableFiles _files_configsave;
        private TableFiles _files_params;
        private TableFiles _files_files;
        private TableFiles _files_configcas;
        private TableFiles _files_configcassave;

        private TableFiles get_files_config()
        {
            if (_files_config != null)
            {
                _files_config = new TableFiles(table_config);
            }
            return _files_config;
        }

        private TableFiles get_files_configsave()
        {
            if (_files_configsave != null)
            {
                _files_configsave = new TableFiles(table_configsave);
            }
            return _files_configsave;
        }

        private TableFiles get_files_params()
        {
            if (_files_params != null)
            {
                _files_params = new TableFiles(table_params);
            }
            return _files_params;
        }

        private TableFiles get_files_files()
        {
            if (_files_files != null)
            {
                _files_files = new TableFiles(table_files);
            }
            return _files_files;
        }

        private TableFiles get_files_configcas()
        {
            if (_files_configcas != null)
            {
                _files_configcas = new TableFiles(table_configcas);
            }
            return _files_configcas;
        }

        private TableFiles get_files_configcassave()
        {
            if (_files_configcassave != null) 
            {
                _files_configcassave = new TableFiles(table_configcassave);
            }
            return _files_configcassave;
        }

        private void init() { }
        public bool getblock(byte[] buf, UInt32 block_number, Int32 blocklen = -1) { return true; } // буфер принадлежит вызывающей процедуре

        private char getblock(UInt32 block_number) { return ' '; }           // буфер не принадлежит вызывающей стороне (принадлежит memblock)
        public Byte[] getblock_for_write(UInt32 block_number, bool read) { return new Byte[2]; } // буфер не принадлежит вызывающей стороне (принадлежит memblock)

        /// <summary>
        /// Пометить блок как свободный
        /// </summary>
        /// <param name="block_number"></param>
        public void set_block_as_free(UInt32 block_number)
        {
            free_blocks.set_block_as_free(block_number);
        }

        /// <summary>
        /// Получить номер свободного блока (и пометить как занятый)
        /// </summary>
        /// <returns></returns>
        public UInt32 get_free_block()
        {
            return free_blocks.get_free_block();
        } 

        private void add_supplier_config(table_file file) { }

        private bool recursive_test_stream_format(Table t, UInt32 nrec) { return true; }
        private bool recursive_test_stream_format2(Table t, UInt32 nrec) { return true; } // для DBSCHEMA
        private bool recursive_test_stream_format(Stream str, String path, bool maybezipped2 = false) { return true; }
        private bool recursive_test_stream_format(v8catalog cat, String path) { return true; }

        private void pagemapfill()
        {
            if (pagemap != null)
            {
                //delete[] pagemap;
                pagemap = null;
            }
            pagemap = new pagemaprec[length];

            pagemap[0].type = pagetype.root;
            pagemap[1].type = pagetype.freeroot;
            pagemap[2].type = pagetype.rootfileroot;

        }
        private String pagemaprec_presentation(pagemaprec pmr)
        {
            switch (pmr.type)
            {
                case pagetype.lost:          return ("потерянная страница");
                case pagetype.root:          return ("корневая страница базы");
                case pagetype.freeroot:      return ("корневая страница таблицы свободных блоков");
                case pagetype.freealloc:     return ("страница размещения таблицы свободных блоков номер ") + pmr.number;
                case pagetype.free:          return ("свободная страница номер ")                           + pmr.number;
                case pagetype.rootfileroot:  return ("корневая страница корневого файла");                  
                case pagetype.rootfilealloc: return ("страница размещения корневого файла номер ")          + pmr.number;
                case pagetype.rootfile:      return ("страница данных корневого файла номер ")              + pmr.number;
                case pagetype.descrroot:     return ("корневая страница файла descr таблицы ")              + tables[pmr.tab].getname();
                case pagetype.descralloc:    return ("страница размещения файла descr таблицы ")            + tables[pmr.tab].getname() + " номер " + pmr.number;
                case pagetype.descr:         return ("страница данных файла descr таблицы ")                + tables[pmr.tab].getname() + " номер " + pmr.number;
                case pagetype.dataroot:      return ("корневая страница файла data таблицы ")               + tables[pmr.tab].getname();
                case pagetype.dataalloc:     return ("страница размещения файла data таблицы ")             + tables[pmr.tab].getname() + " номер " + pmr.number;
                case pagetype.data:          return ("страница данных файла data таблицы ")                 + tables[pmr.tab].getname() + " номер " + pmr.number;
                case pagetype.indexroot:     return ("корневая страница файла index таблицы ")              + tables[pmr.tab].getname();
                case pagetype.indexalloc:    return ("страница размещения файла index таблицы ")            + tables[pmr.tab].getname() + " номер " + pmr.number;
                case pagetype.index:         return ("страница данных файла index таблицы ")                + tables[pmr.tab].getname() + " номер " + pmr.number;
                case pagetype.blobroot:      return ("корневая страница файла blob таблицы ")               + tables[pmr.tab].getname();
                case pagetype.bloballoc:     return ("страница размещения файла blob таблицы ")             + tables[pmr.tab].getname() + " номер " + pmr.number;
                case pagetype.blob:          return ("страница данных файла blob таблицы ")                 + tables[pmr.tab].getname() + " номер " + pmr.number;

                default:
                    return ("??? неизвестный тип страницы ???");
            }
        }

        private depot_ver get_depot_version(char[] record)
        {
            depot_ver depotVer = depot_ver.UnknownVer;

            Field fldd_depotver = table_depot.get_field("DEPOTVER");

            if (fldd_depotver != null)
            {
                return depotVer;
            }

            String Ver = fldd_depotver.get_presentation(record, true);

            

            if (String.Compare(Ver, "0300000000000000") == 0)
            {
                depotVer = depot_ver.Ver3;
            }
            else if (String.Compare(Ver, "0500000000000000") == 0)
            {
                depotVer = depot_ver.Ver5;
            }
            else if (String.Compare(Ver, "0600000000000000") == 0)
            {
                depotVer = depot_ver.Ver6;
            }
            else if (String.Compare(Ver, "0700000000000000") == 0)
            {
                depotVer = depot_ver.Ver7;
            }
            else
            {
                depotVer = depot_ver.UnknownVer;

                //msreg_m.AddMessage_("Неизвестная версия хранилища", MessageState::Error, "Версия хранилища", Ver);
                Console.WriteLine("Неизвестная версия хранилища");
            }

            return depotVer;

        }

    } // Окончание класса T_1CD

}
