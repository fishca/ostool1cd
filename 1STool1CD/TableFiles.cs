using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static _1STool1CD.Structures;

namespace _1STool1CD
{

    public class TableFiles
    {
        private V8Table table;
        private SortedDictionary<String, TableFile> allfiles;

        private char[] record;
        private bool ready = false;

        private bool test_table() { return true; }

        public TableFiles(V8Table t)
        { }

        public bool getready() { return ready; }
        //public table_file getfile(String name) { return null; }
        public V8Table gettable() { return table; }
        public SortedDictionary<String, TableFile> files() { return null; }
        

    }
}
