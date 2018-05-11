using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static _1STool1CD.Constants;

namespace _1STool1CD
{
    public struct Field_type_declaration
    {
        public Type_fields type;
        public bool null_exists;
        public Int32 length;
        public Int32 precision;
        public bool case_sensitive;

        public static Field_type_declaration Parse_tree(Tree field_tree) { return new Field_type_declaration(); }
    }

    public class FieldType
    {
        public virtual Type_fields Gettype() { return Type_fields.tf_binary; }
        public virtual Int32 Getlength() { return 0; }
        public virtual Int32 Getlen() { return 0; }
        public virtual Int32 Getprecision() { return 0; }
        public virtual bool Getcase_sensitive() { return true; }
        public virtual String Get_presentation_type() { return " "; }

        public virtual String Get_presentation(char[] rec, bool EmptyNull, char Delimiter, bool ignore_showGUID, bool detailed) { return " "; }

        public virtual bool Get_binary_value(byte[] buf, String value) { return true; }

        public virtual String Get_XML_presentation(byte[] rec, V8Table parent, bool ignore_showGUID) { return " "; }

        public virtual UInt32 GetSortKey(byte[] rec, byte[] SortKey, Int32 maxlen) { return 0; }


        public static FieldType Create_type_manager(Field_type_declaration type_declaration) { return (FieldType)null; }
        public static FieldType Version8() { return (FieldType)null; }

        // TODO: убрать это куда-нибудь
        public static bool showGUIDasMS; // Признак, что GUID надо преобразовывать по стилю MS (иначе по стилю 1С)
        public static bool showGUID;

    }

    public class CommonFieldType : FieldType
    {
        public CommonFieldType(Field_type_declaration declaration) 
        {
            //declaration.type = Constants.type_fields.
        }

        public override Type_fields Gettype()
	    {
            return type;
	    }

        public override int Getlength() 
	    {
		    return length;
	    }

        public override int Getprecision()
	    {
		    return precision;
	    }

        public override bool Getcase_sensitive()
	    {
		    return case_sensitive;
	    }

        public override String Get_presentation_type()
	    {
		    switch(type)
		    {
			case Type_fields.tf_binary:    return "binary";
			case Type_fields.tf_bool:      return "bool";
			case Type_fields.tf_numeric:   return "number";
			case Type_fields.tf_char:      return "fixed string";
			case Type_fields.tf_varchar:   return "string";
			case Type_fields.tf_version:   return "version";
			case Type_fields.tf_string:    return "memo";
			case Type_fields.tf_text:      return "text";
			case Type_fields.tf_image:     return "image";
			case Type_fields.tf_datetime:  return "datetime";
			case Type_fields.tf_version8:  return "hidden version";
			case Type_fields.tf_varbinary: return "var binary";
		}
		return "{?}";
	}
        //---------------------------------------------------------------------------
        public override int Getlen()
	    {

            if (len != 0) return len;

		    switch(type)
		    {
			    case Type_fields.tf_binary:    len += length;            break;
			    case Type_fields.tf_bool:      len += 1;                 break;
			    case Type_fields.tf_numeric:   len += (length + 2) >> 1; break;
			    case Type_fields.tf_char:      len += length* 2;         break;
			    case Type_fields.tf_varchar:   len += length* 2 + 2;     break;
			    case Type_fields.tf_version:   len += 16;                break;
			    case Type_fields.tf_string:    len += 8;                 break;
			    case Type_fields.tf_text:      len += 8;                 break;
			    case Type_fields.tf_image:     len += 8;                 break;
			    case Type_fields.tf_datetime:  len += 7;                 break;
			    case Type_fields.tf_version8:  len += 8;                 break;
			    case Type_fields.tf_varbinary: len += length + 2;        break;
		    }
		    return len;
	    }

        /*
        public override String get_presentation(byte[] rec, bool EmptyNull, Char Delimiter, bool ignore_showGUID, bool detailed)
        {
            return "";
        }
        */

        /*
        public override String get_fast_presentation(byte[] rec)
        {
            return "";
        }
        */

        public override bool Get_binary_value(byte[] buf, String value)
        {
            return true;
        }

        /*
        public override String get_XML_presentation(byte rec, Table parent, bool ignore_showGUID)
        {
            return "";
        }
        */

        public override uint GetSortKey(byte[] rec, byte[] SortKey, int maxlen)
        {
            return 0;
        }

        public Type_fields type = Type_fields.tf_binary;
        int length = 0;
        int precision = 0;
        bool case_sensitive = false;

        int len = 0;

    }
}
