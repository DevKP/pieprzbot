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
        static log4net.ILog log;

        public static Logger Inst()
        {
            instance = instance ?? new Logger();
            return instance;
        }
        public static void Log(LogType ltype, string text)
        {
            if(log == null)
            {
                try
                {
                    XmlDocument log4netConfig = new XmlDocument();
                    log4netConfig.Load(File.OpenRead("./Configs/log4net.config"));

                    ILoggerRepository repo = log4net.LogManager.CreateRepository(
                    Assembly.GetEntryAssembly(), typeof(log4net.Repository.Hierarchy.Hierarchy));

                    log4net.Config.XmlConfigurator.Configure(repo, log4netConfig["log4net"]);

                    log = log4net.LogManager.GetLogger(repo.Name, "CHAT");
                }catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.ReadLine();
                    Environment.Exit(1);
                }
            }

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
            }
        }
    }
}
