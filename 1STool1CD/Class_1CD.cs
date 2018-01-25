using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

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
        UInt32 numblocks;
        UInt32[] blocks;
    };
    
    public struct root_80
    {
        char[] lang; // 8
        UInt32 numblocks;
        UInt32[] blocks;
    };

    public struct root_81
    {
        char[] lang; //32
        UInt32 numblocks;
        UInt32[] blocks;
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
        Int32 tab;     // Индекс в T_1CD::tables, -1 - страница не относится к таблицам
        pagetype type; // тип страницы
        UInt32 number; // номер страницы в своем типе
        pagemaprec(Int32 _tab = -1, pagetype _type = pagetype.lost, UInt32 _number = 0)
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
	    //public table_file* file;
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
    /// v8object
    /// </summary>
    public class v8object // Надо реализовывать
    {
    }

    /// <summary>
    /// Index
    /// </summary>
    public class Index // Надо реализовывать
    {
    }

    /// <summary>
    /// Index
    /// </summary>
    public class Field // Надо реализовывать
    {
    }

    /// <summary>
    /// ConfigStorageTableConfig
    /// </summary>
    public class ConfigStorageTableConfig // Надо реализовывать
    {
    }

    /// <summary>
    /// ConfigStorageTableConfigSave
    /// </summary>
    public class ConfigStorageTableConfigSave // Надо реализовывать
    {
    }

    public class v8catalog
    {
    }

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

        public bool is_open() { return true; }
        UInt32 get_numtables() { return 100; }
        Table gettable(UInt32 numtable) { return (Table)null; }
        Utils1CD.db_ver getversion() { return Utils1CD.db_ver.ver8_2_14_0; }

        public bool save_config(String filename) { return true; }
        public bool save_configsave(String filename) { return true; }
        public void find_supplier_configs() { }
        public bool save_supplier_configs(UInt32 numcon, String filename) { return true; }
        public bool save_depot_config(String _filename, UInt32 ver = 0) { return true; }
        public bool save_part_depot_config(String _filename, Int32 ver_begin, Int32 ver_end) { return true; }
	    public Int32 get_ver_depot_config(Int32 ver) { return 100; } // Получение номера версии конфигурации (0 - последняя, -1 - предпоследняя и т.д.)
        public bool save_config_ext(String _filename, String uid, String hashname) { return true; }
        public bool save_config_ext_db(String _filename, String hashname) { return true; }

        public bool get_readonly() { return true; }
        public void set_readonly(bool ro) { }
        public void flush() {  }

        public bool test_stream_format() { return true; }
        public bool test_list_of_tables() { return true; } // проверка списка таблиц (по DBNames)
        public void find_lost_objects() { }
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
        private UInt32 pagesize; // размер одной страницы (до версии 8.2.14 всегда 0x1000 (4K), начиная с версии 8.3.8 от 0x1000 (4K) до 0x10000 (64K))

        FileStream fs;

        private Utils1CD.db_ver version; // версия базы

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

        private TableFiles get_files_config() { return (TableFiles)null; }
        private TableFiles get_files_configsave() { return (TableFiles)null; }
        private TableFiles get_files_params() { return (TableFiles)null; }
        private TableFiles get_files_files() { return (TableFiles)null; }
        private TableFiles get_files_configcas() { return (TableFiles)null; }
        private TableFiles get_files_configcassave() { return (TableFiles)null; }

        private void init() { }
        private bool getblock(byte[] buf, UInt32 block_number, Int32 blocklen = -1) { return true; } // буфер принадлежит вызывающей процедуре

        private char getblock(UInt32 block_number) { return ' '; }           // буфер не принадлежит вызывающей стороне (принадлежит memblock)
        private char getblock_for_write(UInt32 block_number, bool read) { return ' '; } // буфер не принадлежит вызывающей стороне (принадлежит memblock)
        private void set_block_as_free(UInt32 block_number) { } // пометить блок как свободный
        private UInt32 get_free_block() { return 100; } // получить номер свободного блока (и пометить как занятый)

        private void add_supplier_config(table_file file) { }

        private bool recursive_test_stream_format(Table t, UInt32 nrec) { return true; }
        private bool recursive_test_stream_format2(Table t, UInt32 nrec) { return true; } // для DBSCHEMA
        private bool recursive_test_stream_format(Stream str, String path, bool maybezipped2 = false) { return true; }
        private bool recursive_test_stream_format(v8catalog cat, String path) { return true; }

        private void pagemapfill() { }
        private String pagemaprec_presentation(pagemaprec pmr) { return " "; }

        private depot_ver get_depot_version(char record) { return depot_ver.Ver3; }

    } // Окончание класса T_1CD

}
