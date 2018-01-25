using System;
using ScriptEngine.Machine.Contexts;
using ScriptEngine.Machine;
using ScriptEngine.HostedScript.Library; // только если подключили OneScript Main Client Libraries

//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

namespace _1STool1CD
{
    /// <summary>
    /// Класс для работы с базой .1CD
    /// </summary>
    [ContextClass("Утилита1CD", "Tool1CD")]
    public class Tool1CD : AutoContext<Tool1CD>
    {
        public Tool1CD()
        {
        }

        /// <summary>
        /// Версия базы 1CD.
        /// </summary>
        [ContextProperty("СвойствоДляЧтения", "ReadonlyProperty")]
        public string ReadonlyProperty
        {
            get
            {
                return "0.0.0.1";
            }
        }

        /// <summary>
        /// Некоторый конструктор
        /// </summary>
        /// <returns></returns>
        [ScriptConstructor]
        public static IRuntimeContextInstance Constructor()
        {
            return new Tool1CD();
        }


    }
}
