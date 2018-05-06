using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _1STool1CD
{
    public class BinaryDecimalBuilder
    {
        public List<int> data;

        public BinaryDecimalBuilder(List<int> data)
        { 
            this.data = data;
        }

        public void push1(byte lo)
        {
            data.Add(lo);
        }

        public void push2(byte hilo)
        {
            push1((byte)(hilo >> 4));
            push1((byte)(hilo & 0x0f));
        }

    }

    public class BinaryDecimalNumber
    {
        public BinaryDecimalNumber()
        {
        }

        public BinaryDecimalNumber(byte raw_data, int length, int precision, bool has_sign_flag)
        {
            this.has_sing_flag = has_sign_flag;
            this.precision = precision;
            this.sign = 1;

            this.data.Clear();

            BinaryDecimalBuilder builder = new BinaryDecimalBuilder(data);

            byte byte_data = raw_data;
            byte first_byte = byte_data;

            if (has_sign_flag)
            {
                if (first_byte >> 4 != 0)
                {
                    sign = 1;
                }
                else
                {
                    sign = -1;
                }
                builder.push1((byte)(first_byte & 0x0f));
            }
            else
            {
                builder.push2(first_byte);
            }
            ++byte_data;
            int i = 1;
            while (i < length)
            {
                if (i + 1 < length)
                {
                    builder.push2(byte_data);
                    i += 2;
                }
                else
                {
                    builder.push1((byte)(byte_data >> 4));
                    i++;
                }
                byte_data++;
            }


        }

        public BinaryDecimalNumber(String presentation, bool has_sign = false, int length = 0, int precision = 0)
        {
            this.has_sing_flag = has_sign;
            this.precision = precision;
            this.sign = 1;
            int INT_PART = 0;
            int FRAC_PART = 1;

            List<int>[] parts = new List<int>[2];
           
            int part_no = 0;

            foreach (Char c in presentation)
            {
                if (c == '.')
                {
                    part_no++;
                }
                else
                {
                    parts[part_no].Add(c - '0');
                }
            }
            if (precision != 0)
            {
                for (int i = 0; i < precision; i++)
                    parts[FRAC_PART].Add(0);
            }
            int add_length = 0;

            if (length != 0)
            {
                add_length = length - parts[INT_PART].Count - parts[FRAC_PART].Count;
                if (add_length < 0)
                {
                    parts[INT_PART].Clear();
                    for(int i = 0; i < (length - precision); i++)
                    parts[INT_PART].Add(9); // забиваем девяточками при превышении размера
                    add_length = 0;
                }
            }
            while (add_length-- != 0)
            {
                data.Add(0);
            }

            foreach (var part in parts)
            {
                foreach (var num in part)
                {
                    data.Add(num);
                }
            }



        }

        public void write_to(byte raw_data)
        {
            byte byte_data = raw_data;

            /*
            var it = data.begin();

            if (has_sing_flag)
            {
                *byte_data = (sign == -1 ? 0 : 0x10) | *it;
                byte_data++;
                it++;
            }
            while (it != data.end())
            {
                uint8_t ddMMyyyy_presentation = *it << 4;
                if (++it != data.end())
                {
                    ddMMyyyy_presentation |= *it;
                    it++;
                }
                *byte_data = ddMMyyyy_presentation;
                byte_data++;
            }
            */

        }

        public virtual String get_presentation()
        {
            String result = "";
            if (has_sing_flag)
            {
                if (sign == -1)
                {
                    result += "-";
                }
            }
            int int_size = data.Count - precision;
            {
                int i = 0;
                while (i < int_size && data[i] == 0)
                {
                    i++;
                }
                if (i < int_size)
                {
                    while (i < int_size)
                    {
                        result += '0' + data[i];
                        i++;
                    }
                }
                else
                {
                    result += '0';
                }
            }
            if (precision != 0)
            {
                String frac = ".";
                bool has_significant_digits = false;
                int max_significant_size = data.Count;
                while (max_significant_size > int_size)
                {
                    if (data[max_significant_size - 1] == 0)
                    {
                        max_significant_size--;
                    }
                    else
                    {
                        break;
                    }
                }
                for (int i = int_size; i < max_significant_size; i++)
                {
                    if (data[i] != 0)
                    {
                        has_significant_digits = true;
                    }
                    frac += '0' + data[i];
                }
                if (has_significant_digits)
                {
                    result += frac;
                }
            }
            return result;
        }

        public String get_part(int startIndex, int count)
        {
            String result = "";
            for (int i = startIndex; i < data.Count && count != 0; count--, i++)
            {
                result += (data[i] + '0');
            }
            return result;
        }

        public List<int> get_int()
        {
            List<int> result;

            result = data.GetRange(0, data.Count - precision);

            return result;
            
        }

        public List<int> get_frac()
        {
            List<int> result;

            result = data.GetRange(data.Count - precision, data.Count);
            
            return result;
        }


        public bool has_sing_flag = false;
        public int precision = 0;
        public List<int> data;
        public int sign;

    }

    public class BinaryDecimalDate : BinaryDecimalNumber {

        public BinaryDecimalDate(byte raw_data) : base(raw_data, 19, 0, false)
        {
        }

        public BinaryDecimalDate(String presentation, String format = "dd.MM.yyyy hh:mm:ss")
        {
            SortedDictionary<Char, List<int>> indexes = new SortedDictionary<char, List<int>>();

            for (int i = 0; i < format.Length; i++)
            {
                indexes.Add(format[i], new List<int>());
            }

            for (int i = 0; i < format.Length; i++)
            {
                indexes[format[i]].Add(i);
            }

            foreach (var part in "yMdhms")
            {
                foreach (var i in indexes[part])
                {
                    if (i < presentation.Length)
                    {
                        data.Add(presentation[i] - '0');
                    }
                    else
                    {
                        data.Add(0);
                    }
                }
            }

        }

        public override String get_presentation()
        {
            String result = "";
            result += get_part(6, 2);
            result += ".";
            result += get_part(4, 2);
            result += ".";
            result += get_part(0, 4);
            result += " ";
            result += get_part(8, 2);
            result += ":";
            result += get_part(10, 2);
            result += ":";
            result += get_part(12, 2);
            return result;
        }

        public int get_year()
        {
            return data[0] * 1000 + data[1] * 100 + data[2] * 10 + data[3]; 
        }

        public int get_month()
        {
            return data[4] * 10 + data[5];
        }

        public int get_day()
        {
            return data[6] * 10 + data[7];
        }

        public int get_hour()
        {
            return data[8] * 10 + data[9];
        }

        public int get_minute()
        {
            return data[10] * 10 + data[11];
        }

        public int get_second()
        {
            return data[12] * 10 + data[13];
        }
    }
}
