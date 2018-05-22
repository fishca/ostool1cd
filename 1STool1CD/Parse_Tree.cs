using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using static _1STool1CD.Structures;

namespace _1STool1CD
{
    class Parse_Tree
    {
    }


    public class Tree
    {
        public static readonly String exp_number     = "^-?\\d+$"; // exp_number
        public static readonly String exp_number_exp = "^-?\\d+(\\.?\\d*)?((e|E)-?\\d+)?$";
        public static readonly String exp_guid       = "^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$";
        public static readonly String exp_binary     = "^#base64:[0-9a-zA-Z\\+=\\r\\n\\/]*$";
        public static readonly String exp_binary2    = "^[0-9a-zA-Z\\+=\\r\\n\\/]+$";
        public static readonly String exp_link       = "^[0-9]+:[0-9a-fA-F]{32}$";
        public static readonly String exp_binary_d   = "^#data:[0-9a-zA-Z\\+=\\r\\n\\/]*$";

        private String value;
        private Node_type type;
        private int num_subnode; // количество подчиненных
        private Tree parent;     // +1
        private Tree next;       // 0
        private Tree prev;       // 0
        private Tree first;      // -1
        private Tree last;       // -1
        private uint index;

        public string Value { get { return value; } set { this.value = value; } }

        public Node_type Type { get { return type; } set { type = value; } }

        public int Num_subnode { get { return num_subnode; } set { num_subnode = value; } }

        public Tree Parent { get { return parent; } set { parent = value; } }

        public Tree Next { get { return next; } set { next = value; } }
        public Tree Prev { get { return prev; } set { prev = value; } }
        public Tree First { get { return first; } set { first = value; } }
        public Tree Last { get { return last; } set { last = value; } }
        public uint Index { get { return index; } set { index = value; } }

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="_value"></param>
        /// <param name="_type"></param>
        /// <param name="_parent"></param>
        public Tree(String _value, Node_type _type, Tree _parent)
        {
            Value  = _value;
            Type   = _type;
            Parent = _parent;

            Num_subnode = 0;
            Index = 0;

            if (Parent != null)
            {
                Parent.Num_subnode++;
                Prev = Parent.Last;

                if (Prev != null)
                {
                    Prev.Next = this;
                    Index = Prev.Index + 1;
                }
                else
                    Parent.First = this;

                Parent.Last = this;
            }
            else
                Prev = null;

            Next  = null;
            First = null;
            Last  = null;
        }

        public Tree Add_child(String _value, Node_type _type)
        {
            return new Tree(_value, _type, this);
        }

        public Tree Add_child()
        {
            return new Tree("", Node_type.nd_empty, this);
        }

        public Tree Add_node()
        {
            return new Tree("", Node_type.nd_empty, this.Parent);
        }

        public String Get_value()
        {
            return Value;
        }

        public Node_type Get_type()
        {
            return Type;
        }

        public int Get_num_subnode()
        {
            return Num_subnode;
        }

        public Tree Get_subnode(int _index)
        {
            if (_index >= Num_subnode)
                return null;

            Tree t = First;

            while (_index != 0)
            {
                t = t.Next;
                --_index;
            }
            return t;
        }

        public Tree Get_subnode(String node_name)
        {
            Tree t = First;
            while (t != null)
            {
                if (t.Value == node_name)
                    return t;

                t = t.Next;
            }
            return null;
        }

        public Tree Get_next()
        {
            return Next;
        }

        public Tree Get_parent()
        {
            return Parent;
        }

        public Tree Get_first()
        {
            return First;
        }

        public Tree Get_last()
        {
            return Last;
        }


        //public tree operator [] (int _index);


        public void Set_value(String v, Node_type t)
        {
            Value = v;
            Type = t;
        }

        public void Outtext(ref String text)
        {
            Node_type lt = Node_type.nd_unknown;

            if (Num_subnode != 0)
            {
                if (text.Length != 0)
                    text += "\r\n";

                text += "{";
                Tree t = First;
                while (t != null)
                {
                    t.Outtext(ref text);
                    lt = t.Type;
                    t = t.Next;
                    if (t != null)
                        text += ",";
                }
                if (lt == Node_type.nd_list)
                    text += "\r\n";
                text += "}";
            }
            else
            {
                switch (Type)
                {
                    case Node_type.nd_string:
                        text += "\"";
                        text += text.Replace("\"", "\"\"");
                        text += "\"";
                        break;
                    case Node_type.nd_number:
                    case Node_type.nd_number_exp:
                    case Node_type.nd_guid:
                    case Node_type.nd_list:
                    case Node_type.nd_binary:
                    case Node_type.nd_binary2:
                    case Node_type.nd_link:
                    case Node_type.nd_binary_d:
                        text += Value;
                        break;
                    default:
                        break;
                }
            }

        }

        public String Path()
        {
            String p = "";
            Tree t;

            //if (this == null)
            //    return ":??"; //-V704

            for (t = this; t.Parent != null; t = t.Parent)
            {
                //p = String(":") + t->index + p;
                p = ":" + t.Index + p;
            }
            return p;
        }

        /// <summary>
        /// Парсинг скобочного дерева 1С
        /// </summary>
        /// <param name="text"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static Tree Parse_1Ctext(String text, String path)
        {

            StringBuilder __curvalue__ = new StringBuilder("");

            String curvalue = "";
            Tree ret = new Tree("", Node_type.nd_list, null);
            Tree t = ret;
            int len = text.Length;
            int i = 0;
            char sym = '0';
            Node_type nt = Node_type.nd_unknown;

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

                                t = new Tree("", Node_type.nd_list, t);
                                break;

                            case '}':

                                if (t.Get_first() != null)
                                    t.Add_child("", Node_type.nd_empty);

                                t = t.Get_parent();

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

                                t.Add_child("", Node_type.nd_empty);
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
                                t = t.Get_parent();
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
                            t.Add_child(__curvalue__.ToString(), Node_type.nd_string);
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
                                    t = t.Get_parent();
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
                                nt = Classification_value(curvalue);
                                if (nt == Node_type.nd_unknown)
                                {
                                    /*
                                    if (msreg) msreg->AddError("Ошибка формата потока. Неизвестный тип значения.",
                                      "Значение", curvalue,
                                      "Путь", path);
                                      */
                                    Console.WriteLine($"Ошибка формата потока. Неизвестный тип значения. Значение: { curvalue }, путь: {path}");
                                }
                                t.Add_child(curvalue, nt);
                                state = _state.s_value;
                                break;
                            case '}':
                                curvalue = __curvalue__.ToString();

                                nt = Classification_value(curvalue);

                                if (nt == Node_type.nd_unknown)
                                {
                                    //if (msreg) msreg->AddError("Ошибка формата потока. Неизвестный тип значения.", "Значение", curvalue, "Путь", path);
                                    Console.WriteLine($"Ошибка формата потока. Неизвестный тип значения. Значение: { curvalue }, путь: {path}");
                                }
                                t.Add_child(curvalue, nt);
                                t = t.Get_parent();
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
                nt = Classification_value(curvalue);
                if (nt == Node_type.nd_unknown)
                { 
                    /*
                    if (msreg) msreg->AddError("Ошибка формата потока. Неизвестный тип значения.",
                      "Значение", curvalue,
                      "Путь", path);
                      */
                    Console.WriteLine($"Ошибка формата потока. Неизвестный тип значения. Значение: { curvalue }, путь: {path}");
                }
                t.Add_child(curvalue, nt);
            }
            else
                if (state == _state.s_quote_or_endstring)
                t.Add_child(__curvalue__.ToString(), Node_type.nd_string);
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
        public Tree Parse_1Cstream(Stream str, String path)
        {
            StringBuilder __curvalue__ = new StringBuilder("");

            String curvalue = "";
            Tree ret = new Tree("", Node_type.nd_list, null);
            Tree t = ret;

            int i = 0;
            char sym = '0';
            int _sym = 0;
            Node_type nt = Node_type.nd_unknown;

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

                                t = new Tree("", Node_type.nd_list, t);
                                break;

                            case '}':

                                if (t.Get_first() != null)
                                    t.Add_child("", Node_type.nd_empty);

                                t = t.Get_parent();

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

                                t.Add_child("", Node_type.nd_empty);
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
                                t = t.Get_parent();
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
                            t.Add_child(__curvalue__.ToString(), Node_type.nd_string);
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
                                    t = t.Get_parent();
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
                                nt = Classification_value(curvalue);
                                if (nt == Node_type.nd_unknown)
                                {
                                    /*
                                    if (msreg) msreg->AddError("Ошибка формата потока. Неизвестный тип значения.",
                                      "Значение", curvalue,
                                      "Путь", path);
                                      */
                                }
                                t.Add_child(curvalue, nt);
                                state = _state.s_value;
                                break;
                            case '}':
                                curvalue = __curvalue__.ToString();

                                nt = Classification_value(curvalue);

                                if (nt == Node_type.nd_unknown)
                                {
                                    //if (msreg) msreg->AddError("Ошибка формата потока. Неизвестный тип значения.", "Значение", curvalue, "Путь", path);
                                }
                                t.Add_child(curvalue, nt);
                                t = t.Get_parent();
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
                nt = Classification_value(curvalue);
                if (nt == Node_type.nd_unknown)
                    /*
                    if (msreg) msreg->AddError("Ошибка формата потока. Неизвестный тип значения.",
                      "Значение", curvalue,
                      "Путь", path);
                      */
                    t.Add_child(curvalue, nt);
            }
            else
                if (state == _state.s_quote_or_endstring)
                t.Add_child(__curvalue__.ToString(), Node_type.nd_string);
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
        public static bool Test_parse_1Ctext(Stream str, String path)
        {

            int i = 0;
            int level = 0;

            StringBuilder __curvalue__ = new StringBuilder();
            String curvalue;
            char sym;
            int _sym;

            Node_type nt = Node_type.nd_unknown;

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
                                nt = Classification_value(curvalue);
                                if (nt == Node_type.nd_unknown)
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
                                nt = Classification_value(curvalue);
                                if (nt == Node_type.nd_unknown)
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
                nt = Classification_value(curvalue);
                if (nt == Node_type.nd_unknown)
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

        public String Outtext(Tree t)
        {
            String text = "";

            if (t != null)
                if (t.Get_first() != null)
                    t.Get_first().Outtext(ref text);

            return text;
        }

        public static Node_type Classification_value(String value)
        {

            if (String.IsNullOrEmpty(value)) return Node_type.nd_empty;

            if (Regex.IsMatch(value, exp_number))     return Node_type.nd_number;
            if (Regex.IsMatch(value, exp_number_exp)) return Node_type.nd_number_exp;
            if (Regex.IsMatch(value, exp_guid))       return Node_type.nd_guid;
            if (Regex.IsMatch(value, exp_binary))     return Node_type.nd_binary;
            if (Regex.IsMatch(value, exp_link))       return Node_type.nd_link;
            if (Regex.IsMatch(value, exp_binary2))    return Node_type.nd_binary2;
            if (Regex.IsMatch(value, exp_binary_d))   return Node_type.nd_binary_d;

            return Node_type.nd_unknown;
        }



    }
}
