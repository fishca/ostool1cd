using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _1STool1CD
{
    public class APIcfBase
    {
        public static string str_cfu = ".cfu";
        public static string str_cfe = ".cfe";
        public static string str_cf = ".cf";
        public static string str_epf = ".epf";
        public static string str_erf = ".erf";
        public static string str_backslash = "\\";

        // шаблон заголовка блока
        public static string _BLOCK_HEADER_TEMPLATE  = "\r\n00000000 00000000 00000000 \r\n";
        public static string _EMPTY_CATALOG_TEMPLATE = "FFFFFF7F020000000000";

        public static Int32 LAST_BLOCK = 0x7FFFFFFF;

        public static UInt32 BLOCK_HEADER_LEN   = 32U;
        public static UInt32 CATALOG_HEADER_LEN = 16U;

        public static Int64 EPOCH_START_WIN = 504911232000000;

        public struct V8header_struct
        {
            public Int64 time_create;
            public Int64 time_modify;
            public Int64 zero;
        }

        public struct Catalog_header
        {
            public Int32 start_empty; // начало первого пустого блока
            public Int32 page_size;   // размер страницы по умолчанию
            public Int32 version;     // версия
            public Int32 zero;        // всегда ноль?
        }

        public enum Block_header : int
        {
            doc_len   = 2,
	        block_len = 11,
	        nextblock = 20
        }

        /// <summary>
        /// установка текущего времени
        /// </summary>
        /// <param name="v8t"></param>
        public static void setCurrentTime(Int64 v8t)
        {
            //SYSTEMTIME st;
            //FILETIME ft;

            //GetSystemTime(&st);
            //SystemTimeToFileTime(&st, &ft);
            //FileTimeToV8time(&ft, v8t);
            v8t = 100500;
        }


    } // окончание класса APIcfBase     
}
