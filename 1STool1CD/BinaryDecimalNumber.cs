using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _1STool1CD
{
    public class BinaryDecimalNumber
    {
        public BinaryDecimalNumber() { }
        public BinaryDecimalNumber(byte[] raw_data, int length, int precision, bool has_sign_flag) { }
        public BinaryDecimalNumber(String presentation, bool has_sign = false, int length = 0, int precision = 0) { }

        public void write_to(byte[] raw_data) { }

        public virtual String get_presentation() { return " "; }
        public String get_part(int startIndex, int count) { return " "; }

        public List<int> get_int() { return null; }
        public List<int> get_frac() { return null; }


        public bool has_sing_flag = false;
        public int precision = 0;
        public List<int> data;
        public int sign;

    }

    public class BinaryDecimalDate : BinaryDecimalNumber {

        public BinaryDecimalDate(byte[] raw_data) { }
        public BinaryDecimalDate(String presentation, String format = "dd.MM.yyyy hh:mm:ss") { }

        public override String get_presentation() { return " "; }

        public int get_year() { return 0; }
        public int get_month() { return 0; }
        public int get_day() { return 0; }

        public int get_hour() { return 0; }
        public int get_minute() { return 0; }
        public int get_second() { return 0; }
    }
}
