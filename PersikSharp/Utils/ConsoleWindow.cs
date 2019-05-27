using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
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


        public static Task StartTrayAsync()
        {
            return Task.Run(() => StartTray());
        }
        public static void StartTray()
        {
            NotifyIcon trayIcon = new NotifyIcon();
            trayIcon.Text = Console.Title;
            trayIcon.Icon = new Icon(SystemIcons.Application, 40, 40);

            ContextMenu trayMenu = new ContextMenu();

            trayMenu.MenuItems.Add("Hide", Min_Click);
            trayMenu.MenuItems.Add("Show", Max_Click);

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
    }
}
