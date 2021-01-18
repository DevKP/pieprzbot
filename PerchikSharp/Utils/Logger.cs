using log4net;
using log4net.Repository;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PerchikSharp
{
    public enum LogType
    {
        Debug = 0,
        Info = 1,
        Error = 2,
        Fatal = 3
    }

    internal class Logger
    {
        private static Logger _instance;
        private static log4net.ILog _log;

        public static Logger Inst()
        {
            _instance ??= new Logger();
            return _instance;
        }
        public static void Log(LogType ltype, string text)
        {
            if(_log == null)
            {
                try
                {
                    var log4NetConfig = new XmlDocument();
                    using(var stm = new FileStream("./Configs/log4net.config", FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        log4NetConfig.Load(stm);
                    }
                    

                    ILoggerRepository repo = log4net.LogManager.CreateRepository(
                    Assembly.GetEntryAssembly(), typeof(log4net.Repository.Hierarchy.Hierarchy));

                    log4net.Config.XmlConfigurator.Configure(repo, log4NetConfig["log4net"]);

                    _log = log4net.LogManager.GetLogger(repo.Name, "CHAT");
                }catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.ReadLine();
                    Environment.Exit(1);
                }
            }

            switch (ltype)
            {
                case LogType.Debug:
                    _log.Debug(text);
                    break;
                case LogType.Info:
                    _log.Info(text);
                    break;
                case LogType.Error:
                    _log.Error(text);
                    break;
                case LogType.Fatal:
                    _log.Fatal(text);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ltype), ltype, null);
            }
        }
    }
}
