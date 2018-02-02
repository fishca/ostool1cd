using System;
using System.IO;
using System.Collections;
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
        public static Int32 BLOCK_HEADER_LEN2 = 32;
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

        public enum block_header : int
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

        /// <summary>
        /// Читает блок из потока каталога stream_from, собирая его по страницам
        /// </summary>
        /// <param name="stream_from"></param>
        /// <param name="start"></param>
        /// <param name="stream_to"></param>
        /// <returns></returns>
        Stream read_block(Stream stream_from, int start, Stream stream_to = null)
        {
            //std::array<char, BLOCK_HEADER_LEN> temp_buf;
            //Char[] temp_buf = new Char[BLOCK_HEADER_LEN];

            //ArrayList temp_buf = new ArrayList(BLOCK_HEADER_LEN2);

            Byte[] temp_buf = new Byte[BLOCK_HEADER_LEN];

            int len, curlen, pos, readlen;

            if (stream_to != null)
                stream_to = new MemoryStream();

            stream_to.Seek(0, SeekOrigin.Begin);
            stream_to.SetLength(0);

            if (start < 0 || start == LAST_BLOCK || start > stream_from.Length)
                return stream_to;

            stream_from.Seek(start, SeekOrigin.Begin);
            stream_from.Read(temp_buf, 0, temp_buf.Length - 1);

            String hex_len = "0x";

            std::copy_n(temp_buf.begin() + (int)block_header::doc_len, HEX_INT_LEN, std::back_inserter(hex_len));
            len = std::stoi(hex_len, nullptr, 16);

            if (!len)
                return stream_to;

            std::string hex_curlen("0x");
            std::copy_n(temp_buf.begin() + (int)block_header::block_len, HEX_INT_LEN, std::back_inserter(hex_curlen));
            curlen = std::stoi(hex_curlen, nullptr, 16);

            std::string hex_start("0x");
            std::copy_n(temp_buf.begin() + (int)block_header::nextblock, HEX_INT_LEN, std::back_inserter(hex_start));
            start = std::stoi(hex_start, nullptr, 16);

            readlen = std::min(len, curlen);
            stream_to->CopyFrom(stream_from, readlen);

            pos = readlen;

            while (start != LAST_BLOCK)
            {

                stream_from->Seek(start, soFromBeginning);
                stream_from->Read(temp_buf.data(), temp_buf.size() - 1);

                std::string hex_curlen("0x");
                std::copy_n(temp_buf.begin() + (int)block_header::block_len, HEX_INT_LEN, std::back_inserter(hex_curlen));
                curlen = std::stoi(hex_curlen, nullptr, 16);

                std::string hex_start("0x");
                std::copy_n(temp_buf.begin() + (int)block_header::nextblock, HEX_INT_LEN, std::back_inserter(hex_start));
                start = std::stoi(hex_start, nullptr, 16);

                readlen = std::min(len - pos, curlen);
                stream_to->CopyFrom(stream_from, readlen);
                pos += readlen;

            }

            return stream_to;

        }


    } // окончание класса APIcfBase     
}
