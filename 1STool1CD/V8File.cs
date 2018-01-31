using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace _1STool1CD
{
    public enum FileIsCatalog { unknown, yes, no }

    public class v8file
    {
        /// <summary>
        /// Вложенный класс...
        /// </summary>

        public class TV8FileStream : MemoryTributary
        {
            #region public
            /// <summary>
            /// Основной конструктор
            /// </summary>
            /// <param name="f"></param>
            /// <param name="ownfile"></param>
            public TV8FileStream(v8file f, bool ownfile = false)
            {
                pos = 0;
                f.streams.Add(this);
            }

            public virtual Int64 Read(byte[] Buffer, Int64 Count)
            {
                int r = (int)file.Read(Buffer, (int)pos, (int)Count);
                pos += r;
                return r;
            }

            public override int Read(byte[] Buffer, int Offset, int Count)
            {
                // int r = (int)file.Read(Buffer, (int)Offset, (int)Count); - возможно надо так ????
                int r = (int)file.Read(Buffer, (int)pos, (int)Count);
                pos += r;
                return r;
            }

            public virtual Int64 Write(byte[] Buffer, Int64 Count)
            {
                int r = (int)file.Write(Buffer, (int)pos, (int)Count);
                pos += r;
                return r;
            }

            public override void Write(byte[] Buffer, int Offset, int Count)
            {
                // int r = (int)file.Write(Buffer, (int)Offset, (int)Count); - Возможно должно быть так ????
                int r = (int)file.Write(Buffer, (int)pos, (int)Count);
                pos += r;
            }
            public override Int64 Seek(Int64 Offset, SeekOrigin Origin)
            {
                Int64 len = file.GetFileLength();
                switch (Origin)
                {
                    case SeekOrigin.Begin:
                        if (Offset >= 0)
                        {
                            if (Offset <= len)
                            {
                                pos = Offset;
                            }
                            else
                            {
                                pos = len;
                            }
                        }
                        break;
                    case SeekOrigin.Current:
                        if (pos + Offset < len)
                        {
                            pos += Offset;
                        }
                        else
                        {
                            pos = len;
                        }
                        break;
                    case SeekOrigin.End:
                        if (Offset <= 0)
                        {
                            if (Offset <= len)
                            {
                                pos = len - Offset;
                            }
                            else
                            {
                                pos = 0;
                            }
                        }
                        break;
                }
                return pos;
            }
            #endregion

            #region protected

            protected v8file file;
            protected bool own;
            protected Int64 pos;
            //protected int pos;

            #endregion

        }

        public v8file(v8catalog _parent, String _name, v8file _previous, int _start_data, int _start_header, Int64 _time_create, Int64 _time_modify) { }

        public bool IsCatalog() { return true; }
        public v8catalog GetCatalog() { return new v8catalog(" "); }

        public Int64 GetFileLength()
        {
            Int64 ret = 0;
            
            if (!try_open())
            {
                return ret;
            }

            ret = data.Length;
            
            return ret;
            
        }

        public Int64 Read(byte[] Buffer, int Start, int Length)
        {

            Int64 ret = 0;
            
            if (!try_open())
            {
                return ret;
            }

            data.Seek(Start, SeekOrigin.Begin);
            data.Read(Buffer, Start, Length);
            
            return ret;
            
        }
        

        public Int64 Write(byte[] Buffer, int Start, int Length){ return 100; }                           // дозапись/перезапись частично
        public Int64 Write(byte[] Buffer, int Length) { return 100; }                                     // перезапись целиком
        public Int64 Write(MemoryTributary Stream_, int Start, int Length) { return 100; }                         // дозапись/перезапись частично
        public Int64 Write(MemoryTributary Stream_) { return 100; }                                                // перезапись целиком

        public String GetFileName() { return " "; }
        public String GetFullName() { return " "; }

        public void SetFileName(String _name) { }
        public v8catalog GetParentCatalog() { return new v8catalog(""); }
        public void DeleteFile() { }
        public v8file GetNext() { return null; }
        public bool Open() { return true; }
        public void Close() { }

        public Int64 WriteAndClose(MemoryTributary Stream_, int Length = -1) { return 100; } // перезапись целиком и закрытие файла (для экономии памяти не используется data файла)

        /* надо реализовывать
        public void GetTimeCreate(System::FILETIME* ft);
        public void GetTimeModify(System::FILETIME* ft);
        public void SetTimeCreate(System::FILETIME* ft);
        public void SetTimeModify(System::FILETIME* ft);
        */

        public void SaveToFile(String FileName) { }

        public void SaveToStream(MemoryTributary stream)
        {
            if (!try_open())
            {
                return;
            }

            data.CopyTo(stream);
        }
        
        //public TV8FileStream* get_stream(bool own = false);
        public void Flush() { }

        private String name;
        private Int64 time_create;
        private Int64 time_modify;
        
        private MemoryTributary data;
        private v8catalog parent;
        private FileIsCatalog iscatalog;

        private v8catalog self;        // указатель на каталог, если файл является каталогом
        private v8file next;           // следующий файл в каталоге
        private v8file previous;       // предыдущий файл в каталоге
        private bool is_opened;         // признак открытого файла (инициализирован поток data)
        private int start_data;         // начало блока данных файла в каталоге (0 означает, что файл в каталоге не записан)
        private int start_header;       // начало блока заголовка файла в каталоге
        private bool is_datamodified;   // признак модифицированности данных файла (требуется запись в каталог при закрытии)
        private bool is_headermodified; // признак модифицированности заголовка файла (требуется запись в каталог при закрытии)
        private bool is_destructed;     // признак, что работает деструктор
        private bool flushed;           // признак, что происходит сброс
        private bool selfzipped;        // Признак, что файл является запакованным независимо от признака zipped каталога

        // private std::set<TV8FileStream*> streams; ХЗ пока что это
        private SortedSet<TV8FileStream> streams;

        private bool try_open()
        {
            return (is_opened ? true : Open());
        }


    }
}
