using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _1STool1CD
{
    public class tree // надо реализовывать
    { }

    public class FieldType // надо реализовывать
    { }

    public class Field
    {
        #region public
        public Field(Table _parent) { }

        /// <summary>
        /// возвращает длину поля в байтах
        /// </summary>
        /// <returns></returns>
        public Int32 getlen() { return 0; }
        public String getname() { return " "; }

        public String get_presentation(char[] rec, bool EmptyNull = false, char Delimiter = '0', bool ignore_showGUID = false, bool detailed = false) { return ""; }

        public String get_XML_presentation( char[] rec, bool ignore_showGUID = false) { return ""; }

        public bool get_binary_value(char[] buf, bool NULL, String value) { return true; }
        public Constants.type_fields gettype() { return Constants.type_fields.tf_binary; }
        public Table getparent() { return new Table(); }
        public bool getnull_exists() { return true; }
        public Int32 getlength() { return 0; }
        public Int32 getprecision() { return 0; }
        public bool getcase_sensitive() { return true; }
        public Int32 getoffset() { return 0; }
        public String get_presentation_type() { return " "; }
        public bool save_blob_to_file(char[] rec, String filename, bool unpack) { return true; }
        public UInt32 getSortKey(char[] rec, char[] SortKey, Int32 maxlen) { return 0; }

        public static Field field_from_tree(tree field_tree, bool has_version, Table parent) { return new Field(new Table()); }
        #endregion

        #region private

        private String name;
        private Constants.type_fields type;
        private bool null_exists = false;
        private FieldType type_manager;

        private Table parent;
        private Int32 len; // длина поля в байтах
        private Int32 offset; // смещение поля в записи
        private static char[] buf;
        private static char[] null_index;
        private static bool null_index_initialized;

        #endregion
    }
}
