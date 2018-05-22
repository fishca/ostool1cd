using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static _1STool1CD.APIcfBase;
using static _1STool1CD.Structures;


namespace _1STool1CD
{

    /// <summary>
    /// Класс v8catalog  
    /// </summary>
    public class V8catalog
    {
        #region public

        #region Конструкторы класса
        /// <summary>
        /// создать каталог из файла
        /// </summary>
        /// <param name="f"></param>
        public V8catalog(v8file f)
        {
            is_cfu = false;
            iscatalogdefined = false;
            File = f;
            
            File.Open();
            Data = File.Data;
            zipped = false;

            if (IsCatalog()) initialize();
            else
            {
                First = null;
                Last = null;
                start_empty = 0;
                page_size = 0;
                version = 0;
                zipped = false;

                Is_fatmodified = false;
                is_emptymodified = false;
                is_modified = false;
                Is_destructed = false;
                flushed = false;
                leave_data = false;
            }
            

        }   // создать каталог из файла

        /// <summary>
        /// создать каталог из физического файла (cf, epf, erf, hbk, cfu)
        /// </summary>
        /// <param name="name"></param>
        public V8catalog(String name)
        {

            
            iscatalogdefined = false;

            String ext = Path.GetExtension(name).ToLower();
            if (ext == str_cfu)
            {
                is_cfu = true;
                zipped = false;
                //data = new MemoryStream();
                Data = new MemoryTributary();
                if (!System.IO.File.Exists(name))
                {
                    //data.WriteBuffer(_EMPTY_CATALOG_TEMPLATE, CATALOG_HEADER_LEN);
                    Data.Write(StringToByteArr(_EMPTY_CATALOG_TEMPLATE, Encoding.UTF8), 0, CATALOG_HEADER_LEN2);
                    cfu = new FileStream(name, FileMode.Create);
                }
                else
                {
                    cfu = new FileStream(name, FileMode.Append);
                    // Inflate((MemoryTributary)cfu, out data); TODO надо дорабатывать обязательно
                }
            }
            else
            {
                zipped = ext == str_cf || ext == str_epf || ext == str_erf || ext == str_cfe;
                is_cfu = false;

                if (!System.IO.File.Exists(name))
                {
                    FileStream data1 = new FileStream(name, FileMode.Create);
                    data1.Write(StringToByteArr(_EMPTY_CATALOG_TEMPLATE, Encoding.UTF8), 0, CATALOG_HEADER_LEN2);
                    //data1 = null;
                    data1.Dispose();
                }
                Data = new FileStream(name, FileMode.Append);
            }

            File = null;
            if (IsCatalog()) initialize();
            else
            {
                First = null;
                Last = null;
                start_empty = 0;
                page_size = 0;
                version = 0;
                zipped = false;

                Is_fatmodified = false;
                is_emptymodified = false;
                is_modified = false;
                Is_destructed = false;
                flushed = false;
                leave_data = false;
            }

            cfu.Dispose();
            Data.Dispose();

        } // создать каталог из физического файла (cf, epf, erf, hbk, cfu)

        /// <summary>
        /// создать каталог из физического файла (cf, epf, erf, hbk, cfu)
        /// </summary>
        /// <param name="name"></param>
        /// <param name="_zipped"></param>
        public V8catalog(String name, bool _zipped)
        {
            
            iscatalogdefined = false;
            is_cfu = false;
            zipped = _zipped;

            if (!System.IO.File.Exists(name))
            {
                FileStream data = new FileStream(name, FileMode.Create);
                data.Write(StringToByteArr(_EMPTY_CATALOG_TEMPLATE, Encoding.UTF8), 0, CATALOG_HEADER_LEN2);
                data.Dispose();
            }
            Data = new FileStream(name, FileMode.Append);
            File = null;
            if (IsCatalog()) initialize();
            else
            {
                First = null;
                Last = null;
                start_empty = 0;
                page_size = 0;
                version = 0;
                zipped = false;

                Is_fatmodified = false;
                is_emptymodified = false;
                is_modified = false;
                Is_destructed = false;
                flushed = false;
                leave_data = false;
            }

            Data.Dispose();

        } // создать каталог из физического файла (cf, epf, erf, hbk, cfu)

        /// <summary>
        /// Создать каталог из потока
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="_zipped"></param>
        /// <param name="leave_stream"></param>
        public V8catalog(Stream stream, bool _zipped, bool leave_stream = false)
        {
            
            is_cfu = false;
            iscatalogdefined = false;
            zipped = _zipped;
            Data = stream;
            File = null;

            if (Data.Length != 0)
                Data.Write(StringToByteArr(_EMPTY_CATALOG_TEMPLATE, Encoding.UTF8), 0, CATALOG_HEADER_LEN2);

            if (IsCatalog())
                initialize();
            else
            {
                First = null;
                Last = null;
                start_empty = 0;
                page_size = 0;
                version = 0;
                zipped = false;

                Is_fatmodified = false;
                is_emptymodified = false;
                is_modified = false;
                Is_destructed = false;
                flushed = false;
            }
            leave_data = leave_stream;
        } // создать каталог из потока
        #endregion

        /// <summary>
        /// Это каталог?
        /// </summary>
        /// <returns></returns>
        public bool IsCatalog()
        {

            Int64 _filelen;
            Int32 _startempty = (-1);
            //char _t[BLOCK_HEADER_LEN];
            Byte[] _t = new Byte[BLOCK_HEADER_LEN];
            
            if (iscatalogdefined)
            {
                
                return iscatalog;
            }
            iscatalogdefined = true;
            iscatalog = false;

            // эмпирический метод?
            _filelen = Data.Length;
            if (_filelen == CATALOG_HEADER_LEN2)
            {
                Data.Seek(0, SeekOrigin.Begin);
                Data.Read(_t, 0, CATALOG_HEADER_LEN2);
                //if (memcmp(_t, _EMPTY_CATALOG_TEMPLATE, CATALOG_HEADER_LEN) != 0)
                if (!_t.ToString().StartsWith(_EMPTY_CATALOG_TEMPLATE))
                    {
                    
                    return false;
                }
                else
                {
                    iscatalog = true;
                    
                    return true;
                }
            }

            Data.Seek(0, SeekOrigin.Begin);
            //data->Read(&_startempty, 4); TODO: ХЗ что с этим делать
            if (_startempty != LAST_BLOCK)
            {
                if (_startempty + 31 >= _filelen)
                {
                    
                    return false;
                }
                Data.Seek(0, SeekOrigin.Begin);
                Data.Read(_t, 0, 31);
                if (_t[0] != 0xd || _t[1] != 0xa || _t[10] != 0x20 || _t[19] != 0x20 || _t[28] != 0x20 || _t[29] != 0xd || _t[30] != 0xa)
                {
                    
                    return false;
                }
            }
            if (_filelen < (BLOCK_HEADER_LEN - 1 + CATALOG_HEADER_LEN))
            {
                
                return false;
            }
            
            Data.Seek(CATALOG_HEADER_LEN, SeekOrigin.Begin);
            Data.Read(_t, 0, 31);
            if (_t[0] != 0xd || _t[1] != 0xa || _t[10] != 0x20 || _t[19] != 0x20 || _t[28] != 0x20 || _t[29] != 0xd || _t[30] != 0xa)
            {
                
                return false;
            }
            iscatalog = true;
            
            return true;


        }

        /// <summary>
        /// Получить файл по имени
        /// </summary>
        /// <param name="FileName"></param>
        /// <returns></returns>
        public v8file GetFile(String FileName)
        {
            v8file ret = null;

            foreach (KeyValuePair<String, v8file> kvp in Files)
            {
                if (kvp.Key.Equals(FileName))
                { 
                    ret = kvp.Value;
                    break;
                }
                else
                    ret = null;
            }
            return ret;
        }

        /// <summary>
        /// Получить первый
        /// </summary>
        /// <returns></returns>
        public v8file GetFirst()
        {
            return First;
        }

        /// <summary>
        /// Создать файл
        /// </summary>
        /// <param name="FileName"></param>
        /// <param name="_selfzipped"></param>
        /// <returns></returns>
        public v8file CreateFile(String FileName, bool _selfzipped = false)
        {
            Int64 v8t = 0; ;
            v8file f;

            f = GetFile(FileName);
            if ( f != null)
            {
                SetCurrentTime(v8t);
                f = new v8file(this, FileName, Last, 0, 0, v8t, v8t);
                f.Selfzipped = _selfzipped;
                Last = f;
                Is_fatmodified = true;
            }
            
            return f;
            
        }         // CreateFile в win64 определяется как CreateFileW, пришлось заменить на маленькую букву

        /// <summary>
        /// Создать каталог
        /// </summary>
        /// <param name="FileName"></param>
        /// <param name="_selfzipped"></param>
        /// <returns></returns>
        public V8catalog CreateCatalog(String FileName, bool _selfzipped = false)
        {
            V8catalog ret;
            
            v8file f = CreateFile(FileName, _selfzipped);
            if (f.GetFileLength() > 0)
            {
                if (f.IsCatalog()) ret = f.GetCatalog();
                else ret = null;
            }
            else
            {
                f.Write(Encoding.UTF8.GetBytes(_EMPTY_CATALOG_TEMPLATE), CATALOG_HEADER_LEN2);
                ret = f.GetCatalog();
            }
            
            return ret;
            return null;
        }

        /// <summary>
        /// Удалить файл по имени
        /// </summary>
        /// <param name="FileName"></param>
        public void DeleteFile(String FileName)
        {
            v8file f = First;
            while (f != null)
            {
                //if (!f.name.CompareIC(FileName))
                if (String.Compare(f.Name, FileName, true) != 0)
                {
                    f.DeleteFile();
                    f = null;
                }

                if (f != null)
                    f = f.Next;
            }
        }

        /// <summary>
        /// Получить родительский каталог
        /// </summary>
        /// <returns></returns>
        public V8catalog GetParentCatalog()
        {
            return File?.Parent;
        }

        /// <summary>
        /// Получить свой файл
        /// </summary>
        /// <returns></returns>
        public v8file GetSelfFile()
        {
            return File;
        }

        /// <summary>
        /// Сохранить в каталог на диске
        /// </summary>
        /// <param name="DirName"></param>
        public void SaveToDir(String DirName)
        {

            v8file f = First;

            DirectoryInfo di = new DirectoryInfo(DirName);

            di.Create();

            if (!DirName.EndsWith("\\"))
                DirName += '\\';

            while (f != null)
            {
                if (f.IsCatalog())
                    f.GetCatalog().SaveToDir(DirName + f.Name);
                else
                    f.SaveToFile(DirName + f.Name);
                f.Close();
                f = f.Next;
            }



            /*
            CreateDir(DirName);
            if (DirName.SubString(DirName.Length(), 1) != str_backslash) DirName += str_backslash;
            
            v8file* f = first;
            while (f)
            {
                if (f->IsCatalog()) f->GetCatalog()->SaveToDir(DirName + f->name);
                else f->SaveToFile(DirName + f->name);
                f->Close();
                f = f->next;
            }
            */
        }

        /// <summary>
        /// Файл открыт?
        /// </summary>
        /// <returns></returns>
        public bool IsOpen()
        {
            return IsCatalog();
        }

        /// <summary>
        /// Сбросить
        /// </summary>
        public void Flush()
        {
            Fat_item fi = new Fat_item();
            v8file f;

            
            if (flushed)
            {
                
                return;
            }
            flushed = true;

            f = First;
            while (f != null)
            {
                f.Flush();
                f = f.Next;
            }

            if (Data != null)
            {
                if (Is_fatmodified)
                {
                    MemoryStream fat = new MemoryStream();
                    fi.Ff = LAST_BLOCK2;
                    f = First;
                    while (f != null)
                    {
                        fi.Header_start = (uint)f.Start_header;
                        fi.Data_start = (uint)f.Start_data;
                        //fat.Write(fi, 0, 12); TODO: Подумать
                        f = f.Next;
                    }
                    write_block(fat, CATALOG_HEADER_LEN2, true);
                    Is_fatmodified = false;
                }

                if (is_emptymodified)
                {
                    Data.Seek(0, SeekOrigin.Begin);
                    // data.Write(start_empty, 0, 4); TODO: Подумать
                    is_emptymodified = false;
                }
                if (is_modified)
                {
                    version++;
                    Data.Seek(0, SeekOrigin.Begin);
                    // data.Write(version, 0, 4); TODO: Подумать
                }
            }

            if (File != null)
            {
                if (is_modified)
                {
                    File.Is_datamodified = true;
                }
                File.Flush();
            }
            else
            {
                if (is_cfu)
                {
                    if (Data != null && cfu != null && is_modified)
                    {
                        Data.Seek(0, SeekOrigin.Begin);
                        cfu.Seek(0, SeekOrigin.Begin);

                        //ZDeflateStream(data, cfu);
                        // Deflate(data, out cfu); TODO: Додумать
                    }
                }
            }

            is_modified = false;
            flushed = false;

        }

        /// <summary>
        /// Хальф клосе
        /// </summary>
        public void HalfClose()
        {
            
            Flush();
            if (is_cfu)
            {
                
                cfu = null;
            }
            else
            {
                
                Data = null;
            }
            
        }

        /// <summary>
        /// Хальф опен
        /// </summary>
        /// <param name="name"></param>
        public void HalfOpen(String name)
        {
            
            if (is_cfu)
                cfu = new FileStream(name, FileMode.Append);
            else
                Data = new FileStream(name, FileMode.Append);
            
        }

        /// <summary>
        /// Получить первый файл
        /// </summary>
        /// <returns></returns>
        public v8file GetFirstFile()
        {
            return First;
        }

        /// <summary>
        /// Установить первый файл
        /// </summary>
        /// <param name="value"></param>
        public void FirstFile(v8file value)
        {
            First = value;
        }

        /// <summary>
        /// Получить последний файл
        /// </summary>
        /// <returns></returns>
        public v8file GetLastFile()
        {
            return Last;
        }

        /// <summary>
        /// Последний файл
        /// </summary>
        /// <param name="value"></param>
        public void LastFile(v8file value)
        {
            Last = value;
        }

        #endregion

        #region private

        private v8file file;  // файл, которым является каталог. Для корневого каталога NULL
        private Stream data; // поток каталога. Если file не NULL (каталог не корневой), совпадает с file->data
        private Stream cfu;  // поток файла cfu. Существует только при is_cfu == true
        private void initialize() { }
        //private v8file first; // первый файл в каталоге
        private v8file first; // первый файл в каталоге
        private v8file last;  // последний файл в каталоге
        //private SortedDictionary<String, v8file> files; // Соответствие имен и файлов
        private SortedDictionary<String, v8file> files; // Соответствие имен и файлов
        private Int64 start_empty; // начало первого пустого блока
        private int page_size;   // размер страницы по умолчанию
        private int version;     // версия
        private bool zipped;     // признак зазипованности файлов каталога
        private bool is_cfu;     // признак файла cfu (файл запакован deflate'ом)
        private bool iscatalog;
        private bool iscatalogdefined;

        private bool is_fatmodified;
        private bool is_emptymodified;
        private bool is_modified;

        public void free_block(int start) { }

        private int write_block(Stream block, int start, bool use_page_size, int len = -1) { return 0; }       // возвращает адрес начала блока
        public int write_datablock(Stream block, int start, bool _zipped = false, int len = -1) { return 0; } // возвращает адрес начала блока

        public MemoryTributary read_datablock(int start) { return null; }
        public Int64 get_nextblock(Int64 start) { return 0; }

        private bool is_destructed; // признак, что работает деструктор
        private bool flushed;       // признак, что происходит сброс
        private bool leave_data;    // признак, что не нужно удалять основной поток (data) при уничтожении объекта

        public v8file File { get { return file; } set { file = value; } }

        public Stream Data { get { return data; } set { data = value; } }

        public v8file First { get { return first; } set { first = value; } }

        public v8file Last { get { return last; } set { last = value; } }

        public SortedDictionary<string, v8file> Files { get { return files; } set { files = value; } }

        public bool Is_fatmodified { get { return is_fatmodified; } set { is_fatmodified = value; } }

        public bool Is_destructed { get { return is_destructed; } set { is_destructed = value; } }

        #endregion
    }
}
