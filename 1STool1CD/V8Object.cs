using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace _1STool1CD
{
    public struct _version_rec
    {
        public UInt32 version_1; // версия реструктуризации
        public UInt32 version_2; // версия изменения
    }

    public struct _version
    {
        public UInt32 version_1; // версия реструктуризации
        public UInt32 version_2; // версия изменения
        public UInt32 version_3; // версия изменения 2
    }

    // Структура страницы размещения уровня 1 версий от 8.3.8
    public struct objtab838
    {
        public UInt32[] blocks; // реальное количество блоков зависит от размера страницы (pagesize)
    }

    // структура заголовочной страницы файла данных или файла свободных страниц
    public struct v8ob
    {
        public char[] sig; // сигнатура SIG_OBJ
        public UInt32 len; // длина файла
        public _version version;
        public UInt32[] blocks;
    }

    // структура заголовочной страницы файла данных начиная с версии 8.3.8
    struct v838ob_data
    {
        public char[] sig;       // сигнатура 0x1C 0xFD (1C File Data?)
        public Int16 fatlevel;   // уровень таблицы размещения (0x0000 - в таблице blocks номера страниц с данными, 0x0001 - в таблице blocks номера страниц с таблицами размещения второго уровня, в которых уже, в свою очередь, находятся номера страниц с данными)
        public _version version;
        public UInt64 len;       // длина файла
        public UInt32[] blocks;  // Реальная длина массива зависит от размера страницы и равна pagesize/4-6 (от это 1018 для 4К до 16378 для 64К)
    }

    // структура заголовочной страницы файла свободных страниц начиная с версии 8.3.8
    public struct v838ob_free
    {
        public char[] sig;     // сигнатура 0x1C 0xFF (1C File Free?)
        public Int16 fatlevel; // 0x0000 пока! но может ... уровень таблицы размещения (0x0000 - в таблице blocks номера страниц с данными, 0x0001 - в таблице blocks номера страниц с таблицами размещения второго уровня, в которых уже, в свою очередь, находятся номера страниц с данными)
        public UInt32 version;        // ??? предположительно...
        public UInt32[] blocks;       // Реальная длина массива зависит от размера страницы и равна pagesize/4-6 (от это 1018 для 4К до 16378 для 64К)
    }

    // типы внутренних файлов
    // типы внутренних файлов
    public enum v8objtype
    {
	    unknown = 0, // тип неизвестен
	    data80  = 1, // файл данных формата 8.0 (до 8.2.14 включительно)
	    free80  = 2, // файл свободных страниц формата 8.0 (до 8.2.14 включительно)
	    data838 = 3, // файл данных формата 8.3.8
	    free838 = 4  // файл свободных страниц формата 8.3.8
    }


    public class v8object
    {
        /// <summary>
        /// Конструктор существующего объекта
        /// </summary>
        /// <param name="_base"></param>
        /// <param name="blockNum"></param>
        public v8object(T_1CD _base, UInt32 blockNum) 
        { }

        /// <summary>
        /// Конструктор нового (еще не существующего) объекта
        /// </summary>
        /// <param name="_base"></param>
        public v8object(T_1CD _base) 
        { }

        #region Public

        public char getdata() { return '0'; } // чтение всего объекта целиком, поддерживает кеширование объектов. Буфер принадлежит объекту
        public char getdata(byte[] buf, UInt64 _start, UInt64 _length) { return '0'; } // чтение кусочка объекта, поддерживает кеширование блоков. Буфер не принадлежит объекту
        public bool setdata(byte[] buf, UInt64 _start, UInt64 _length) { return true; } // запись кусочка объекта, поддерживает кеширование блоков.
        public bool setdata(byte[] buf, UInt64 _length) { return true; } // запись объекта целиком, поддерживает кеширование блоков.
        public bool setdata(Stream stream) { return true; } // записывает поток целиком в объект, поддерживает кеширование блоков.
        public bool setdata(Stream stream, UInt64 _start, UInt64 _length) { return true; } // запись части потока в объект, поддерживает кеширование блоков.
        public UInt64 getlen() { return 0; }
        public void savetofile(String filename) { }
        public void set_lockinmemory(bool _lock) { }
        public static void garbage() { }
        public UInt64 get_fileoffset(UInt64 offset) { return 0; } // получить физическое смещение в файле по смещению в объекте
        public void set_block_as_free(UInt32 block_number) { } // пометить блок как свободный
        public UInt32 get_free_block() { return 0; } // получить номер свободного блока (и пометить как занятый)
        public void get_version_rec_and_increase(_version ver) {  } // получает версию очередной записи и увеличивает сохраненную версию объекта
        public void get_version(_version ver) {  } // получает сохраненную версию объекта
        public void write_new_version() { } // записывает новую версию объекта
        public static v8object get_first() { return (v8object)null; }
        public static v8object get_last() { return (v8object)null; }
        public v8object get_next() { return (v8object)null; }
        public UInt32 get_block_number() { return 0; }
        public Stream readBlob(Stream _str, UInt32 _startblock, UInt32 _length = UInt32.MaxValue, bool rewrite = true) { return (Stream)null; }


        #endregion

        #region Private

        private T_1CD base_;

    	private UInt64 len;                 // длина объекта. Для типа таблицы свободных страниц - количество свободных блоков
        private _version version;           // текущая версия объекта
        private _version_rec version_rec;   // текущая версия записи
        private bool new_version_recorded;  // признак, что новая версия объекта записана
        private v8objtype type;             // тип и формат файла
        private Int32 fatlevel;             // Количество промежуточных уровней в таблице размещения
        private UInt64 numblocks;           // кол-во страниц в корневой таблице размещения объекта
        private UInt32 real_numblocks;      // реальное кол-во страниц в корневой таблице (только для файлов свободных страниц, может быть больше numblocks)
        private UInt32[] blocks;            // таблица страниц корневой таблицы размещения объекта (т.е. уровня 0)
        private UInt32 block;               // номер блока объекта
        private char[] data;                // данные, представляемые объектом, NULL если не прочитаны или len = 0
        
        private static v8object first;
        private static v8object last;
        private v8object next;
        private v8object prev;
        private UInt32 lastdataget;         // время (Windows time, в миллисекундах) последнего обращения к данным объекта (data)
        private bool lockinmemory;

        private void set_len(UInt64 _len) { } // установка новой длины объекта

        private void init() { }
        private void init(T_1CD _base, Int32 blockNum) { }


        #endregion

    }
}
