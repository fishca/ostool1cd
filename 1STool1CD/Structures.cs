using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _1STool1CD
{
    public static class Structures
    {
        #region Структуры
        public struct IndexRecord
        {
            private V8Field field;
            private Int32 len;

            public V8Field Field { get { return field; } set { field = value; } }

            public int Len { get { return len; } set { len = value; } }
        }

        public struct UnpackIndexRecord
        {
            UInt32 _record_number; // номер (индекс) записи в таблице записей
                                   //unsigned char _index[1]; // значение индекса записи. Реальная длина значения определяется полем length класса index
            private byte[] index;

            public byte[] Index { get { return index; } set { index = value; } }
        }

        /// <summary>
        /// Структура заголовка
        /// </summary>
        public struct Fat_item
        {
            private UInt32 header_start;
            private UInt32 data_start;
            private UInt32 ff;            // всегда 7fffffff

            public uint Header_start { get { return header_start; } set { header_start = value; } }
            public uint Data_start { get { return data_start; } set { data_start = value; } }
            public uint Ff { get { return ff; } set { ff = value; } }
        }

        #endregion

        #region Перечисления

        /// <summary>
        /// Версии формата базы 1С
        /// </summary>
        public enum DBVer
        {
            ver8_0_3_0 = 1,
            ver8_0_5_0 = 2,
            ver8_1_0_0 = 3,
            ver8_2_0_0 = 4,
            ver8_2_14_0 = 5,
            ver8_3_8_0 = 6
        }

        public enum NodeType
        {
            nd_empty = 0,   // пусто
            nd_string = 1,  // строка
            nd_number = 2,  // число
            nd_number_exp = 3,  // число с показателем степени
            nd_guid = 4,    // уникальный идентификатор
            nd_list = 5,    // список
            nd_binary = 6,  // двоичные данные (с префиксом #base64:)
            nd_binary2 = 7, // двоичные данные формата 8.2 (без префикса)
            nd_link = 8,    // ссылка
            nd_binary_d = 9,    // двоичные данные (с префиксом #data:)
            nd_unknown          // неизвестный тип
        }

        #endregion

    }
}
