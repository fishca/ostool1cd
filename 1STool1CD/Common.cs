using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static _1STool1CD.Constants;

namespace _1STool1CD
{
    /// <summary>
    /// Класс содержит различные сервисные функции
    /// </summary>
    public static class Common
    {

        /// <summary>
        /// Преобразование времени из строки в DateTime
        /// </summary>
        /// <param name="ft"></param>
        /// <param name="time1CD"></param>
        public static void Time1CDtoFileTime(ref DateTime ft, String time1CD)
        {
            BinaryDecimalDate bdd = new BinaryDecimalDate(time1CD);
            ft = DateTime.Parse(time1CD);
        }

        /// <summary>
        /// Меняет порядок байт в числе
        /// 0x11223344 => 0x44332211
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static UInt32 ReverseByteOrder(UInt32 value)
        {

            var bytes = BitConverter.GetBytes(value);

            Array.Reverse(bytes);

            return (UInt32)BitConverter.ToInt32(bytes, 0);
            
        }

        /// <summary>
        /// Отображение GUID в стиле 1С
        /// </summary>
        /// <param name="str_guid"></param>
        /// <returns></returns>
        public static String GUIDas1c(String str_guid)
        {
            Guid g = new Guid();
            StringBuilder str = new StringBuilder(str_guid);



        	return "";
        }

        /// <summary>
        /// Отображение GUID в стиле Microsoft
        /// </summary>
        /// <param name="str_guid"></param>
        /// <returns></returns>
        public static String GUIDasMS(String str_guid)
        {
            StringBuilder str = new StringBuilder(str_guid);

            return "";
        }

        /// <summary>
        /// Преобразование GUID в строку
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public static String GUIDtoString(Guid guid)
        {
            return GUIDas1c(guid.ToString());
        }

        /// <summary>
        /// Преобразование строки в GUID
        /// </summary>
        /// <param name="str"></param>
        /// <param name="guid"></param>
        /// <returns></returns>
        public static bool StringToGUID(String str, Guid guid)
        {
            int i, j;

            String g = guid.ToString();
            Char hi, lo;
            Char tmp_char;

            //memset(guid, 0, sizeof(TGUID));

            bool res = true;
            if (str.Length != 36)
                res = false;
            else
            {
                j = 1;
                for (i = 12; i < 16; i++)
                {
                    hi = str[j++];
                    lo = str[j++];
                    tmp_char = g[i];
                    res = res && TwoHexDigitsToByte(hi, lo, ref tmp_char);
                }
                res = res && (str[j++] == '-');
                for (i = 10; i < 12; i++)
                {
                    hi = str[j++];
                    lo = str[j++];
                    tmp_char = g[i];
                    res = res && TwoHexDigitsToByte(hi, lo, ref tmp_char);
                }
                res = res && (str[j++] == '-');
                for (i = 8; i < 10; i++)
                {
                    hi = str[j++];
                    lo = str[j++];
                    tmp_char = g[i];
                    res = res && TwoHexDigitsToByte(hi, lo, ref tmp_char);
                }
                res = res && (str[j++] == '-');
                for (i = 0; i < 2; i++)
                {
                    hi = str[j++];
                    lo = str[j++];
                    tmp_char = g[i];
                    res = res && TwoHexDigitsToByte(hi, lo, ref tmp_char);
                }
                res = res && (str[j++] == '-');
                for (i = 2; i < 8; i++)
                {
                    hi = str[j++];
                    lo = str[j++];
                    tmp_char = g[i];
                    res = res && TwoHexDigitsToByte(hi, lo, ref tmp_char);
                }

            }

            return res;
        }

        /// <summary>
        /// Преобразование GUID в плоскую строку
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public static String GUIDtoStringFlat(Guid guid)
        {
            int i, j;

            Char[] buf = new Char[33];
            Char sym;
            String g = guid.ToString();

            j = 0;
            for (i = 0; i < 16; i++)
            {
                sym = (Char)('0' + (g[i] >> 4));
                if (sym > '9')
                    sym += (Char)('a' - '9' - 1);

                buf[j++] = sym;

                sym = (Char)('0' + (g[i] & 0xf));

                if (sym > '9')
                    sym += (Char)('a' - '9' - 1);

                buf[j++] = sym;
            }
            buf[j] = '0';

    	    return buf.ToString();
        }

        /// <summary>
        /// Преобзование из строки в GUID плоский
        /// </summary>
        /// <param name="str"></param>
        /// <param name="guid"></param>
        /// <returns></returns>
        public static bool StringToGUIDflat(String str, Guid guid)
        {
            int i, j;

            String g = guid.ToString();
            Char hi, lo;

            Char cur_char = '0';

            bool res = false;

            if (str.Length != 32)
            {
                res = false;
            }
            else
            {
                j = 1;
                for (i = 0; i < 16; i++)
                {
                    hi = str[j++];
                    lo = str[j++];
                    cur_char = g[i];
                    res = res || TwoHexDigitsToByte(hi, lo, ref cur_char);
                }
            }

            return res;
        }

        /// <summary>
        /// Преобразование двух шестнадцатеричных цифр в байт
        /// </summary>
        /// <param name="hi"></param>
        /// <param name="lo"></param>
        /// <param name="res"></param>
        /// <returns></returns>
        public static bool TwoHexDigitsToByte(Char hi, Char lo, ref Char res)
        {
            if (hi >= '0' && hi <= '9') res = (Char)((hi - '0') << 4);
        	else if (hi >= 'a' && hi <= 'f') res = (Char)((hi - ('a' - 0xa)) << 4);
	        else if (hi >= 'A' && hi <= 'F') res = (Char)((hi - ('A' - 0xa)) << 4);
	        else{
                res = '0';
                return false;
            }

            if (lo >= '0' && lo <= '9') res += (Char)(lo - '0');
	        else if (lo >= 'a' && lo <= 'f') res += (Char)(lo - ('a' - 0xa));
	        else if (lo >= 'A' && lo <= 'F') res += (Char)(lo - ('A' - 0xa));
	        else{
                res = '0';
                return false;
            }

            return true;
        }

        /// <summary>
        /// Преобразование строки 1С в дату
        /// </summary>
        /// <param name="str"></param>
        /// <param name="bytedate"></param>
        /// <returns></returns>
        public static bool String1cToDate(String str, DateTime bytedate)
        {
            BinaryDecimalDate bdd = new BinaryDecimalDate(str, "yyyyMMddhhmmss");
            //bdd.write_to(bytedate);
            return true;
        }
        
        /// <summary>
        /// Преобразование строки в дату
        /// </summary>
        /// <param name="str"></param>
        /// <param name="bytedate"></param>
        /// <returns></returns>
        public static bool StringToDate(String str, DateTime bytedate)
        {
            BinaryDecimalDate bdd = new BinaryDecimalDate(str);
            //bdd.write_to(bytedate);
            return true;
        }

        /// <summary>
        /// Преобразование даты в строку 1С
        /// </summary>
        /// <param name="bytedate"></param>
        /// <returns></returns>
        public static String DateToString1c(DateTime bytedate)
        {
            BinaryDecimalDate bdd = new BinaryDecimalDate(bytedate.ToString());
            return bdd.get_part(0, 14);
        }

        /// <summary>
        /// Преобразование даты в строку
        /// </summary>
        /// <param name="bytedate"></param>
        /// <returns></returns>
        public static String DateToString(DateTime bytedate)
        {

            BinaryDecimalDate bdd = new BinaryDecimalDate(bytedate.ToString());
            return bdd.get_presentation();
            
        }

        /// <summary>
        /// Преобразование буфера в шестнадцатеричную строку
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public static String HexString(Char[] buf, int n)
        {
            int i;
            String s = "";
            Char b;
            Char c;

            for (i = 0; i < n; i++)
            {
                c = buf[i];
		        c >>= 4;
		        b = hexdecode[c];
		        s += b;
		        c = buf[i];
                c = (Char)(c & 0xf);
		        b = hexdecode[c];
		        s += b;
		        if( i < n - 1) s += " ";
        	}

        	return s;
        }

        /// <summary>
        /// Преобразование из потока в шестнадцатеричную строку
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static String HexString(Stream str)
        {
            String s = "";
            Char b;
            Char c;
            Byte[] d = new Byte[1];

            while (str.Read(d, 0, 1) != 0)
            {
                
                c = (Char)(BitConverter.ToInt32(d, 0) >> 4);
                b = hexdecode[c];
                s += b;
                c = (Char)(BitConverter.ToInt32(d, 0) & 0xf);
                b = hexdecode[c];
                s += b;
            }

            return s;
            
        }

        /// <summary>
        /// Преобразование строки с заменой недопустимых символов
        /// </summary>
        /// <param name="in_str"></param>
        /// <returns></returns>
        public static String ToXML(String in_str)
        {
            StringBuilder tmp_str = new StringBuilder(in_str);

            return tmp_str.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("'", "&apos;").Replace("\"", "&quot;").ToString();
        }

        /// <summary>
        /// Преобразование символа из шестнадцатеричной цифры
        /// </summary>
        /// <param name="digit"></param>
        /// <returns></returns>
        public static Char FromHexDigit(Char digit)
        {
            if (digit >= '0' && digit <= '9') return (Char)(digit - '0');
            if (digit >= 'a' && digit <= 'f') return (Char)(digit - 'a' + 10);
            if (digit >= 'A' && digit <= 'F') return (Char)(digit - 'A' + 10);
            return '0';
        }


    }
}
