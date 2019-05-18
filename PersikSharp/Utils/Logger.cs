using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersikSharp
{
    public enum LogType
    {
        Debug = 0,
        Info = 1,
        Error = 2,
        Fatal = 3
    }

    class Logger
    {
        private static Logger instance;
        private static readonly ILog log = LogManager.GetLogger("CHAT");
        public static Logger Inst()
        {
            if (instance == null)
                instance = new Logger();
            return instance;
        }
        public static void Log(LogType ltype, string text)
        {
            lock (CommandLine.Inst())
            {
                switch (ltype)
                {
                    case LogType.Debug:
                        log.Debug(text);
                        break;
                    case LogType.Info:
                        log.Info(text);
                        break;
                    case LogType.Error:
                        log.Error(text);
                        break;
                    case LogType.Fatal:
                        log.Fatal(text);
                        break;
                }
                CommandLine.Inst().Draw();
            }
        }
    }
}
