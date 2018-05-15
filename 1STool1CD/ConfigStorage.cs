using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace _1STool1CD
{
    /// <summary>
    /// Структура открытого файла адаптера контейнера конфигурации
    /// </summary>
    public struct ConfigFile
    {
        private Stream str;
        private char[] addin;

        public Stream Str { get { return str; } set { str = value; } }

        public char[] Addin { get { return addin; } set { addin = value; } }
    }

    /// <summary>
    /// Перечисление признака упакованности файла
    /// </summary>
    public enum table_file_packed
    {
        unknown,
	    no,
	    yes
    }

    /// <summary>
    /// Структура файла контейнера файлов
    /// </summary>
    public class Container_file
    {
        private Table_file file;
        private String name; // Приведенное имя (очищенное от динамического обновления)
        private Stream stream;
        private Stream rstream; // raw stream (нераспакованный поток)
        private V8catalog cat;
        private table_file_packed packed;
        private int dynno; // Номер (индекс) динамического обновления (0, 1 и т.д.). Если без динамического обновления, то -1, если UID динамического обновления не найден, то -2. Для пропускаемых файлов -3.

        public Table_file File { get { return file; } set { file = value; } }

        public string Name { get { return name; } set { name = value; } }

        public Stream Stream { get { return stream; } set { stream = value; } }

        public Stream Rstream { get { return rstream; } set { rstream = value; } }

        public V8catalog Cat { get { return cat; } set { cat = value; } }

        public table_file_packed Packed { get { return packed; } set { packed = value; } }

        public int Dynno { get { return dynno; } set { dynno = value; } }

        public Container_file(Table_file _f, String _name)
        {
            File = _f;
            Name = _name;
            Stream = null;
            Rstream = null;
            Cat = null;
            Packed = table_file_packed.unknown;
            Dynno = -3;
        }

        public bool open() { return true; }
        public bool ropen() { return true; } // raw open
        public void close() {  }
        public bool isPacked() { return true; }
    }
    /// <summary>
    /// Базовый класс адаптеров контейнеров конфигурации
    /// </summary>
    public class ConfigStorage
    {

        public ConfigStorage() { }

        // Если файл не существует, возвращается NULL
        public virtual ConfigFile readfile(String path) 
        {
            return new ConfigFile();
        }
        public virtual bool writefile(String path, Stream str) { return true; }
        public virtual String presentation() { return " "; }
        public virtual void close(ConfigFile cf) { }
        public virtual bool fileexists(String path) { return true; }
    }

    /// <summary>
    /// Класс адаптера контейнера конфигурации - Директория
    /// </summary>
    public class ConfigStorageDirectory : ConfigStorage
    {

        private String fdir;

        public ConfigStorageDirectory(String _dir) { }
        public override ConfigFile readfile(String path) { return new ConfigFile(); }
        public override bool writefile(String path, Stream str) { return true; }
        public override String presentation() { return " "; }
        public override void close(ConfigFile cf)  { }
        public override bool fileexists(String path) { return true; }

    }

    /// <summary>
    /// Класс адаптера контейнера конфигурации - cf (epf, erf, cfe) файл
    /// </summary>
    class ConfigStorageCFFile : ConfigStorage
    {
    
	    private String filename;
        private V8catalog cat;

        public ConfigStorageCFFile(String fname) { }

        public override ConfigFile readfile(String path) { return new ConfigFile(); }
        public override bool writefile(String path, Stream str) { return true; }
        public override String presentation() { return " "; }
        public override void close(ConfigFile cf) { }
    	public override bool fileexists(String path) { return true; }

    }

    /// <summary>
    /// Базовый класс адаптера таблицы - контейнера конфигурации (CONFIG, CONFICAS, CONFIGSAVE, CONFICASSAVE)
    /// </summary>
    public class ConfigStorageTable : ConfigStorage
    {

        public ConfigStorageTable(T_1CD _base = null) : base(){}

        public override ConfigFile readfile(String path) { return new ConfigFile(); }
        public override bool writefile(String path, Stream str) { return true; }
        public override void close(ConfigFile cf) { }
        /// <summary>
        /// сохранение конфигурации в файл
        /// </summary>
        /// <param name="_filename"></param>
        /// <returns></returns>
	    public bool save_config(String _filename) { return true; }
        public bool getready() { return ready; }
        public override bool fileexists(String path) { return true; }


        protected SortedDictionary<String, Container_file> files;

        protected bool ready = false;

	    private T_1CD base_; // установлена, если база принадлежит адаптеру конфигурации
    }
    /// <summary>
    /// Класс адаптера таблицы - контейнера конфигурации CONFIG (конфигурации базы данных)
    /// </summary>
    public class ConfigStorageTableConfig : ConfigStorageTable
    {

        public ConfigStorageTableConfig(TableFiles tabf, T_1CD _base = null) { }
        public override String presentation() { return " "; }

        private String present;
    }

    /// <summary>
    /// Класс адаптера таблицы - контейнера конфигурации CONFIGSAVE (основной конфигурации)
    /// </summary>
    public class ConfigStorageTableConfigSave : ConfigStorageTable
    {

        public ConfigStorageTableConfigSave(TableFiles tabc, TableFiles tabcs, T_1CD _base = null) { }
        public override String presentation() { return " "; }

        private String present;
    }

    /// <summary>
    /// Класс адаптера таблицы - контейнера конфигурации CONFIGCAS (расширения конфигурации базы данных)
    /// </summary>
    public class ConfigStorageTableConfigCas : ConfigStorageTable
    {

        public ConfigStorageTableConfigCas(TableFiles tabc, String configver, T_1CD _base = null) { }
        public override String presentation() { return " "; }

        private String present;
    }

    /// <summary>
    /// Класс адаптера таблицы - контейнера конфигурации CONFIGCASSAVE (расширения основной конфигурации)
    /// </summary>
    public class ConfigStorageTableConfigCasSave : ConfigStorageTable
    {
        public ConfigStorageTableConfigCasSave(TableFiles tabc, TableFiles tabcs, Guid uid, String configver, T_1CD _base = null) { }
        public override String presentation() { return " "; }

        private	String present;
    }


}
