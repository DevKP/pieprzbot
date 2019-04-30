using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PersikSharp
{
    class CommandLine
    {
        private static CommandLine instance;

        private object syncobject;
        public static object SyncObject
        {
            get
            {
                return Inst().syncobject;
            }
            set
            {
                Inst().syncobject = value;
            }
        }

        private string text_var;
        public static string Text
        {
            get
            {
                return Inst().text_var;
            }
            set
            {
                Inst().text_var = value;
                lock (CommandLine.Inst())
                {
                    Inst().Draw();
                }
            }
        }

        private event Action<object,string> onenteraction;

        public event Action<object, string> onSubmitAction
        {
            add
            {
                onenteraction += value;
            }
            remove
            {
                onenteraction -= value;
            }
        }

        private int last_cursor_top;
        private bool Running;

        public static CommandLine Inst()
        {
            if (instance == null)
                instance = new CommandLine();
            return instance;
        }

        public CommandLine()
        {
            last_cursor_top = Console.WindowHeight;
            text_var = "";
            Running = false;

            Console.WriteLine();
            this.Draw();
        }

        public void StartUpdating()
        {
            this.Running = true;
            Thread loop = new Thread(update_loop);
            loop.Start();
        }

        public void StopUpdating()
        {
            this.Running = false;
        }

        private IEnumerable<ConsoleKeyInfo> GetInput()
        {
            var input = new HashSet<ConsoleKeyInfo>();
            var keyCount = 0;
            while (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                if (++keyCount > 2) continue;
                input.Add(key);
            }
            return input;
        }

        private void update_loop()
        {
            while (Running)
            {
                var keysHit = GetInput();
                foreach (var key in keysHit)
                {
                    switch (key.Key)
                    {
                        case ConsoleKey.F1:
                            Logger.Log(LogType.Info, "F1 was pressed!!!");
                            break;
                        case ConsoleKey.End:
                            break;
                        case ConsoleKey.Backspace:
                            if(Text.Length > 0)
                                Text = Text.Remove(Text.Length - 1, 1);
                            break;
                        case ConsoleKey.Enter:
                            onenteraction?.Invoke(this, text_var);
                            break;
                        default:
                            Text = Text + key.KeyChar;
                            break;
                    }
                    
                }
                Thread.Sleep(5);
            }
        }

        private void clr_old()
        {
            int temp_cursor_top = Console.CursorTop;
            Console.SetCursorPosition(0, this.last_cursor_top);

            char[] blankstring = new char[Console.WindowWidth - 1]; //+1 for /0
            for(int i = 0; i < blankstring.Length;i++)
            {
                blankstring[i] = ' ';
            }

            Console.Write(blankstring);
            Console.SetCursorPosition(0, temp_cursor_top);
        }
        public void Draw()
        {
            clr_old();

            int temp_cursor_top = Console.CursorTop;

            if (Console.CursorTop < Console.WindowHeight - 2)
                Console.SetCursorPosition(0, Console.WindowHeight);
            else
                Console.SetCursorPosition(0, Console.CursorTop + 2);

            Console.BackgroundColor = ConsoleColor.Gray;
            Console.ForegroundColor = ConsoleColor.Black;

            Console.Write("> " + text_var);

            StringBuilder filler = new StringBuilder(' ', Console.WindowWidth - text_var.Length - 3);
            for (int i = 0; i < Console.WindowWidth - text_var.Length - 3; i++)
                filler.Append(' ') ;
            filler[0] = '▄';
            Console.Write(filler);

            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;

            this.last_cursor_top = Console.CursorTop;

            Console.SetCursorPosition(0, temp_cursor_top);
        }
    }
}
