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
        public v8file(V8catalog _parent, String _name, v8file _previous, int _start_data, int _start_header, Int64 _time_create, Int64 _time_modify)
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
                if (!Try_open())
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

        public V8catalog GetCatalog()
        {
            V8catalog ret;
            
            if (IsCatalog())
            {
                if (self != null)
                {
                    self = new V8catalog(this);
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
            
            if (!Try_open())
            {
                return ret;
            }

            ret = data.Length;
            
            return ret;
            
        }

        public Int64 Read(byte[] Buffer, int Start, int Length)
        {

            Int64 ret = 0;
            
            if (!Try_open())
            {
                return ret;
            }

            data.Seek(Start, SeekOrigin.Begin);
            data.Read(Buffer, Start, Length);
            
            return ret;
            
        }

        /// <summary>
        /// Дозапись/перезапись частично
        /// </summary>
        /// <param name="Buffer"></param>
        /// <param name="Start"></param>
        /// <param name="Length"></param>
        /// <returns></returns>
        public Int64 Write(byte[] Buffer, int Start, int Length)
        {
            Int64 ret = 0;

            if (!Try_open())
            {
                return ret;
            }
            SetCurrentTime(time_modify);
            is_headermodified = true;
            is_datamodified   = true;
            data.Seek(Start, SeekOrigin.Begin);
            data.Write(Buffer, Start, Length);
            ret = Length;

            return ret;
        }

        /// <summary>
        /// Перезапись целиком
        /// </summary>
        /// <param name="Buffer"></param>
        /// <param name="Length"></param>
        /// <returns></returns>
        public Int64 Write(byte[] Buffer, int Length)
        {
            Int64 ret = 0;
            
            if (!Try_open())
            {
                return ret;
            }
            SetCurrentTime(time_modify);
            is_headermodified = true;
            is_datamodified   = true;
            if (data.Length > Length)
                data.SetLength(Length);

            data.Seek(0, SeekOrigin.Begin);
            data.Write(Buffer, 0, Length);
            ret = Length;

            return ret;
        }

        /// <summary>
        /// Дозапись/перезапись частично
        /// </summary>
        /// <param name="Stream_"></param>
        /// <param name="Start"></param>
        /// <param name="Length"></param>
        /// <returns></returns>
        public Int64 Write(MemoryTributary Stream_, int Start, int Length)
        {
            Int64 ret = 0;
            if (!Try_open())
            {
                return ret;
            }
            SetCurrentTime(time_modify);
            is_headermodified = true;
            is_datamodified   = true;
            data.Seek(Start, SeekOrigin.Begin);
            Stream_.CopyTo(data, Length);
            ret = Length;

            return ret;
        }

        /// <summary>
        /// Перезапись целиком 
        /// </summary>
        /// <param name="Stream_"></param>
        /// <returns></returns>
        public Int64 Write(MemoryTributary Stream_)
        {
            Int64 ret = 0;
            if (!Try_open())
            {
                return ret;
            }
            SetCurrentTime(time_modify);
            is_headermodified = true;
            is_datamodified   = true;
            if (data.Length > Stream_.Length)
                data.SetLength(Stream_.Length);
            data.Seek(0, SeekOrigin.Begin);
            Stream_.CopyTo(data);
            ret = data.Length;

            return ret;
        }

        public String GetFileName() { return name; }

        public String GetFullName()
        {
            if (parent != null)
            { 
                if (parent.file != null)
                {
                    String fulln = parent.file.GetFullName();
                    if (!String.IsNullOrEmpty(fulln))
                    {
                        fulln += "\\";
                        fulln += name;
                        return fulln;
                    }
                }
            }
            return name;
        }

        public void SetFileName(String _name)
        {
            name = _name;
            is_headermodified = true;
        }

        public V8catalog GetParentCatalog() { return parent; }

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

        /// <summary>
        /// Открыть файл
        /// </summary>
        /// <returns></returns>
        public bool Open()
        {
            if (parent != null)
                return false;
            
            if (is_opened)
            {
                return true;
            }
            if (parent != null)
            {
                data = parent.read_datablock(start_data);
                is_opened = true;
            }
            return true;
        }

        /// <summary>
        /// Закрыть файл
        /// </summary>
        public void Close()
        {
            int _t = 0;

            if (parent != null) return;
            
            if (!is_opened) return;

            if (self != null) if (!self.is_destructed)
                {
                    self = null;
                }

            self = null;

            if (parent != null)
            {
                if (parent.data != null)
                {
                    if (is_datamodified || is_headermodified)
                    {

                        if (is_datamodified)
                        {
                            start_data = parent.write_datablock(this.data, start_data, selfzipped);
                        }
                        if (is_headermodified)
                        {
                            // TODO: Что-то с этим надо делать
                            /*
                            TMemoryStream* hs = new TMemoryStream();
                            hs->Write(&time_create, 8);
                            hs->Write(&time_modify, 8);
                            hs->Write(&_t, 4);
                            # ifndef _DELPHI_STRING_UNICODE // FIXME: определится используем WCHART или char
                            int ws = name.WideCharBufSize();
                            char* tb = new char[ws];
                            name.WideChar((WCHART*)tb, ws);
                            hs->Write((char*)tb, ws);
                            delete[] tb;
                            #else
                            hs->Write(name.c_str(), name.Length() * 2);
                            #endif
                            hs->Write(&_t, 4);

                            start_header = parent->write_block(hs, start_header, false);
                            delete hs;
                            */
                        }

                    }
                }
            }
            data = null;
            iscatalog = FileIsCatalog.unknown;
            is_opened = false;
            is_datamodified = false;
            is_headermodified = false;
        }

        /// <summary>
        /// Перезапись целиком и закрытие файла (для экономии памяти не используется data файла)
        /// </summary>
        /// <param name="Stream_"></param>
        /// <param name="Length"></param>
        /// <returns></returns>
        public Int64 WriteAndClose(MemoryTributary Stream_, int Length = -1)
        {
            Int32 _4bzero = 0;

            
            if (!Try_open())
            {
                return 0;
            }

            if (parent != null)
            {
                return 0;
            }

            if (self != null) 
                self = null;

            data = null;

            if (parent != null)
            {
                if (parent.data != null)
                {
                    /* TODO: Что-то с этим надо сделать
                     * 
                    int name_size = name.WideCharBufSize();
                    WCHART* wname = new WCHART[name_size];
                    name.WideChar(wname, name.Length());

                    parent->Lock->Acquire();
                    start_data = parent->write_datablock(Stream, start_data, selfzipped, Length);
                    TMemoryStream hs;
                    hs.Write(&time_create, 8);
                    hs.Write(&time_modify, 8);
                    hs.Write(&_4bzero, 4);
                    hs.Write(wname, name.Length() * sizeof(WCHART));
                    hs.Write(&_4bzero, 4);
                    start_header = parent->write_block(&hs, start_header, false);
                    parent->Lock->Release();
                    delete[] wname;
                    */
                }
            }
            iscatalog = FileIsCatalog.unknown;
            is_opened         = false;
            is_datamodified   = false;
            is_headermodified = false;

            if (Length == -1)
                return Stream_.Length;

            return Length;
        }

        /* надо реализовывать */

        public void GetTimeCreate(Int64 ft)
        {

        }

        public void GetTimeModify(Int64 ft)
        {

        }

        public void SetTimeCreate(Int64 ft)
        {

        }

        public void SetTimeModify(Int64 ft)
        {

        }

        public void SaveToFile(String FileName)
        {
            //FILETIME create, modify;

            /*
            # ifdef _MSC_VER

                struct _utimbuf ut;

            #else

        		struct utimbuf ut;

            #endif // _MSC_VER
            */
            if (!Try_open())
            {
                return;
            }

            /*
            TFileStream* fs = new TFileStream(FileName, fmCreate);
            Lock->Acquire();
            fs->CopyFrom(data, 0);
            Lock->Release();

            GetTimeCreate(&create);
            GetTimeModify(&modify);

            time_t RawtimeCreate = FileTime_to_POSIX(&create);
            struct tm * ptm_create = localtime(&RawtimeCreate);
            ut.actime = mktime(ptm_create);

            time_t RawtimeModified = FileTime_to_POSIX(&create);
            struct tm * ptm_modified = localtime(&RawtimeModified);
            ut.modtime = mktime(ptm_modified);

            # ifdef _MSC_VER

                _utime(FileName.c_str(), &ut);

            #else

                utime(FileName.c_str(), &ut);

            #endif // _MSC_VER

            delete fs;
            */
        }
    
        public void SaveToStream(MemoryTributary stream)
        {
            if (!Try_open())
            {
                return;
            }

            data.CopyTo(stream);
        }

        public TV8FileStream Get_stream(bool own = false)
        {
            return new TV8FileStream(this, own);
        }

        public void Flush()
        {
            int _t = 0;
            
            if (flushed)
            {
                return;
            }

            if ( parent != null )
            {
                return;
            }
            if (!is_opened)
            {
                return;
            }

            flushed = true;
            if (self != null) self.Flush();

            if (parent != null)
            {
                if (parent.data != null)
                {
                    if (is_datamodified || is_headermodified)
                    {

                        if (is_datamodified)
                        {
                            start_data = parent.write_datablock(data, start_data, selfzipped);
                            is_datamodified = false;
                        }
                        if (is_headermodified)
                        {
                            // TODO: Что-то надо делать с этим...
                            /*
                            TMemoryStream* hs = new TMemoryStream();
                            hs->Write(&time_create, 8);
                            hs->Write(&time_modify, 8);
                            hs->Write(&_t, 4);
                            # ifndef _DELPHI_STRING_UNICODE
                            int ws = name.WideCharBufSize();
                            char* tb = new char[ws];
                            name.WideChar((WCHART*)tb, ws);
                            hs->Write((char*)tb, ws);
                            delete[] tb;
                            #else
                            hs->Write(name.c_str(), name.Length() * 2);
                            #endif
                            hs->Write(&_t, 4);

                            start_header = parent->write_block(hs, start_header, false);
                            delete hs;
                            is_headermodified = false;
                            */
                        }
                    }
                }
            }
            flushed = false;
        }

        public bool Try_open()
        {
            return (is_opened ? true : Open());
        }

        public String name;
        private Int64 time_create;
        private Int64 time_modify;
        
        public MemoryTributary data;
        public V8catalog parent;
        private FileIsCatalog iscatalog;

        public V8catalog self;          // указатель на каталог, если файл является каталогом
        public v8file next;            // следующий файл в каталоге
        private v8file previous;        // предыдущий файл в каталоге
        private bool is_opened;         // признак открытого файла (инициализирован поток data)
        public int start_data;         // начало блока данных файла в каталоге (0 означает, что файл в каталоге не записан)
        public int start_header;       // начало блока заголовка файла в каталоге
        public bool is_datamodified;   // признак модифицированности данных файла (требуется запись в каталог при закрытии)
        private bool is_headermodified; // признак модифицированности заголовка файла (требуется запись в каталог при закрытии)
        private bool is_destructed;     // признак, что работает деструктор
        private bool flushed;           // признак, что происходит сброс
        public bool selfzipped;        // Признак, что файл является запакованным независимо от признака zipped каталога

        // private std::set<TV8FileStream*> streams; ХЗ пока что это
        private SortedSet<TV8FileStream> streams;

    }
}
