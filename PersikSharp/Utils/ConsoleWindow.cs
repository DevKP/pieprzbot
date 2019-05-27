using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PersikSharp
{
    static class ConsoleWindow
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public const int SW_HIDE = 0;
        public const int SW_SHOW = 5;

        private static NotifyIcon trayIcon = new NotifyIcon();

        public static Task StartTrayAsync()
        {
            return Task.Run(() => StartTray());
        }
        public static void StartTray()
        {
            trayIcon.Text = Console.Title;
            trayIcon.Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);

            ContextMenu trayMenu = new ContextMenu();

            trayMenu.MenuItems.Add("Hide", Min_Click);
            trayMenu.MenuItems.Add("Show", Max_Click);
            trayMenu.MenuItems.Add("-");
            trayMenu.MenuItems.Add("Close", Close_Click);

            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible = true;

            Application.Run();
        }

        private static void Min_Click(object sender, EventArgs e)
        {
            ShowWindow(GetConsoleWindow(), SW_HIDE);
        }
        private static void Max_Click(object sender, EventArgs e)
        {
            ShowWindow(GetConsoleWindow(), SW_SHOW);
        }
        private static void Close_Click(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            Environment.Exit(0);
        }
    }
}
