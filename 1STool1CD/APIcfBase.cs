using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _1STool1CD
{
    class APIcfBase
    {
        const string str_cfu = ".cfu";
        const string str_cfe = ".cfe";
        const string str_cf = ".cf";
        const string str_epf = ".epf";
        const string str_erf = ".erf";
        const string str_backslash = "\\";

        // шаблон заголовка блока
        const string _BLOCK_HEADER_TEMPLATE  = "\r\n00000000 00000000 00000000 \r\n";
        const string _EMPTY_CATALOG_TEMPLATE = "FFFFFF7F020000000000";

        const Int32 LAST_BLOCK = 0x7FFFFFFF;

        const UInt32 BLOCK_HEADER_LEN   = 32U;
        const UInt32 CATALOG_HEADER_LEN = 16U;

        const Int64 EPOCH_START_WIN = 504911232000000;

        public struct V8header_struct
        {
            Int64 time_create;
            Int64 time_modify;
            Int64 zero;
        };

        public struct Catalog_header
        {
            Int32 start_empty; // начало первого пустого блока
            Int32 page_size;   // размер страницы по умолчанию
            Int32 version;     // версия
            Int32 zero;        // всегда ноль?
        };

        enum Block_header : int
        {
            doc_len   = 2,
	        block_len = 11,
	        nextblock = 20
        };






    } // окончание класса APIcfBase     
}
