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

        [ContextProperty("ВерсияФорматаФайла", "Version1CD")]
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

        [ContextMethod("УстановитьИмяЛогФайла", "SetLogFile")]
        public String SetLogFile(String FileName)
        {
            return "";
        }

        [ContextMethod("ОткрытьНеМонопольно", "SetNotExclusively")]
        public String SetNotExclusively(String FileName)
        {
            return "";
        }

        [ContextMethod("ЭкспортироватьВсеТаблицы", "ExportAllToXML")]
        public String ExportAllToXML(String FileName)
        {
            return "";
        }

        /// <summary>
        /// Экспортировать по указанному пути указанные таблицы в XML. 
        /// В списке через запятую, точку с запятой или пробел указывается список имен экспортируемых таблиц. 
        /// Можно использовать знаки подстановки * и ? 
        /// Если в списке содержатся пробелы, список необходимо заключать в кавычки.
        /// </summary>
        /// <param name="FileName"></param>
        /// <returns></returns>
        [ContextMethod("ЭкспортироватьТаблицу", "ExportToXML")]
        public String ExportToXML(String FileName)
        {
            return "";
        }

        /// <summary>
        /// Выгрузить основную конфигурацию информационной базы по указанному пути
        /// </summary>
        /// <param name="FileName"></param>
        /// <returns></returns>
        [ContextMethod("ВыгрузитьОсновнуюКонфигурацию", "DumpConfig")]
        public String DumpConfig(String FileName)
        {
            return "";
        }

        /// <summary>
        /// Выгрузить конфигурацию базы данных по указанному пути
        /// </summary>
        /// <param name="FileName"></param>
        /// <returns></returns>
        [ContextMethod("ВыгрузитьКонфигурацию", "DumpDBConfig")]
        public String DumpDBConfig(String FileName)
        {
            return "";
        }

        /// <summary>
        /// Выгрузить конфигурации поставщиков информационной базы по указанному пути
        /// </summary>
        /// <param name="FileName"></param>
        /// <returns></returns>
        [ContextMethod("ВыгрузитьКонфигурацииПоставщиков", "DumpVendorsConfigs")]
        public String DumpVendorsConfigs(String FileName)
        {
            return "";
        }

        /// <summary>
        /// Выгрузить все конфигурации информационной базы по указанному пути
        /// </summary>
        /// <param name="FileName"></param>
        /// <returns></returns>
        [ContextMethod("ВыгрузитьВсеКонфигурации", "DumpAllConfigs")]
        public String DumpAllConfigs(String FileName)
        {
            return "";
        }

        /// <summary>
        /// Выгрузить конфигурацию хранилища заданной версии по указанному пути. 
        /// Номер версии - это целое число. 1, 2, 3 и т.д. - выгрузить конфигурацию указанной версии, 
        /// 0 - выгрузить последнюю версию, -1 - предпоследнюю и т.д.
        /// </summary>
        /// <param name="FileName"></param>
        /// <returns></returns>
        [ContextMethod("ВыгрузитьКонфигурациюХранилища", "DumpDepotConfig")]
        public String DumpDepotConfig(String FileName, Int32 Ver)
        {
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
