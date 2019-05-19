using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace PersikSharp
{
    public class CommandLineEventArgs : EventArgs
    {
        public CommandLineEventArgs(string t) { Text = t; }
        public string Text { get; }
    }

    class CommandLine
    {
        private static CommandLine instance;

        private string text;
        public static string Text
        {
            get
            {
                return Inst().text;
            }
            set
            {
                Inst().text = value;
                lock (CommandLine.Inst())
                {
                    Inst().Draw();
                }
            }
        }


        public event EventHandler<CommandLineEventArgs> onSubmitAction;

        private int last_cursor_top;
        private static CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
        private static CancellationToken cancel_token = cancelTokenSource.Token;

        public static CommandLine Inst()
        {
            if (instance == null)
                instance = new CommandLine();
            return instance;
        }

        public CommandLine()
        {
            last_cursor_top = Console.WindowHeight;
            text = "";

            Console.WriteLine();
            this.Draw();
        }

        public void StartUpdating()
        {
            Thread loop = new Thread(update_loop);
            loop.Start();
        }

        public void StopUpdating()
        {
            cancelTokenSource.Cancel();
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
            while (!cancel_token.IsCancellationRequested)
            {
                var keysHit = GetInput();
                foreach (var key in keysHit)
                {
                    switch (key.Key)
                    {
                        case ConsoleKey.End:
                            break;
                        case ConsoleKey.Backspace:
                            if(Text.Length > 0)
                                Text = Text.Remove(Text.Length - 1, 1);
                            break;
                        case ConsoleKey.Enter:
                            onSubmitAction?.Invoke(this, new CommandLineEventArgs(text));
                            break;
                        default:
                            if(Text.Length < Console.WindowWidth - 3)
                                Text += key.KeyChar;
                            break;
                    }
                    
                }
                Thread.Sleep(5);
            }
        }

        private void clear_line()
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
            clear_line();

            int temp_cursor_top = Console.CursorTop;

            if (Console.CursorTop < Console.WindowHeight - 2)
                Console.SetCursorPosition(0, Console.WindowHeight);
            else
                Console.SetCursorPosition(0, Console.CursorTop + 2);

            Console.BackgroundColor = ConsoleColor.Gray;
            Console.ForegroundColor = ConsoleColor.Black;

            Console.Write("> " + text);

            StringBuilder filler = new StringBuilder(Console.WindowWidth);
            if (Console.WindowWidth - text.Length - 3 > 0)
            {
                for (int i = 0; i < Console.WindowWidth - text.Length - 3; i++)
                    filler.Append(' ');
                filler[0] = '▄';
            }
            Console.Write(filler);

            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;

            this.last_cursor_top = Console.CursorTop;

            Console.SetCursorPosition(0, temp_cursor_top);
        }
    }
}
