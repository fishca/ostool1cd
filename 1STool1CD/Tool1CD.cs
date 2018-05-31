using System;
using ScriptEngine.Machine.Contexts;
using ScriptEngine.Machine;
using ScriptEngine.HostedScript.Library; // только если подключили OneScript Main Client Libraries

//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
using System.IO;

namespace _1STool1CD
{
    /// <summary>
    /// Класс для работы с базой .1CD
    /// </summary>
    [ContextClass("Утилита1CD", "Tool1CD")]
    public class Tool1CD : AutoContext<Tool1CD>
    {
        private String Version; // Версия формата открытого файла
        private Int32 PageSize; // Размер страницы открытого файла
        private String Data1CD; // Полный путь к файлу базы данных
        

        public Tool1CD(IValue F1CD)
        {
            Data1CD = F1CD.ToString();
            try
            {
                Tools1CD Data1C = new Tools1CD(Data1CD);
                Version = Data1C.Version.ToString();
            }
            finally
            {

            }

        }

        #region 123
        /// <summary>
        /// Версия базы 1CD.
        /// </summary>
        [ContextProperty("Версия1CD", "ReadonlyProperty")]
        public String ReadonlyProperty
        {
            get
            {
                return "0.0.0.1";
            }
        }

        [ContextProperty("ВерсияФормата1CD", "Version1CD")]
        public String Version1CD
        {
            /*
            set
            {
                Version = value;
            }
            */
            get
            {
                return Version;
            }
        }


        [ContextProperty("Размер", "PSize")]
        public String PSize
        {
            get
            {
                return "_";
            }
        }


        [ContextProperty("РазмерСтраницы", "PageSize1CD")]
        public Int32 PageSize1CD
        {
            set
            {
                PageSize = value;
            }
            get
            {
                return PageSize;
            }
        }

        [ContextMethod("ПолучитьТаблицу", "GetTable")]
        public String GetTable(String tbl)
        {
            if (tbl == "config")
                return "Таблица № 1 Configs";
            if (tbl == "users")
                return "Таблица № 2 users";
            else
                return "";
        }

        #endregion
        /// <summary>
        /// Некоторый конструктор
        /// </summary>
        /// <returns></returns>
        [ScriptConstructor]
        public static IRuntimeContextInstance Constructor(IValue fName)
        {
            return new Tool1CD(fName);
        }
    }
}
