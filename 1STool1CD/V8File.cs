using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace _1STool1CD
{
    class V8File
    {
        enum FileIsCatalog { unknown, yes, no }
        
        public V8File() // конструктор
        { }

        public string name;  // имя файла GUID

        public Int64 time_create;       // дата/время создания
        public Int64 time_modify;       // дата/время модификации

        FileIsCatalog iscatalog;

        public bool is_opened;         // признак открытого файла (инициализирован поток data)

        public int start_data;         // начало блока данных файла в каталоге (0 означает, что файл в каталоге не записан)
        public int start_header;       // начало блока заголовка файла в каталоге

        public bool is_datamodified;   // признак модифицированности данных файла (требуется запись в каталог при закрытии)
        public bool is_headermodified; // признак модифицированности заголовка файла (требуется запись в каталог при закрытии)
        public bool is_destructed;     // признак, что работает деструктор
        public bool flushed;           // признак, что происходит сброс
        public bool selfzipped;        // Признак, что файл является запакованным независимо от признака zipped каталога



        /// <summary>
        /// Определение каталога
        /// </summary>
        /// <returns></returns>
        public bool IsCatalog()
        {
            return true;
        }

        /// <summary>
        /// Определение "длину" каталога
        /// </summary>
        /// <returns></returns>
        public Int64 GetFileLength()
        {
            return 100;
        }

        public string GetFileName()
        {
            return name;
        }

        public string GetFullName()
        {
            return "";
        }

        public void SetFilName(string _name)
        {
            name = _name;
        }

        public bool Try_open()
        {
            // condition ? first_expression : second_expression;
            return is_opened ? true : Open();
        }

        public bool Open()
        {
            return true;
        }
        public void Close()
        {

        }
        public void Flush()
        {

        }

        public void DeleteFile()
        {
        }


    }
}
