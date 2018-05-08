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
        public UInt32 blob_start;
        public UInt32 blob_length;
    }

    /// <summary>
    /// Структура записи таблицы контейнера файлов
    /// </summary>
    public struct table_rec
    {
        public String name;
        public table_blob_file addr;
        public Int32 partno;
        public DateTime ft_create;
        public DateTime ft_modify;
    };

    public class TableFiles
    {
        private V8Table table;
        private SortedDictionary<String, table_file> allfiles;

        private char[] record;
        private bool ready = false;

        private bool test_table() { return true; }

        public TableFiles(V8Table t)
        { }

        public bool getready() { return ready; }
        //public table_file getfile(String name) { return null; }
        public V8Table gettable() { return table; }
        public SortedDictionary<String, table_file> files() { return null; }
        

    }
}
