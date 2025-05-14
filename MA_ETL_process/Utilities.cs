using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace MA_ETL_process
{
    static class Utilities
    {
        private static TextBox? txtConsole;
        public static TextBox? SetTxtConsoleReference { set { txtConsole = value; } }

        public static void ConsoleLog(string? message)
        {
            if (txtConsole != null)
            {
                // call the dispatcher
                Application.Current.Dispatcher.Invoke(
                    DispatcherPriority.Render,
                    () => {
                        // do stuff to show up in the UI
                        txtConsole.Text += message + "\n";
                        txtConsole.ScrollToEnd();
                });
            }
        }
    }
}
