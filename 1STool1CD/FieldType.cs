using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _1STool1CD
{
    public struct field_type_declaration
    {
        public Constants.type_fields type;
        public bool null_exists;
        public Int32 length;
        public Int32 precision;
        public bool case_sensitive;

        public static field_type_declaration parse_tree(tree field_tree) { return new field_type_declaration(); }
    }

    public class FieldType
    {
        public virtual Constants.type_fields gettype() { return Constants.type_fields.tf_binary; }
        public virtual Int32 getlength() { return 0; }
        public virtual Int32 getlen() { return 0; }
        public virtual Int32 getprecision() { return 0; }
        public virtual bool getcase_sensitive() { return true; }
        public virtual String get_presentation_type() { return " "; }

        public virtual String get_presentation(char[] rec, bool EmptyNull, char Delimiter, bool ignore_showGUID, bool detailed) { return " "; }

        public virtual bool get_binary_value(byte[] buf, String value) { return true; }

        public virtual String get_XML_presentation(byte[] rec, Table parent, bool ignore_showGUID) { return " "; }

        public virtual UInt32 getSortKey(byte[] rec, byte[] SortKey, Int32 maxlen) { return 0; }


        public static FieldType create_type_manager(field_type_declaration type_declaration) { return (FieldType)null; }
	    public static FieldType Version8() { return (FieldType)null; }

        // TODO: убрать это куда-нибудь
        public static bool showGUIDasMS; // Признак, что GUID надо преобразовывать по стилю MS (иначе по стилю 1С)
        public static bool showGUID;

    }
}
