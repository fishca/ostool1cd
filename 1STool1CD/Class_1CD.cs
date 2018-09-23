using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using static _1STool1CD.Utils1CD;
using static _1STool1CD.APIcfBase;
using static _1STool1CD.Constants;
using static _1STool1CD.Structures;


namespace _1STool1CD
{

    // класс конфигурации поставщика
    public class SupplierConfig
    {
        private TableFile file;
        private String name;     // имя конфигурация поставщика
        private String supplier; // синоним конфигурация поставщика
        private String version;  // версия конфигурация поставщика

        public TableFile File
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public string Supplier
        {
            get;
            set;
        }

        public string Version
        {
            get;
            set;
        }
    };




    /****************************************************************************************************************
     * 
     * 
     * 
     * 
     * 
     * **************************************************************************************************************/


    /// <summary>
    /// Основной класс
    /// </summary>
    public class Tools1CD
    {
        private static bool recoveryMode;
        private char[] locale;    // код языка базы
        private bool is_infobase; // признак информационной базы
        private bool is_depot;    // признак хранилища конфигурации

        private Stream data1CD;

        /// <summary>
        /// Свойство, показывающее в каком режиме открыт файл БД
        /// </summary>
        public bool ReadOnly
        {
            get { return Get_readonly(); }
            set { _ReadOnly = value;     }
        }

        public static bool RecoveryMode
        {
            get { return recoveryMode; }
            set { recoveryMode = value; }
        }

        public char[] Locale
        {
            get { return locale; }
            set { locale = value; }
        }

        public bool Is_infobase
        {
            get { return is_infobase; }
            set { is_infobase = value; }
        }

        public bool Is_depot
        {
            get { return is_depot; }
            set { is_depot = value; }
        }

        public Stream Data1CD
        {
            get { return data1CD; }
            set { data1CD = value; }
        }

        public V8Table Table_config
        {
            get { return table_config; }
            set { table_config = value; }
        }

        public V8Table Table_configsave
        {
            get { return table_configsave; }
            set { table_configsave = value; }
        }

        public V8Table Table_params { get { return table_params; } set { table_params = value; } }

        public V8Table Table_files { get { return table_files; } set { table_files = value; } }

        public V8Table Table_dbschema { get { return table_dbschema; } set { table_dbschema = value; } }

        public V8Table Table_configcas { get { return table_configcas; } set { table_configcas = value; } }

        public V8Table Table_configcassave { get { return table_configcassave; } set { table_configcassave = value; } }

        public V8Table Table__extensionsinfo { get { return table__extensionsinfo; } set { table__extensionsinfo = value; } }

        public V8Table Table_depot { get { return table_depot; } set { table_depot = value; } }

        public V8Table Table_users { get { return table_users; } set { table_users = value; } }

        public V8Table Table_objects { get { return table_objects; } set { table_objects = value; } }

        public V8Table Table_versions { get { return table_versions; } set { table_versions = value; } }

        public V8Table Table_labels { get { return table_labels; } set { table_labels = value; } }

        public V8Table Table_history { get { return table_history; } set { table_history = value; } }

        public V8Table Table_lastestversions { get { return table_lastestversions; } set { table_lastestversions = value; } }

        public V8Table Table_externals { get { return table_externals; } set { table_externals = value; } }

        public V8Table Table_selfrefs { get { return table_selfrefs; } set { table_selfrefs = value; } }

        public V8Table Table_outrefs { get { return table_outrefs; } set { table_outrefs = value; } }

        public string Ver { get { return ver; } set { ver = value; } }

        public List<SupplierConfig> Supplier_configs { get { return supplier_configs; } set { supplier_configs = value; } }

        public bool Supplier_configs_defined { get { return supplier_configs_defined; } set { supplier_configs_defined = value; } }

        public uint Pagesize { get { return pagesize; } set { pagesize = value; } }

        public DBVer Version { get { return version; } set { version = value; } }

        public bool Get_readonly()
        {
            return _ReadOnly;
        }

        // Таблицы информационной базы
        private V8Table table_config;
        private V8Table table_configsave;
        private V8Table table_params;
        private V8Table table_files;
        private V8Table table_dbschema;
        private V8Table table_configcas;
        private V8Table table_configcassave;
        private V8Table table__extensionsinfo;

        // таблицы - хранилища файлов
        ConfigStorageTableConfig cs_config;
        ConfigStorageTableConfigSave cs_configsave;

        // Таблицы хранилища конфигураций
        private V8Table table_depot;
        private V8Table table_users;
        private V8Table table_objects;
        private V8Table table_versions;
        private V8Table table_labels;
        private V8Table table_history;
        private V8Table table_lastestversions;
        private V8Table table_externals;
        private V8Table table_selfrefs;
        private V8Table table_outrefs;

        private String ver;

        private List<SupplierConfig> supplier_configs; // конфигурации поставщика

        private bool supplier_configs_defined; // признак, что был произведен поиск конфигураций поставщика

        /// <summary>
        ///  Конструктор 
        /// </summary>
        public Tools1CD(String FileName1C)
        {
            pagesize = DEFAULT_PAGE_SIZE;
            Root81 root81 = new Root81();
            root81.Blocks = new UInt32[1];
            root81.Numblocks = 0;

            Data1CD = new FileStream(FileName1C, FileMode.Open);

            ReadPage0(); // читаем структуру первой страницы контейнера

            pagesize = Page0.pagesize;

            //Page0.sig = Encoding.UTF8.GetChars(buf, 0, 8);

            //Page0.ver1 = br.ReadByte();
            //Page0.ver2 = br.ReadByte();
            //Page0.ver3 = br.ReadByte();
            //Page0.ver4 = br.ReadByte();

            //Page0.length = br.ReadUInt32();
            //Page0.firstblock = br.ReadUInt32();
            //Page0.pagesize = br.ReadUInt32();


            Console.WriteLine($"Сигнатура файла... {Convert.ToString(Page0.sig)}");
            Console.WriteLine($"Ver1... {Page0.ver1}");
            Console.WriteLine($"Ver2... {Page0.ver2}");
            Console.WriteLine($"Ver3... {Page0.ver3}");
            Console.WriteLine($"Ver4... {Page0.ver4}");
            Console.WriteLine($"Page0.length... {Page0.length}");
            Console.WriteLine($"Page0.firstblock... {Page0.firstblock}");
            Console.WriteLine($"Page0.pagesize... {Page0.pagesize}");


            //length = ?(pagesize != 0): (UInt32)Data1CD.Length / pagesize, 0;
            if (pagesize != 0)
            {
                length = (UInt32)Data1CD.Length / pagesize;
                //Console.WriteLine('Размер страницы определить не удается...');
            }
            else
            {
                length = 0;
                Console.WriteLine($"Размер страницы определить не удается...");
            }

            //if (length * pagesize)




            String verDB = Page0.getver();

            if (verDB == "8.2.14.0")
            {
                version = DBVer.ver_8_2_14_0;
            }
            else if (verDB == "8.3.8.0")
            {
                version = DBVer.ver_8_3_8_0;
                pagesize = Page0.pagesize;
            }

            


        }

        V8Con Page0;

        public void ReadPage0()
        {

            BinaryReader br = new BinaryReader(data1CD);
            byte[] buf = new byte[100];

            buf = br.ReadBytes(8);

            Page0.sig = Encoding.UTF8.GetChars(buf, 0, 8);

            Page0.ver1 = br.ReadByte();
            Page0.ver2 = br.ReadByte();
            Page0.ver3 = br.ReadByte();
            Page0.ver4 = br.ReadByte();

            Page0.length     = br.ReadUInt32();
            Page0.firstblock = br.ReadUInt32();
            Page0.pagesize   = br.ReadUInt32();

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

        DBVer Getversion()
        {
            return DBVer.ver_8_2_14_0;
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
            Container_file f;
            TableFile tf;

            if (numcon >= Supplier_configs.Capacity)
                return false;

            tf = Supplier_configs[(int)numcon].File;

            f = new Container_file(tf, tf.Name);
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
        public UInt32 Getpagesize() { return Pagesize; }

        private String filename;
        private UInt32 pagesize; // размер одной страницы (до версии 8.2.14 всегда 0x1000 (4K), начиная с версии 8.3.8 от 0x1000 (4K) до 0x10000 (64K))

        FileStream fs;

        private DBVer version;

        private UInt32 length;        // длина базы в блоках

        private V8object free_blocks; // свободные блоки
        private V8object root_object; // корневой объект

        private Int32 num_tables;     // количество таблиц

        private V8Table[] tables;       // таблицы базы

        private bool _ReadOnly;

        private PageMapRec[] pagemap;         // Массив длиной length

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
                _files_config = new TableFiles(Table_config);
            }
            return _files_config;
        }

        private TableFiles Get_files_configsave()
        {
            if (_files_configsave != null)
            {
                _files_configsave = new TableFiles(Table_configsave);
            }
            return _files_configsave;
        }

        private TableFiles Get_files_params()
        {
            if (_files_params != null)
            {
                _files_params = new TableFiles(Table_params);
            }
            return _files_params;
        }

        private TableFiles Get_files_files()
        {
            if (_files_files != null)
            {
                _files_files = new TableFiles(Table_files);
            }
            return _files_files;
        }

        private TableFiles Get_files_configcas()
        {
            if (_files_configcas != null)
            {
                _files_configcas = new TableFiles(Table_configcas);
            }
            return _files_configcas;
        }

        private TableFiles Get_files_configcassave()
        {
            if (_files_configcassave != null) 
            {
                _files_configcassave = new TableFiles(Table_configcassave);
            }
            return _files_configcassave;
        }

        private void Init() { }

        public byte[] Getblock(UInt32 block_number)
        {
            
            if (Data1CD == null)
                return null;
            if (block_number >= length)
            {
                Console.WriteLine($"Попытка чтения блока за пределами файла. Индекс блока {block_number}. Всего блоков {length}");
                return null;
            }

            V8MemBlock tmpV8MemBlock = new V8MemBlock((FileStream)Data1CD, block_number, false, true);
            return V8MemBlock.GetBlock((FileStream)Data1CD, block_number);
        }

        public bool Getblock(ref byte[] buf, UInt32 block_number, Int32 blocklen = -1) // буфер принадлежит вызывающей процедуре
        {
            if (Data1CD == null)
                return false;

            if (blocklen < 0)
                blocklen = (Int32)Pagesize;

            if (block_number >= length)
            {
                Console.WriteLine($"Попытка чтения блока за пределами файла. Индекс блока {block_number}, всего блоков {length}");
                return false;
            }

            //memcpy(buf, MemBlock::getblock(fs, block_number), blocklen);


            V8MemBlock tmp_mem_block = new V8MemBlock((FileStream)Data1CD, block_number, false, true);
            byte[] tmp_buf = V8MemBlock.GetBlock((FileStream)Data1CD, block_number);
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

        private void Add_supplier_config(TableFile file) { }

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
            pagemap = new PageMapRec[length];

            pagemap[0].Type = PageType.root;
            pagemap[1].Type = PageType.freeroot;
            pagemap[2].Type = PageType.rootfileroot;

        }

        private String Pagemaprec_presentation(PageMapRec pmr)
        {
            switch (pmr.Type)
            {
                case PageType.lost:          return ("потерянная страница");
                case PageType.root:          return ("корневая страница базы");
                case PageType.freeroot:      return ("корневая страница таблицы свободных блоков");
                case PageType.freealloc:     return ("страница размещения таблицы свободных блоков номер ") + pmr.Number;
                case PageType.free:          return ("свободная страница номер ")                           + pmr.Number;
                case PageType.rootfileroot:  return ("корневая страница корневого файла");                  
                case PageType.rootfilealloc: return ("страница размещения корневого файла номер ")          + pmr.Number;
                case PageType.rootfile:      return ("страница данных корневого файла номер ")              + pmr.Number;
                case PageType.descrroot:     return ("корневая страница файла descr таблицы ")              + tables[pmr.Tab].Getname();
                case PageType.descralloc:    return ("страница размещения файла descr таблицы ")            + tables[pmr.Tab].Getname() + " номер " + pmr.Number;
                case PageType.descr:         return ("страница данных файла descr таблицы ")                + tables[pmr.Tab].Getname() + " номер " + pmr.Number;
                case PageType.dataroot:      return ("корневая страница файла data таблицы ")               + tables[pmr.Tab].Getname();
                case PageType.dataalloc:     return ("страница размещения файла data таблицы ")             + tables[pmr.Tab].Getname() + " номер " + pmr.Number;
                case PageType.data:          return ("страница данных файла data таблицы ")                 + tables[pmr.Tab].Getname() + " номер " + pmr.Number;
                case PageType.indexroot:     return ("корневая страница файла index таблицы ")              + tables[pmr.Tab].Getname();
                case PageType.indexalloc:    return ("страница размещения файла index таблицы ")            + tables[pmr.Tab].Getname() + " номер " + pmr.Number;
                case PageType.index:         return ("страница данных файла index таблицы ")                + tables[pmr.Tab].Getname() + " номер " + pmr.Number;
                case PageType.blobroot:      return ("корневая страница файла blob таблицы ")               + tables[pmr.Tab].Getname();
                case PageType.bloballoc:     return ("страница размещения файла blob таблицы ")             + tables[pmr.Tab].Getname() + " номер " + pmr.Number;
                case PageType.blob:          return ("страница данных файла blob таблицы ")                 + tables[pmr.Tab].Getname() + " номер " + pmr.Number;

                default:
                    return ("??? неизвестный тип страницы ???");
            }
        }

        private DepotVer Get_depot_version(byte[] record)
        {
            DepotVer depotVer = DepotVer.UnknownVer;

            V8Field fldd_depotver = Table_depot.Get_field("DEPOTVER");

            if (fldd_depotver != null)
            {
                String Ver = fldd_depotver.Get_presentation(record, true);

                if (String.Compare(Ver, "0300000000000000") == 0)
                {
                    depotVer = DepotVer.Ver3;
                }
                else if (String.Compare(Ver, "0500000000000000") == 0)
                {
                    depotVer = DepotVer.Ver5;
                }
                else if (String.Compare(Ver, "0600000000000000") == 0)
                {
                    depotVer = DepotVer.Ver6;
                }
                else if (String.Compare(Ver, "0700000000000000") == 0)
                {
                    depotVer = DepotVer.Ver7;
                }
                else
                {
                    depotVer = DepotVer.UnknownVer;

                    //msreg_m.AddMessage_("Неизвестная версия хранилища", MessageState::Error, "Версия хранилища", Ver);
                    Console.WriteLine("Неизвестная версия хранилища");
                }

                return depotVer;
            }

            return depotVer;

        }

    } // Окончание класса Tools1CD

}
