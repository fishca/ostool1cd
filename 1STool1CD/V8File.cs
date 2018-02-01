using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static _1STool1CD.APIcfBase;


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

        /// <summary>
        /// Основной конструктор
        /// </summary>
        /// <param name="_parent"></param>
        /// <param name="_name"></param>
        /// <param name="_previous"></param>
        /// <param name="_start_data"></param>
        /// <param name="_start_header"></param>
        /// <param name="_time_create"></param>
        /// <param name="_time_modify"></param>
        public v8file(v8catalog _parent, String _name, v8file _previous, int _start_data, int _start_header, Int64 _time_create, Int64 _time_modify)
        {
            is_destructed = false;
            flushed       = false;
            parent        = _parent;
            name          = _name;
            previous      = _previous;
            next          = null;
            data          = null;
            start_data    = _start_data;
            start_header  = _start_header;

            //is_datamodified = !start_data;
            is_datamodified = !(start_data == 0) ? true : false;

            //is_headermodified = !start_header;
            is_headermodified = !(start_header == 0) ? true : false;

            if (previous != null)
                previous.next = this;
            else
                parent.first = this;

            iscatalog = FileIsCatalog.unknown;
            self      = null;
            is_opened = false;

            time_create = _time_create;
            time_modify = _time_modify;

            selfzipped = false;

            if (parent != null)
                parent.files[name.ToUpper()] = this;

        }

        public bool IsCatalog()
        {
            Int64 _filelen;
            Int32 _startempty = -1;

            //Char[] _t = new Char[BLOCK_HEADER_LEN];
            byte[] _t = new byte[BLOCK_HEADER_LEN];


            if (iscatalog == FileIsCatalog.unknown)
            {
                // эмпирический метод?
                if (!try_open())
                {
                    return false;
                }
                _filelen = data.Length;
                if (_filelen == CATALOG_HEADER_LEN)
                {
                    data.Seek(0, SeekOrigin.Begin);
                    data.Read(_t, 0, (int)CATALOG_HEADER_LEN);
                    if (!_t.ToString().StartsWith(_EMPTY_CATALOG_TEMPLATE))
                    {
                        iscatalog = FileIsCatalog.no;
                        return false;
                    }
                    else
                    {
                        iscatalog = FileIsCatalog.yes;
                        return true;
                    }

                }
                data.Seek(0, SeekOrigin.Begin);
                //data.Read(&_startempty, 4);  TODO: ХЗ что с этим делать
                if (_startempty != LAST_BLOCK)
                {
                    if (_startempty + 31 >= _filelen)
                    {
                        iscatalog = FileIsCatalog.no;
                        return false;
                    }
                    data.Seek(_startempty, SeekOrigin.Begin);
                    data.Read(_t, 0, 31);
                    if (_t[0] != 0xd || _t[1] != 0xa || _t[10] != 0x20 || _t[19] != 0x20 || _t[28] != 0x20 || _t[29] != 0xd || _t[30] != 0xa)
                    {
                        iscatalog = FileIsCatalog.no;
                        return false;
                    }
                }
                if (_filelen < (BLOCK_HEADER_LEN - 1 + CATALOG_HEADER_LEN))
                {
                    iscatalog = FileIsCatalog.no;
                    return false;
                }
                data.Seek(CATALOG_HEADER_LEN, SeekOrigin.Begin);
                data.Read(_t, 0, 31);
                if (_t[0] != 0xd || _t[1] != 0xa || _t[10] != 0x20 || _t[19] != 0x20 || _t[28] != 0x20 || _t[29] != 0xd || _t[30] != 0xa)
                {
                    iscatalog = FileIsCatalog.no;
                    return false;
                }
                iscatalog = FileIsCatalog.yes;
                return true;
            }

            return iscatalog == FileIsCatalog.yes;

        }

        public v8catalog GetCatalog()
        {
            v8catalog ret;
            
            if (IsCatalog())
            {
                if (self != null)
                {
                    self = new v8catalog(this);
                }
                ret = self;
            }
            else
                ret = null;
            
            return ret;
        }


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
        public v8catalog GetParentCatalog() { return parent; }

        public void DeleteFile()
        {
            if (parent != null)
            {
                if (next != null)
                {
                    next.previous = previous;
                }
                else parent.last = previous;
                if (previous != null)
                {
                    previous.next = next;
                }
                else
                    parent.first = next;

                parent.is_fatmodified = true;
                parent.free_block(start_data);
                parent.free_block(start_header);
                //parent.files.erase(name.UpperCase());  TODO: что-то надо с этим делать
                parent = null;
            }
            data = null;
            data = null;
            if (self != null)
            {
                this.self.data = null;
                this.self.data = null;
                self = null;
            }
            iscatalog = FileIsCatalog.no;
            next = null;
            previous = null;
            is_opened = false;
            start_data = 0;
            start_header = 0;
            is_datamodified = false;
            is_headermodified = false;

        }

        public v8file GetNext() { return next; }


        public bool Open()
        {
            if (parent != null) return false;
            
            if (is_opened)
            {
                return true;
            }
            data = parent.read_datablock(start_data);
            is_opened = true;
            return true;
        }


        public void Close()
        {

        }

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

        public v8catalog self;        // указатель на каталог, если файл является каталогом
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
