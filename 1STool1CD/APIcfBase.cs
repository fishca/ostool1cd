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
        #region Константы

        public static readonly string str_cfu = ".cfu";
        public static readonly string str_cfe = ".cfe";
        public static readonly string str_cf = ".cf";
        public static readonly string str_epf = ".epf";
        public static readonly string str_erf = ".erf";
        public static readonly string str_backslash = "\\";

        // шаблон заголовка блока
        public static readonly string _BLOCK_HEADER_TEMPLATE  = "\r\n00000000 00000000 00000000 \r\n";
        public static readonly string _EMPTY_CATALOG_TEMPLATE = "FFFFFF7F020000000000";

        public static readonly Int32  LAST_BLOCK  = 0x7FFFFFFF;
        public static readonly UInt32 LAST_BLOCK2 = 0x7FFFFFFF;
        public static readonly UInt32 BLOCK_HEADER_LEN    = 32U;
        public static readonly Int32  BLOCK_HEADER_LEN2   = 32;
        public static readonly UInt32 CATALOG_HEADER_LEN  = 16U;
        public static readonly Int32  CATALOG_HEADER_LEN2 = 16;

        public static readonly Int64 EPOCH_START_WIN = 504911232000000;
        public static readonly Int32 HEX_INT_LEN = 2 * 2;

        #endregion

        #region Struct
        public struct V8header_struct
        {
            private Int64 time_create;
            private Int64 time_modify;
            private Int64 zero;

            public long Time_create { get => time_create; set => time_create = value; }
            public long Time_modify { get => time_modify; set => time_modify = value; }
            public long Zero { get => zero; set => zero = value; }
        }

        public struct Catalog_header
        {
            private Int32 start_empty; // начало первого пустого блока
            private Int32 page_size;   // размер страницы по умолчанию
            private Int32 version;     // версия
            private Int32 zero;        // всегда ноль?

            public int Start_empty { get => start_empty; set => start_empty = value; }
            public int Page_size { get => page_size; set => page_size = value; }
            public int Version { get => version; set => version = value; }
            public int Zero { get => zero; set => zero = value; }
        }

        public enum Block_header : int
        {
            doc_len   = 2,
	        block_len = 11,
	        nextblock = 20
        }
        #endregion

        #region InflateAndDeflate

        /// <summary>
        /// Распаковка
        /// </summary>
        /// <param name="compressedMemoryStream"></param>
        /// <param name="outBufStream"></param>
        /// <returns></returns>
        public static bool Inflate(MemoryTributary compressedMemoryStream, out MemoryTributary outBufStream)
        {
            bool result = true;

            outBufStream = new MemoryTributary();

            try
            {
                compressedMemoryStream.Position = 0;
                System.IO.Compression.DeflateStream decompressStream = new System.IO.Compression.DeflateStream(compressedMemoryStream, System.IO.Compression.CompressionMode.Decompress);
                decompressStream.CopyTo(outBufStream);
            }
            catch (Exception ex)
            {
                outBufStream = compressedMemoryStream;
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Сжатие
        /// </summary>
        /// <param name="pDataStream"></param>
        /// <param name="outBufStream"></param>
        /// <returns></returns>
        public static bool Deflate(MemoryTributary pDataStream, out MemoryTributary outBufStream)
        {
            bool result = true;

            int DataSize = (int)pDataStream.Length;
            outBufStream = new MemoryTributary();

            pDataStream.Position = 0;
            try
            {
                MemoryTributary srcMemStream = pDataStream;
                {
                    using (MemoryTributary compressedMemStream = new MemoryTributary())
                    {
                        using (System.IO.Compression.DeflateStream strmDef = new System.IO.Compression.DeflateStream(compressedMemStream, System.IO.Compression.CompressionMode.Compress))
                        {
                            srcMemStream.CopyTo(strmDef);
                        }

                        outBufStream = compressedMemStream;
                    }
                }
            }
            catch (Exception ex)
            {
                outBufStream = pDataStream;
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Распаковка произвольных файлов
        /// </summary> 
        public void Inflate(string in_filename, string out_filename, bool enableNewCode = true)
        {
            if (!File.Exists(in_filename))
                throw new Exception("Input file not found!");

            using (FileStream fileReader = File.Open(in_filename, FileMode.Open))
            {
                MemoryTributary memOutBuffer;
                using (MemoryTributary memBuffer = new MemoryTributary())
                {
                    fileReader.CopyTo(memBuffer);

                    bool success = Inflate(memBuffer, out memOutBuffer);
                    if (!success)
                        throw new Exception("Inflate error!");

                    using (FileStream fileWriter = new FileStream(out_filename, FileMode.Create))
                    {
                        memOutBuffer.Position = 0;
                        memOutBuffer.CopyTo(fileWriter);
                    }
                    memOutBuffer.Close();
                }
            }
        }

        /// <summary>
        /// Сжатие произвольных файлов
        /// </summary>
        public void Deflate(string in_filename, string out_filename, bool enableNewCode = true)
        {
            if (!File.Exists(in_filename))
                throw new Exception("Input file not found!");

            using (FileStream fileReader = File.Open(in_filename, FileMode.Open))
            {
                MemoryTributary memOutBuffer;
                using (MemoryTributary memBuffer = new MemoryTributary())
                {
                    fileReader.CopyTo(memBuffer);

                    bool success = Deflate(memBuffer, out memOutBuffer);
                    if (!success)
                        throw new Exception("Deflate error!");

                    using (FileStream fileWriter = new FileStream(out_filename, FileMode.Create))
                    {
                        memOutBuffer.Position = 0;
                        memOutBuffer.CopyTo(fileWriter);
                    }
                    memOutBuffer.Close();
                }
            }
        }

        #endregion

        #region Service
        /// <summary>
        /// _httoi(Byte[] value) - преобразует массив Byte[] в целое значение 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static UInt32 _httoi(Byte[] value)
        {
            UInt32 result = 0;

            string newByte = System.Text.Encoding.Default.GetString(value);
            result = UInt32.Parse(newByte, System.Globalization.NumberStyles.HexNumber);

            return result;
        }

        /// <summary>
        /// _intTo_BytesChar - преобразует целое значение в массив Byte[]
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Byte[] _intTo_BytesChar(UInt32 value)
        {
            string valueString = IntToHexString((int)value, 8).ToLower();
            Byte[] resultBytes = new Byte[8];

            for (int i = 0; i < valueString.Length; i++)
                resultBytes[i] = Convert.ToByte(valueString[i]);

            return resultBytes;
        }

        /// <summary>
        /// _inttoBytes - преобразует целое значение в массив Byte[]
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Byte[] _inttoBytes(UInt32 value)
        {
            string valueString = IntToHexString((int)value, 8).ToUpper();

            Byte[] resultBytes = new Byte[8];

            for (int i = 0; i < valueString.Length; i++)
            {
                switch (valueString[i])
                {
                    case 'A':
                        resultBytes[i] = 10;
                        break;
                    case 'B':
                        resultBytes[i] = 11;
                        break;
                    case 'C':
                        resultBytes[i] = 12;
                        break;
                    case 'D':
                        resultBytes[i] = 13;
                        break;
                    case 'E':
                        resultBytes[i] = 14;
                        break;
                    case 'F':
                        resultBytes[i] = 15;
                        break;
                    default:
                        resultBytes[i] = (Byte)(Convert.ToByte(valueString[i]) - 0x30);
                        break;
                }
            }

            return resultBytes;
        }

        /// <summary>
        /// IntToHexString - преобразование целого с определенной длиной в строку шестнадцатеричную
        /// </summary>
        /// <param name="n"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        public static String IntToHexString(int n, int len)
        {
            Char[] ch = new Char[len--];
            for (int i = len; i >= 0; i--)
            {
                ch[len - i] = ByteToHexChar((Byte)((uint)(n >> 4 * i) & 15));
            }
            return new String(ch);
        }

        public static Int32 HexStringToInt(String instr)
        {
            Int32 Result = 0;

            Result = Convert.ToInt32(instr);

            return Result;
        }

        /// <summary>
        /// ByteToHexChar - перевод байта в шестнадцатеричный символ (a-f)
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Char ByteToHexChar(Byte b)
        {
            if (b < 0 || b > 15)
                throw new Exception("IntToHexChar: input out of range for Hex value");
            return b < 10 ? (Char)(b + 48) : (Char)(b + 55);
        }

        /// <summary>
        /// ClearTempData - Очистка временного каталога
        /// </summary>
        /// <param name="tmpFolder"></param>
        /// <param name="_tmpFolder"></param>
        public static void ClearTempData(String tmpFolder = "", String _tmpFolder = "")
        {
            if (!String.IsNullOrEmpty(tmpFolder))
            {
                try
                {
                    Directory.Delete(_tmpFolder, true);
                }
                catch
                {
                }
            }

            String V8FormatsTmp = String.Format("{0}V8Formats{1}", Path.GetTempPath(), Path.DirectorySeparatorChar);
            if (Directory.Exists(V8FormatsTmp))
            {
                string[] foundDirectories = Directory.GetDirectories(V8FormatsTmp);
                foreach (string dirFullname in foundDirectories)
                {
                    try
                    {
                        DirectoryInfo tmpDir = new DirectoryInfo(dirFullname);
                        if (tmpDir.CreationTime < DateTime.Now.AddHours(-1))
                        {
                            tmpDir.Delete(true);
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }
        }

        /// <summary>
        /// Определяет размер каталога
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public static long DirSize(DirectoryInfo d)
        {
            long Size = 0;
            // Добавляем размер файлов
            FileInfo[] fis = d.GetFiles();
            foreach (FileInfo fi in fis)
            {
                Size += fi.Length;
            }
            // Добавляем размер подкаталогов
            DirectoryInfo[] dis = d.GetDirectories();
            foreach (DirectoryInfo di in dis)
            {
                Size += DirSize(di);
            }
            return (Size);
        }
        #endregion


        /// <summary>
        /// установка текущего времени
        /// </summary>
        /// <param name="v8t"></param>
        public static void SetCurrentTime(Int64 v8t)
        {
            //SYSTEMTIME st;
            //FILETIME ft;

            //GetSystemTime(&st);
            //SystemTimeToFileTime(&st, &ft);
            //FileTimeToV8time(&ft, v8t);
            v8t = 100500;
        }

        public static UInt64 GetTickCount()
        {
            return 100500;
        }    
        /// <summary>
        /// Читает блок из потока каталога stream_from, собирая его по страницам
        /// </summary>
        /// <param name="stream_from"></param>
        /// <param name="start"></param>
        /// <param name="stream_to"></param>
        /// <returns></returns>
        Stream Read_block(Stream stream_from, int start, Stream stream_to = null)
        {
            //std::array<char, BLOCK_HEADER_LEN> temp_buf;
            //Char[] temp_buf = new Char[BLOCK_HEADER_LEN];

            //ArrayList temp_buf = new ArrayList(BLOCK_HEADER_LEN2);

            Byte[] temp_buf = new Byte[BLOCK_HEADER_LEN];

            int len, curlen, pos, readlen;

            if (stream_to != null)
            { 
                stream_to = new MemoryStream();

                stream_to.Seek(0, SeekOrigin.Begin);
                stream_to.SetLength(0);
            }

            if (start < 0 || start == LAST_BLOCK || start > stream_from.Length)
                return stream_to;

            stream_from.Seek(start, SeekOrigin.Begin);
            stream_from.Read(temp_buf, 0, temp_buf.Length - 1);

            String hex_len = "0x";

            Array ahex_len = hex_len.ToArray();

            int indxdest = ahex_len.Length-1;

            Array.Copy(temp_buf, (int)Block_header.doc_len, ahex_len, indxdest, HEX_INT_LEN);

            len = HexStringToInt(hex_len);

            if (len != 0)
                return stream_to;

            String hex_curlen = "0x";

            Array ahex_curlen = hex_curlen.ToArray();

            int indxdest_curlen = ahex_curlen.Length - 1;

            Array.Copy(temp_buf, (int)Block_header.block_len, ahex_curlen, indxdest_curlen, HEX_INT_LEN);

            curlen = HexStringToInt(ahex_curlen.ToString());
                        
            String hex_start = "0x";

            Array ahex_start = hex_start.ToArray();

            int indxdest_hex_start = ahex_start.Length - 1;

            Array.Copy(temp_buf, (int)Block_header.nextblock, ahex_start, indxdest_hex_start, HEX_INT_LEN);

            start = HexStringToInt(ahex_start.ToString());

            readlen = Math.Min(len, curlen);

            stream_from.CopyTo(stream_to, readlen);

            pos = readlen;

            while (start != LAST_BLOCK)
            {

                stream_from.Seek(start, SeekOrigin.Begin);
                stream_from.Read(temp_buf, 0, temp_buf.Length - 1);

                String hex_curlen1 ="0x";

                Array ahex_curlen1 = hex_curlen1.ToArray();

                int indxdest_curlen1 = ahex_curlen1.Length - 1;

                Array.Copy(temp_buf, (int)Block_header.block_len, ahex_curlen1, indxdest_curlen1, HEX_INT_LEN);

                curlen = HexStringToInt(ahex_curlen1.ToString());

                String hex_start1 = "0x";

                Array ahex_start1 = hex_start1.ToArray();

                int indxdest_hex_start1 = ahex_start.Length - 1;

                Array.Copy(temp_buf, (int)Block_header.nextblock, ahex_start1, indxdest_hex_start1, HEX_INT_LEN);

                start = HexStringToInt(ahex_start1.ToString());

                readlen = Math.Min(len - pos, curlen);

                stream_from.CopyTo(stream_to, readlen);
                
                pos += readlen;

            }

            return stream_to;

        }

        /// <summary>
        /// Преобразование строки в байтовый массив
        /// </summary>
        /// <param name="instr"></param>
        /// <param name="enc"></param>
        /// <returns></returns>
        public static Byte[] StringToByteArr(String instr, Encoding enc)
        {
            return enc.GetBytes(instr);
        }


    } // окончание класса APIcfBase     
}
