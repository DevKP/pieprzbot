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
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public const int SW_HIDE = 0;
        public const int SW_SHOW = 5;
        private static bool Visible = true;

        private static NotifyIcon trayIcon = new NotifyIcon();
        public static void ShowConsole()
        {
            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_SHOW);
            Visible = true;
        }
        public static void HideConsole()
        {
            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_HIDE);
            Visible = false;
        }
        public static Task StartTrayAsync()
        {
            return Task.Run(() => StartTray());
        }
        public static void StartTray(bool hidden = false)
        {
            trayIcon.Text = Console.Title;
            trayIcon.Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
            trayIcon.MouseDoubleClick += TrayIcon_MouseDoubleClick;

            ContextMenu trayMenu = new ContextMenu();

            trayMenu.MenuItems.Add("Show", Min_Click);
            trayMenu.MenuItems.Add("-");
            trayMenu.MenuItems.Add("Close", Close_Click);

            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible = true;

            Application.Run();
        }

        private static void TrayIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            trayIcon.ContextMenu.MenuItems[0].PerformClick();
        }

        private static void Min_Click(object sender, EventArgs e)
        {
            if (Visible == true)
            {
                HideConsole();
                (sender as MenuItem).Text = "Show";
            }
            else
            {
                ShowConsole();
                (sender as MenuItem).Text = "Hide";
            }
        }
        private static void Close_Click(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            Environment.Exit(0);
        }
    }
}
