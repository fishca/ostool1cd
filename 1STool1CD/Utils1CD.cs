using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _1STool1CD
{
    public static class Utils1CD
    {
        /// <summary>
        /// Версии формата базы 1С
        /// </summary>
        public enum db_ver
        {
            ver8_0_3_0  = 1,
        	ver8_0_5_0  = 2,
	        ver8_1_0_0  = 3,
	        ver8_2_0_0  = 4,
	        ver8_2_14_0 = 5,
	        ver8_3_8_0  = 6
        }

        public enum node_type
        {
            nd_empty      = 0,	// пусто
	        nd_string     = 1,	// строка
	        nd_number     = 2,	// число
	        nd_number_exp = 3,	// число с показателем степени
	        nd_guid       = 4,	// уникальный идентификатор
	        nd_list       = 5,	// список
	        nd_binary     = 6,	// двоичные данные (с префиксом #base64:)
	        nd_binary2    = 7,	// двоичные данные формата 8.2 (без префикса)
	        nd_link       = 8,	// ссылка
	        nd_binary_d   = 9,	// двоичные данные (с префиксом #data:)
	        nd_unknown          // неизвестный тип
        }
                
        /// <summary>
        /// 0x7FFFFFFF - Обозначение последней страницы
        /// </summary>
        public static readonly Int32 LAST_PAGE = Int32.MaxValue; 
        


    }
}
