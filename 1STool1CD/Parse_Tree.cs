using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace _1STool1CD
{
    class Parse_Tree
    {
    }

    public enum node_type
    {
        nd_empty = 0,       // пусто
        nd_string = 1,      // строка
        nd_number = 2,      // число
        nd_number_exp = 3,  // число с показателем степени
        nd_guid = 4,        // уникальный идентификатор
        nd_list = 5,        // список
        nd_binary = 6,      // двоичные данные (с префиксом #base64:)
        nd_binary2 = 7,     // двоичные данные формата 8.2 (без префикса)
        nd_link = 8,        // ссылка
        nd_binary_d = 9,    // двоичные данные (с префиксом #data:)
        nd_unknown          // неизвестный тип
    }

    public enum _state
    {
        s_value,              // ожидание начала значения
        s_delimitier,         // ожидание разделителя
        s_string,             // режим ввода строки
        s_quote_or_endstring, // режим ожидания конца строки или двойной кавычки
        s_nonstring           // режим ввода значения не строки
    }



    public class tree
    {
        public static readonly String exp_number     = "^-?\\d+$"; // exp_number
        public static readonly String exp_number_exp = "^-?\\d+(\\.?\\d*)?((e|E)-?\\d+)?$";
        public static readonly String exp_guid       = "^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$";
        public static readonly String exp_binary     = "^#base64:[0-9a-zA-Z\\+=\\r\\n\\/]*$";
        public static readonly String exp_binary2    = "^[0-9a-zA-Z\\+=\\r\\n\\/]+$";
        public static readonly String exp_link       = "^[0-9]+:[0-9a-fA-F]{32}$";
        public static readonly String exp_binary_d   = "^#data:[0-9a-zA-Z\\+=\\r\\n\\/]*$";

        public String value;
        public node_type type;
        public int num_subnode; // количество подчиненных
        public tree parent;     // +1
        public tree next;       // 0
        public tree prev;       // 0
        public tree first;      // -1
        public tree last;       // -1
        public uint index;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="_value"></param>
        /// <param name="_type"></param>
        /// <param name="_parent"></param>
        public tree(String _value, node_type _type, tree _parent)
        {
            value  = _value;
            type   = _type;
            parent = _parent;

            num_subnode = 0;
            index = 0;

            if (parent != null)
            {
                parent.num_subnode++;
                prev = parent.last;

                if (prev != null)
                {
                    prev.next = this;
                    index = prev.index + 1;
                }
                else
                    parent.first = this;

                parent.last = this;
            }
            else
                prev = null;

            next  = null;
            first = null;
            last  = null;
        }

        public tree add_child(String _value, node_type _type)
        {
            return new tree(_value, _type, this);
        }

        public tree add_child()
        {
            return new tree("", node_type.nd_empty, this);
        }

        public tree add_node()
        {
            return new tree("", node_type.nd_empty, this.parent);
        }

        public String get_value()
        {
            return value;
        }

        public node_type get_type()
        {
            return type;
        }

        public int get_num_subnode()
        {
            return num_subnode;
        }

        public tree get_subnode(int _index)
        {
            if (_index >= num_subnode)
                return null;

            tree t = first;

            while (_index != 0)
            {
                t = t.next;
                --_index;
            }
            return t;
        }

        public tree get_subnode(String node_name)
        {
            tree t = first;
            while (t != null)
            {
                if (t.value == node_name)
                    return t;

                t = t.next;
            }
            return null;
        }

        public tree get_next()
        {
            return next;
        }

        public tree get_parent()
        {
            return parent;
        }

        public tree get_first()
        {
            return first;
        }

        public tree get_last()
        {
            return last;
        }


        //public tree operator [] (int _index);


        public void set_value(String v, node_type t)
        {
            value = v;
            type = t;
        }

        public void outtext(ref String text)
        {
            node_type lt = node_type.nd_unknown;

            if (num_subnode != 0)
            {
                if (text.Length != 0)
                    text += "\r\n";

                text += "{";
                tree t = first;
                while (t != null)
                {
                    t.outtext(ref text);
                    lt = t.type;
                    t = t.next;
                    if (t != null)
                        text += ",";
                }
                if (lt == node_type.nd_list)
                    text += "\r\n";
                text += "}";
            }
            else
            {
                switch (type)
                {
                    case node_type.nd_string:
                        text += "\"";
                        text += text.Replace("\"", "\"\"");
                        text += "\"";
                        break;
                    case node_type.nd_number:
                    case node_type.nd_number_exp:
                    case node_type.nd_guid:
                    case node_type.nd_list:
                    case node_type.nd_binary:
                    case node_type.nd_binary2:
                    case node_type.nd_link:
                    case node_type.nd_binary_d:
                        text += value;
                        break;
                    default:
                        break;
                }
            }

        }
        public String path()
        {
            String p = "";
            tree t;

            if (this == null)
                return ":??"; //-V704

            for (t = this; t.parent != null; t = t.parent)
            {
                //p = String(":") + t->index + p;
                p = ":" + t.index + p;
            }
            return p;
        }

        /// <summary>
        /// Парсинг скобочного дерева 1С
        /// </summary>
        /// <param name="text"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static tree parse_1Ctext(String text, String path)
        {

            StringBuilder __curvalue__ = new StringBuilder("");

            String curvalue = "";
            tree ret = new tree("", node_type.nd_list, null);
            tree t = ret;
            int len = text.Length;
            int i = 0;
            char sym = '0';
            node_type nt = node_type.nd_unknown;

            _state state = _state.s_value;

            //for (i = 0; i <= len-1; i++)
            for (i = 1; i < len; i++)
            {
                sym = text[i];

                if (String.IsNullOrEmpty(sym.ToString())) break;

                switch (state)
                {
                    case _state.s_value:
                        switch (sym)
                        {
                            case ' ': // space
                            case '\t':
                            case '\r':
                            case '\n':
                                break;
                            case '"':

                                __curvalue__.Clear();
                                state = _state.s_string;
                                break;

                            case '{':

                                t = new tree("", node_type.nd_list, t);
                                break;

                            case '}':

                                if (t.get_first() != null)
                                    t.add_child("", node_type.nd_empty);

                                t = t.get_parent();

                                if (t == null)
                                {
                                    //if (msreg) msreg->AddError("Ошибка формата потока. Лишняя закрывающая скобка }.", "Позиция", i, "Путь", path);
                                    //delete ret;
                                    //String msreg = $"Ошибка формата потока. Лишняя закрывающая скобка. Позиция: { i }, Путь: {path}";
                                    Console.WriteLine($"Ошибка формата потока. Лишняя закрывающая скобка. В позиции: { i }, Путь: {path}");
                                    ret = null;
                                    return null;
                                }
                                state = _state.s_delimitier;
                                break;

                            case ',':

                                t.add_child("", node_type.nd_empty);
                                break;

                            default:

                                __curvalue__.Clear();
                                __curvalue__.Append(sym);
                                state = _state.s_nonstring;

                                break;
                        }
                        break;
                    case _state.s_delimitier:
                        switch (sym)
                        {
                            case ' ': // space
                            case '\t':
                            case '\r':
                            case '\n':
                                break;
                            case ',':
                                state = _state.s_value;
                                break;
                            case '}':
                                t = t.get_parent();
                                if (t == null)
                                {
                                    /*
                                    if (msreg) msreg->AddError("Ошибка формата потока. Лишняя закрывающая скобка }.",
                                         "Позиция", i,
                                         "Путь", path);
                                    */
                                    Console.WriteLine($"Ошибка формата потока. Лишняя закрывающая скобка. В позиции: { i }, Путь: {path}");
                                    ret = null;
                                    return null;
                                }
                                break;
                            default:
                                /*
                                if (msreg) msreg->AddError("Ошибка формата потока. Ошибочный символ в режиме ожидания разделителя.",
                                     "Символ", sym,
                                     "Код символа", tohex(sym),
                                     "Путь", path);
                                */
                                Console.WriteLine($"Ошибка формата потока. Ошибочный символ в режиме ожидания разделителя. Символ: { sym }, код символа: {sym} Путь: {path}");
                                ret = null;
                                return null;
                        }
                        break;
                    case _state.s_string:
                        if (sym == '"')
                        {
                            state = _state.s_quote_or_endstring;
                        }
                        else
                            __curvalue__.Append(sym);
                        break;
                    case _state.s_quote_or_endstring:
                        if (sym == '"')
                        {
                            __curvalue__.Append(sym);
                            state = _state.s_string;
                        }
                        else
                        {
                            t.add_child(__curvalue__.ToString(), node_type.nd_string);
                            switch (sym)
                            {
                                case ' ': // space
                                case '\t':
                                case '\r':
                                case '\n':
                                    state = _state.s_delimitier;
                                    break;
                                case ',':
                                    state = _state.s_value;
                                    break;
                                case '}':
                                    t = t.get_parent();
                                    if (t == null)
                                    {
                                        /*
                                        if (msreg) msreg->AddError("Ошибка формата потока. Лишняя закрывающая скобка }.",
                                             "Позиция", i,
                                             "Путь", path);
                                        */
                                        Console.WriteLine($"Ошибка формата потока. Лишняя закрывающая скобка. Позиция: { i }, путь: {path}");
                                        ret = null;
                                        return null;
                                    }
                                    state = _state.s_delimitier;
                                    break;
                                default:
                                    /*
                                    if (msreg) msreg->AddError("Ошибка формата потока. Ошибочный символ в режиме ожидания разделителя.",
                                         "Символ", sym,
                                         "Код символа", tohex(sym),
                                         "Путь", path);
                                    */
                                    Console.WriteLine($"Ошибка формата потока. Ошибочный символ в режиме ожидания разделителя. Символ: { sym }, путь: {path}");
                                    ret = null;
                                    return null;
                            }
                        }
                        break;
                    case _state.s_nonstring:
                        switch (sym)
                        {
                            case ',':
                                curvalue = __curvalue__.ToString();
                                nt = classification_value(curvalue);
                                if (nt == node_type.nd_unknown)
                                {
                                    /*
                                    if (msreg) msreg->AddError("Ошибка формата потока. Неизвестный тип значения.",
                                      "Значение", curvalue,
                                      "Путь", path);
                                      */
                                    Console.WriteLine($"Ошибка формата потока. Неизвестный тип значения. Значение: { curvalue }, путь: {path}");
                                }
                                t.add_child(curvalue, nt);
                                state = _state.s_value;
                                break;
                            case '}':
                                curvalue = __curvalue__.ToString();

                                nt = classification_value(curvalue);

                                if (nt == node_type.nd_unknown)
                                {
                                    //if (msreg) msreg->AddError("Ошибка формата потока. Неизвестный тип значения.", "Значение", curvalue, "Путь", path);
                                    Console.WriteLine($"Ошибка формата потока. Неизвестный тип значения. Значение: { curvalue }, путь: {path}");
                                }
                                t.add_child(curvalue, nt);
                                t = t.get_parent();
                                if (t == null)
                                {
                                    /*
                                    if (msreg) msreg->AddError("Ошибка формата потока. Лишняя закрывающая скобка }.",
                                         "Позиция", i,
                                         "Путь", path);
                                    */
                                    Console.WriteLine($"Ошибка формата потока. Лишняя закрывающая скобка. Позиция: { i }, путь: {path}");
                                    ret = null;
                                    return null;
                                }
                                state = _state.s_delimitier;
                                break;
                            default:
                                __curvalue__.Append(sym);
                                break;
                        }
                        break;
                    default:
                        /*
                        if (msreg) msreg->AddError("Ошибка формата потока. Неизвестный режим разбора.",
                             "Режим разбора", tohex(state),
                             "Путь", path);
                             */
                        Console.WriteLine($"Ошибка формата потока. Неизвестный режим разбора. Режим разбора: { state }, путь: {path}");
                        ret = null;
                        return null;
                }
            }


            if (state == _state.s_nonstring)
            {
                curvalue = __curvalue__.ToString();
                nt = classification_value(curvalue);
                if (nt == node_type.nd_unknown)
                { 
                    /*
                    if (msreg) msreg->AddError("Ошибка формата потока. Неизвестный тип значения.",
                      "Значение", curvalue,
                      "Путь", path);
                      */
                    Console.WriteLine($"Ошибка формата потока. Неизвестный тип значения. Значение: { curvalue }, путь: {path}");
                }
                t.add_child(curvalue, nt);
            }
            else
                if (state == _state.s_quote_or_endstring)
                t.add_child(__curvalue__.ToString(), node_type.nd_string);
            else
                if (state != _state.s_delimitier)
            {
                /*
                if (msreg) msreg->AddError("Ошибка формата потока. Незавершенное значение",
                     "Режим разбора", tohex(state),
                     "Путь", path);
                */
                Console.WriteLine($"Ошибка формата потока. Незавершенное значение. Режим разбора: { state }, путь: {path}");
                ret = null;
                return null;
            }

            if (t != ret)
            {
                /*
                if (msreg) msreg->AddError("Ошибка формата потока. Не хватает закрывающих скобок } в конце текста разбора.",
                     "Путь", path);
                    */
                Console.WriteLine($"Ошибка формата потока. Не хватает закрывающих скобок в конце текста разбора, путь: {path}");
                ret = null;
                return null;
            }

            return ret;

        } // End parse_1Ctext

        /// <summary>
        /// Парсинг скобочного дерева 1С через поток
        /// </summary>
        /// <param name="str"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public tree parse_1Cstream(Stream str, String path)
        {
            StringBuilder __curvalue__ = new StringBuilder("");

            String curvalue = "";
            tree ret = new tree("", node_type.nd_list, null);
            tree t = ret;

            int i = 0;
            char sym = '0';
            int _sym = 0;
            node_type nt = node_type.nd_unknown;

            StreamReader reader = new StreamReader(str, true);
            _state state = _state.s_nonstring;

            for (i = 1, _sym = reader.Read(); _sym >= 0; i++, _sym = reader.Read())
            {
                sym = (Char)_sym;

                if (String.IsNullOrEmpty(sym.ToString())) break;

                switch (state)
                {
                    case _state.s_value:
                        switch (sym)
                        {
                            case ' ': // space
                            case '\t':
                            case '\r':
                            case '\n':
                                break;
                            case '"':

                                __curvalue__.Clear();
                                state = _state.s_string;
                                break;

                            case '{':

                                t = new tree("", node_type.nd_list, t);
                                break;

                            case '}':

                                if (t.get_first() != null)
                                    t.add_child("", node_type.nd_empty);

                                t = t.get_parent();

                                if (t == null)
                                {
                                    //if (msreg) msreg->AddError("Ошибка формата потока. Лишняя закрывающая скобка }.", "Позиция", i, "Путь", path);
                                    //delete ret;
                                    ret = null;
                                    return null;
                                }
                                state = _state.s_delimitier;
                                break;

                            case ',':

                                t.add_child("", node_type.nd_empty);
                                break;

                            default:

                                __curvalue__.Clear();
                                __curvalue__.Append(sym);
                                state = _state.s_nonstring;

                                break;
                        }
                        break;
                    case _state.s_delimitier:
                        switch (sym)
                        {
                            case ' ': // space
                            case '\t':
                            case '\r':
                            case '\n':
                                break;
                            case ',':
                                state = _state.s_value;
                                break;
                            case '}':
                                t = t.get_parent();
                                if (t == null)
                                {
                                    /*
                                    if (msreg) msreg->AddError("Ошибка формата потока. Лишняя закрывающая скобка }.",
                                         "Позиция", i,
                                         "Путь", path);
                                    */
                                    ret = null;
                                    return null;
                                }
                                break;
                            default:
                                /*
                                if (msreg) msreg->AddError("Ошибка формата потока. Ошибочный символ в режиме ожидания разделителя.",
                                     "Символ", sym,
                                     "Код символа", tohex(sym),
                                     "Путь", path);
                                */
                                ret = null;
                                return null;
                        }
                        break;
                    case _state.s_string:
                        if (sym == '"')
                        {
                            state = _state.s_quote_or_endstring;
                        }
                        else
                            __curvalue__.Append(sym);
                        break;
                    case _state.s_quote_or_endstring:
                        if (sym == '"')
                        {
                            __curvalue__.Append(sym);
                            state = _state.s_string;
                        }
                        else
                        {
                            t.add_child(__curvalue__.ToString(), node_type.nd_string);
                            switch (sym)
                            {
                                case ' ': // space
                                case '\t':
                                case '\r':
                                case '\n':
                                    state = _state.s_delimitier;
                                    break;
                                case ',':
                                    state = _state.s_value;
                                    break;
                                case '}':
                                    t = t.get_parent();
                                    if (t == null)
                                    {
                                        /*
                                        if (msreg) msreg->AddError("Ошибка формата потока. Лишняя закрывающая скобка }.",
                                             "Позиция", i,
                                             "Путь", path);
                                        */

                                        ret = null;
                                        return null;
                                    }
                                    state = _state.s_delimitier;
                                    break;
                                default:
                                    /*
                                    if (msreg) msreg->AddError("Ошибка формата потока. Ошибочный символ в режиме ожидания разделителя.",
                                         "Символ", sym,
                                         "Код символа", tohex(sym),
                                         "Путь", path);
                                    */
                                    ret = null;
                                    return null;
                            }
                        }
                        break;
                    case _state.s_nonstring:
                        switch (sym)
                        {
                            case ',':
                                curvalue = __curvalue__.ToString();
                                nt = classification_value(curvalue);
                                if (nt == node_type.nd_unknown)
                                {
                                    /*
                                    if (msreg) msreg->AddError("Ошибка формата потока. Неизвестный тип значения.",
                                      "Значение", curvalue,
                                      "Путь", path);
                                      */
                                }
                                t.add_child(curvalue, nt);
                                state = _state.s_value;
                                break;
                            case '}':
                                curvalue = __curvalue__.ToString();

                                nt = classification_value(curvalue);

                                if (nt == node_type.nd_unknown)
                                {
                                    //if (msreg) msreg->AddError("Ошибка формата потока. Неизвестный тип значения.", "Значение", curvalue, "Путь", path);
                                }
                                t.add_child(curvalue, nt);
                                t = t.get_parent();
                                if (t == null)
                                {
                                    /*
                                    if (msreg) msreg->AddError("Ошибка формата потока. Лишняя закрывающая скобка }.",
                                         "Позиция", i,
                                         "Путь", path);
                                    */
                                    ret = null;
                                    return null;
                                }
                                state = _state.s_delimitier;
                                break;
                            default:
                                __curvalue__.Append(sym);
                                break;
                        }
                        break;
                    default:
                        /*
                        if (msreg) msreg->AddError("Ошибка формата потока. Неизвестный режим разбора.",
                             "Режим разбора", tohex(state),
                             "Путь", path);
                             */
                        ret = null;
                        return null;
                }
            }

            if (state == _state.s_nonstring)
            {
                curvalue = __curvalue__.ToString();
                nt = classification_value(curvalue);
                if (nt == node_type.nd_unknown)
                    /*
                    if (msreg) msreg->AddError("Ошибка формата потока. Неизвестный тип значения.",
                      "Значение", curvalue,
                      "Путь", path);
                      */
                    t.add_child(curvalue, nt);
            }
            else
                if (state == _state.s_quote_or_endstring)
                t.add_child(__curvalue__.ToString(), node_type.nd_string);
            else
                if (state != _state.s_delimitier)
            {
                /*
                if (msreg) msreg->AddError("Ошибка формата потока. Незавершенное значение",
                     "Режим разбора", tohex(state),
                     "Путь", path);
                */
                ret = null;
                return null;
            }

            if (t != ret)
            {
                /*
                if (msreg) msreg->AddError("Ошибка формата потока. Не хватает закрывающих скобок } в конце текста разбора.",
                     "Путь", path);
                    */
                ret = null;
                return null;
            }

            return ret;
        }

        /// <summary>
        /// Проверка формата потока
        /// </summary>
        /// <param name="str"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool test_parse_1Ctext(Stream str, String path)
        {

            int i = 0;
            int level = 0;

            StringBuilder __curvalue__ = new StringBuilder();
            String curvalue;
            char sym;
            int _sym;

            node_type nt = node_type.nd_unknown;

            bool ret = true;

            _state state = _state.s_nonstring;

            StreamReader reader = new StreamReader(str, true);

            for (i = 1, _sym = reader.Read(); _sym > 0; i++, _sym = reader.Read())
            {
                sym = (Char)_sym;

                switch (state)
                {
                    case _state.s_value:
                        switch (sym)
                        {
                            case ' ': // space
                            case '\t':
                            case '\r':
                            case '\n':
                                break;
                            case '"':
                                __curvalue__.Clear();
                                state = _state.s_string;
                                break;
                            case '{':
                                level++;
                                break;
                            case '}':
                                if (level <= 0)
                                {
                                    /*
                                    if (msreg) msreg->AddError("Ошибка формата потока. Лишняя закрывающая скобка }.",
                                         "Позиция", i,
                                         "Путь", path);
                                    */
                                    ret = false;
                                }
                                state = _state.s_delimitier;
                                level--;
                                break;
                            default:
                                __curvalue__.Clear();
                                __curvalue__.Append(sym);
                                state = _state.s_nonstring;
                                break;
                        }
                        break;
                    case _state.s_delimitier:
                        switch (sym)
                        {
                            case ' ': // space
                            case '\t':
                            case '\r':
                            case '\n':
                                break;
                            case ',':
                                state = _state.s_value;
                                break;
                            case '}':
                                if (level <= 0)
                                {
                                    /*
                                    if (msreg) msreg->AddError("Ошибка формата потока. Лишняя закрывающая скобка }.",
                                         "Позиция", i,
                                         "Путь", path);
                                    */
                                    ret = false;
                                }
                                level--;
                                break;
                            default:
                                /*
                                if (msreg) msreg->AddError("Ошибка формата потока. Ошибочный символ в режиме ожидания разделителя.",
                                     "Символ", sym,
                                     "Код символа", tohex(sym),
                                     "Путь", path);
                                */
                                reader.Dispose();
                                return ret;
                        }
                        break;
                    case _state.s_string:
                        if (sym == '"')
                        {
                            state = _state.s_quote_or_endstring;
                        }
                        else __curvalue__.Append(sym);
                        break;
                    case _state.s_quote_or_endstring:
                        if (sym == '"')
                        {
                            __curvalue__.Append(sym);
                            state = _state.s_string;
                        }
                        else
                        {
                            switch (sym)
                            {
                                case ' ': // space
                                case '\t':
                                case '\r':
                                case '\n':
                                    state = _state.s_delimitier;
                                    break;
                                case ',':
                                    state = _state.s_value;
                                    break;
                                case '}':
                                    if (level <= 0)
                                    {
                                        /*
                                        if (msreg) msreg->AddError("Ошибка формата потока. Лишняя закрывающая скобка }.",
                                         "Позиция", i,
                                         "Путь", path);
                                         */
                                        ret = false;
                                    }
                                    level--;
                                    state = _state.s_delimitier;
                                    break;
                                default:

                                    /*
                        if (msreg) msreg->AddError("Ошибка формата потока. Ошибочный символ в режиме ожидания разделителя.",
                             "Символ", sym,
                             "Код символа", tohex(sym),
                             "Путь", path);
                             */
                                    reader.Dispose();
                                    return ret;
                            }
                        }
                        break;

                    case _state.s_nonstring:
                        switch (sym)
                        {
                            case ',':
                                curvalue = __curvalue__.ToString();
                                nt = classification_value(curvalue);
                                if (nt == node_type.nd_unknown)
                                {
                                    /*
                        if (msreg) msreg->AddError("Ошибка формата потока. Неизвестный тип значения.",
                             "Значение", curvalue,
                             "Путь", path);
                             */
                                    ret = false;
                                }
                                state = _state.s_nonstring;
                                break;

                            case '}':
                                curvalue = __curvalue__.ToString();
                                nt = classification_value(curvalue);
                                if (nt == node_type.nd_unknown)
                                {
                                    /*
                        if (msreg) msreg->AddError("Ошибка формата потока. Неизвестный тип значения.",
                             "Значение", curvalue,
                             "Путь", path);
                             */
                                    ret = false;
                                }
                                if (level <= 0)
                                {
                                    /*
                        if (msreg) msreg->AddError("Ошибка формата потока. Лишняя закрывающая скобка }.",
                             "Позиция", i,
                             "Путь", path);
                             */
                                    ret = false;
                                }
                                level--;
                                state = _state.s_delimitier;
                                break;
                            default:
                                __curvalue__.Append(sym);
                                break;
                        }
                        break;
                    default:
                        /*
                if (msreg) msreg->AddError("Ошибка формата потока. Неизвестный режим разбора.",
                     "Режим разбора", tohex(state),
                     "Путь", path);
                     */
                        ret = false;
                        break;
                }
            }

            if (state == _state.s_nonstring)
            {
                curvalue = __curvalue__.ToString();
                nt = classification_value(curvalue);
                if (nt == node_type.nd_unknown)
                {
                    /*
            if(msreg) msreg->AddError("Ошибка формата потока. Неизвестный тип значения.",
                "Значение", curvalue,
                "Путь", path);
                */
                    ret = false;
                }
            }
            else if (state == _state.s_quote_or_endstring)
            {

            }
            else if (state != _state.s_delimitier)
            {
                /*
        if(msreg) msreg->AddError("Ошибка формата потока. Незавершенное значение",
            "Режим разбора", tohex(state),
            "Путь", path);
            */
                ret = false;
            }

            if (level > 0)
            {
                /*
        if(msreg) msreg->AddError("Ошибка формата потока. Не хватает закрывающих скобок } в конце текста разбора.",
            "Путь", path);
            */
                ret = false;
            }

            reader.Dispose();
            return ret;


        }

        public String outtext(tree t)
        {
            String text = "";

            if (t != null)
                if (t.get_first() != null)
                    t.get_first().outtext(ref text);

            return text;
        }

        public static node_type classification_value(String value)
        {

            if (String.IsNullOrEmpty(value)) return node_type.nd_empty;

            if (Regex.IsMatch(value, exp_number))     return node_type.nd_number;
            if (Regex.IsMatch(value, exp_number_exp)) return node_type.nd_number_exp;
            if (Regex.IsMatch(value, exp_guid))       return node_type.nd_guid;
            if (Regex.IsMatch(value, exp_binary))     return node_type.nd_binary;
            if (Regex.IsMatch(value, exp_link))       return node_type.nd_link;
            if (Regex.IsMatch(value, exp_binary2))    return node_type.nd_binary2;
            if (Regex.IsMatch(value, exp_binary_d))   return node_type.nd_binary_d;

            return node_type.nd_unknown;
        }



    }
}
