using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _1STool1CD
{
    /// <summary>
    /// Структура адреса файла таблицы-контейнера файлов
    /// </summary>
    public struct table_blob_file
    {
        private UInt32 blob_start;
        private UInt32 blob_length;

        public uint Blob_start { get { return blob_start; } set { blob_start = value; } }
        public uint Blob_length { get { return blob_length; } set { blob_length = value; } }
    }

    /// <summary>
    /// Структура записи таблицы контейнера файлов
    /// </summary>
    public struct table_rec
    {
        private String name;
        private table_blob_file addr;
        private Int32 partno;
        private DateTime ft_create;
        private DateTime ft_modify;

        public string Name { get { return name; } set { name = value; } }
        public table_blob_file Addr { get { return addr; } set { addr = value; } }
        public int Partno { get { return partno; } set { partno = value; } }
        public DateTime Ft_create { get { return ft_create; } set { ft_create = value; } }
        public DateTime Ft_modify { get { return ft_modify; } set { ft_modify = value; } }
    };

    public class TableFiles
    {
        private V8Table table;
        private SortedDictionary<String, Table_file> allfiles;

        private char[] record;
        private bool ready = false;

        private bool test_table() { return true; }

        public TableFiles(V8Table t)
        { }

        public bool getready() { return ready; }
        //public table_file getfile(String name) { return null; }
        public V8Table gettable() { return table; }
        public SortedDictionary<String, Table_file> files() { return null; }
        

    }
}
