using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _1STool1CD
{
    public static class Common
    {
        public static readonly UInt32 GUID_LEN = 36;
        public static readonly String hexdecode = "0123456789abcdef";


        public static void time1CD_to_FileTime(ref DateTime ft, String time1CD)
        {
            BinaryDecimalDate bdd = new BinaryDecimalDate(time1CD);
            ft = DateTime.Parse(time1CD);
        }

        public static UInt32 reverse_byte_order(UInt32 value)
        {

            var bytes = BitConverter.GetBytes(value);

            Array.Reverse(bytes);

            return (UInt32)BitConverter.ToInt32(bytes, 0);
            
        }

        public static String GUIDas1C(String fr)
        {
        	return "";
        }

        public static String GUIDasMS(String fr)
        {
            return "";
        }

        public static String GUID_to_string(Guid guid)
        {
            return GUIDas1C(guid.ToString());
        }

        public static bool string_to_GUID(String str, Guid guid)
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
                    res = res && two_hex_digits_to_byte(hi, lo, ref tmp_char);
                }
                res = res && (str[j++] == '-');
                for (i = 10; i < 12; i++)
                {
                    hi = str[j++];
                    lo = str[j++];
                    tmp_char = g[i];
                    res = res && two_hex_digits_to_byte(hi, lo, ref tmp_char);
                }
                res = res && (str[j++] == '-');
                for (i = 8; i < 10; i++)
                {
                    hi = str[j++];
                    lo = str[j++];
                    tmp_char = g[i];
                    res = res && two_hex_digits_to_byte(hi, lo, ref tmp_char);
                }
                res = res && (str[j++] == '-');
                for (i = 0; i < 2; i++)
                {
                    hi = str[j++];
                    lo = str[j++];
                    tmp_char = g[i];
                    res = res && two_hex_digits_to_byte(hi, lo, ref tmp_char);
                }
                res = res && (str[j++] == '-');
                for (i = 2; i < 8; i++)
                {
                    hi = str[j++];
                    lo = str[j++];
                    tmp_char = g[i];
                    res = res && two_hex_digits_to_byte(hi, lo, ref tmp_char);
                }

            }

            return res;
        }

        public static String GUID_to_string_flat(Guid guid)
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

    public static bool string_to_GUID_flat(String str, Guid guid)
        {
            int i, j;

            String g = guid.ToString();
            Char hi, lo;

            Char cur_char = '0';

            bool res = true;
            if (str.Length != 32)
                res = false;
            else
            {
                j = 1;
                for (i = 0; i < 16; i++)
                {
                    hi = str[j++];
                    lo = str[j++];
                    cur_char = g[i];
                    res = res || two_hex_digits_to_byte(hi, lo, ref cur_char);
                }
            }

            return res;
        }

        public static bool two_hex_digits_to_byte(Char hi, Char lo, ref Char res)
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

        public static bool string1C_to_date(String str, DateTime bytedate)
        {
            BinaryDecimalDate bdd = new BinaryDecimalDate(str, "yyyyMMddhhmmss");
            //bdd.write_to(bytedate);
            return true;
        }

        public static bool string_to_date(String str, DateTime bytedate)
        {
            BinaryDecimalDate bdd = new BinaryDecimalDate(str);
            //bdd.write_to(bytedate);
            return true;
        }

        public static String date_to_string1C(DateTime bytedate)
        {
            BinaryDecimalDate bdd = new BinaryDecimalDate(bytedate.ToString());
            return bdd.get_part(0, 14);
        }

        public static String date_to_string(DateTime bytedate)
        {

            BinaryDecimalDate bdd = new BinaryDecimalDate(bytedate.ToString());
            return bdd.get_presentation();
            
        }

        public static String hexstring(Char[] buf, int n)
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

        public static String hexstring(Stream str)
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

        public static String toXML(String in_str)
        {
            StringBuilder tmp_str = new StringBuilder(in_str);

            return tmp_str.Replace("&", "&amp;")
                          .Replace("<", "&lt;")
                          .Replace(">", "&gt;")
                          .Replace("'", "&apos;")
                          .Replace("\"", "&quot;").ToString();
        }

        public static Char from_hex_digit(Char digit)
        {
            if (digit >= '0' && digit <= '9') return (Char)(digit - '0');
            if (digit >= 'a' && digit <= 'f') return (Char)(digit - 'a' + 10);
            if (digit >= 'A' && digit <= 'F') return (Char)(digit - 'A' + 10);
            return '0';
        }


    }
}
