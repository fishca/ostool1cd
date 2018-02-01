using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace _1STool1CD
{
    /// <summary>
    /// Структура заголовка
    /// </summary>
    public struct fat_item
    {
        public UInt32 header_start;
        public UInt32 data_start;
        public UInt32 ff;            // всегда 7fffffff
    }

    /// <summary>
    /// Класс v8catalog
    /// </summary>
    public class v8catalog
    {
        #region public

        #region Конструкторы класса
        public v8catalog(v8file f) { }   // создать каталог из файла
        public v8catalog(String name) { } // создать каталог из физического файла (cf, epf, erf, hbk, cfu)
        public v8catalog(String name, bool _zipped) { } // создать каталог из физического файла (cf, epf, erf, hbk, cfu)
        public v8catalog(Stream stream, bool _zipped, bool leave_stream = false) { } // создать каталог из потока
        #endregion

        public bool IsCatalog() { return true; }

        public v8file GetFile(String FileName) { return null; }
		public v8file GetFirst() { return null; }
        public v8file createFile(String FileName, bool _selfzipped = false) { return null; }         // CreateFile в win64 определяется как CreateFileW, пришлось заменить на маленькую букву
        public v8catalog CreateCatalog(String FileName, bool _selfzipped = false) { return null; }
        public void DeleteFile(String FileName) { }
		public v8catalog GetParentCatalog() { return null; }
        public v8file GetSelfFile() { return null; }
        public void SaveToDir(String DirName) { }
        public bool isOpen() { return true; }
        public void Flush() { }
        public void HalfClose() { }
        public void HalfOpen(String name) { }

        public v8file get_first_file() { return null; }
        public void first_file(v8file value) { }

        public v8file get_last_file() { return null; }
        public void last_file(v8file value) { }

        #endregion

        #region private

        private v8file file;  // файл, которым является каталог. Для корневого каталога NULL
        public Stream data; // поток каталога. Если file не NULL (каталог не корневой), совпадает с file->data
        private Stream cfu;  // поток файла cfu. Существует только при is_cfu == true
        private void initialize() { }
        //private v8file first; // первый файл в каталоге
        public v8file first; // первый файл в каталоге
        public v8file last;  // последний файл в каталоге
        //private SortedDictionary<String, v8file> files; // Соответствие имен и файлов
        public SortedDictionary<String, v8file> files; // Соответствие имен и файлов
        private Int64 start_empty; // начало первого пустого блока
        private int page_size;   // размер страницы по умолчанию
        private int version;     // версия
        private bool zipped;     // признак зазипованности файлов каталога
        private bool is_cfu;     // признак файла cfu (файл запакован deflate'ом)
        private bool iscatalog;
        private bool iscatalogdefined;

        public bool is_fatmodified;
        private bool is_emptymodified;
        private bool is_modified;

        public void free_block(int start) { }

        private int write_block(Stream block, int start, bool use_page_size, int len = -1) { return 0; }       // возвращает адрес начала блока
        private int write_datablock(Stream block, int start, bool _zipped = false, int len = -1) { return 0; } // возвращает адрес начала блока

        public MemoryTributary read_datablock(int start) { return null; }
        public Int64 get_nextblock(Int64 start) { return 0; }

        private bool is_destructed; // признак, что работает деструктор
        private bool flushed;       // признак, что происходит сброс
        private bool leave_data;    // признак, что не нужно удалять основной поток (data) при уничтожении объекта

        #endregion
    }
}
