using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static _1STool1CD.Utils1CD;
using static _1STool1CD.Constants;

namespace _1STool1CD
{

    public class Field
    {
        #region public

        public bool null_index_initialized = false;

        /// <summary>
        /// Конструктор основной
        /// </summary>
        /// <param name="_parent"></param>
        public Field(Table _parent)
        {
            if (!null_index_initialized)
            {
                null_index[0] = 1;
                Array.Clear(null_index, 1, 0x1000 - 1);
                null_index_initialized = true;
            }

            parent = _parent;
            len = 0;
            offset = 0;
            name = "";
        }

        /// <summary>
        /// возвращает длину поля в байтах
        /// </summary>
        /// <returns></returns>
        public Int32 getlen()
        {
            return (null_exists ? 1 : 0) + type_manager.getlen();
        }

        /// <summary>
        /// Возвращает имя
        /// </summary>
        /// <returns></returns>
        public String getname()
        {
            return name;
        }

        public String get_presentation(Char[] rec, bool EmptyNull = false, Char Delimiter = ',', bool ignore_showGUID = false, bool detailed = false)
        {
            //const char* fr = rec + offset;
            Char[] fr = new Char[0x1000];
            Char[] fr2 = new Char[0x1000];

            fr[0] = rec[offset]; // возможно здесь надо скопировать весь массив из rec в fr
                //= rec[0 + offset];
            if (getnull_exists())
            {
                if (fr[0] == 0)
                {
                    return EmptyNull ? "" : "{NULL}";
                }
                //fr++;
                fr.CopyTo(fr2, 1);
            }
            return type_manager.get_presentation(fr2, EmptyNull, Delimiter, ignore_showGUID, detailed);
        }

        public String get_XML_presentation(byte[] rec, bool ignore_showGUID = false)
        {
            byte[] fr = new byte[0x1000];
            byte[] fr2 = new byte[0x1000];

            fr[0] = rec[offset];
            
            if (null_exists)
            {
                if (fr[0] == 0)
                {
                    return "";
                }
                fr.CopyTo(fr2, 1);
            }
            return type_manager.get_XML_presentation(fr2, parent, ignore_showGUID);
        }



        // char *binary_value => byte[] binary_value
        public bool get_binary_value(byte[] binary_value, bool NULL, String value)
        {
            Array.Clear(binary_value, 0, len);
            byte[] binary_value2 = new byte[len + 1];

            if (null_exists)
            {
                if (NULL)
                {
                    return true;
                }
                binary_value[0] = 1;
                binary_value.CopyTo(binary_value2, 1);
                
            }
            return type_manager.get_binary_value(binary_value2, value);
        }

        public Constants.type_fields gettype()
        {
            return type_manager.gettype();
        }

        public Table getparent()
        {
            return parent;
        }

        public bool getnull_exists()
        {
            return null_exists;
        }

        public Int32 getlength()
        {
            return type_manager.getlength();
        }

        public Int32 getprecision()
        {
            return type_manager.getprecision();
        }

        public bool getcase_sensitive()
        {
            return type_manager.getcase_sensitive();
        }

        public Int32 getoffset()
        {
            return offset;
        }

        public String get_presentation_type()
        {
            return type_manager.get_presentation_type();
        }

        public bool save_blob_to_file(char[] rec, String filename, bool unpack) { return true; }

        // char[] rec => byte[] rec, хотя может и String rec надо сделать
        // char[] SortKey => byte[] SortKey
        public UInt32 getSortKey(byte[] rec, byte[] SortKey, Int32 maxlen)
        {
            //const char* fr = rec + offset;
            byte[] fr = new byte[0x1000];
            byte[] fr2 = new byte[0x1000];

            if (null_exists)
            {
                if (fr[0] == 0)
                {
                    SortKey[1] = 0;
                    null_index.CopyTo(SortKey, 0);
                    return 0;
                }
                SortKey[1] = 1; //*(SortKey++) = 1;

                fr.CopyTo(fr2, 1); // fr++;

            }

            try
            {

                return type_manager.getSortKey(fr2, SortKey, maxlen);

            }
            catch
            {

                /*
            catch (SerializationException &exception) {
                exception.add_detail("Таблица", parent->name)
                        .add_detail("Поле", name)
                        .show();
                        */
                Console.WriteLine("Таблица Беда бедища");
            }
            return 0;
        }

        public static Field field_from_tree(tree field_tree, bool has_version, Table parent)
        {

            Field fld = new Field(parent);

            

            if (field_tree.get_type() != node_type.nd_string)
            {
                //throw FieldStreamParseException("Ошибка получения имени поля таблицы. Узел не является строкой.");
                //Console.WriteLine($"Ошибка формата потока. Лишняя закрывающая скобка. В позиции: { i }, Путь: {path}");
                Console.WriteLine($"Ошибка получения имени поля таблицы. Узел не является строкой.");

            }
            fld.name = field_tree.get_value();

            field_tree = field_tree.get_next();

            field_type_declaration type_declaration;
            try
            {

                type_declaration = field_type_declaration.parse_tree(field_tree);

            }
            //catch (FieldStreamParseException &formatError) {
            //    throw formatError.add_detail("Поле", fld->name);
            catch
            {
                Console.WriteLine($"Поле {fld.name}");
            }

            fld.type = type_declaration.type;
            fld.null_exists = type_declaration.null_exists;
            fld.type_manager = FieldType.create_type_manager(type_declaration);

            if (fld.type == type_fields.tf_version)
            {
                has_version = true;
            }
            return fld;

        }
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
        private int[] null_index = new int[0x1000];
        //private static bool null_index_initialized;

        #endregion
    }
}
