using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

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
                txtConsole.Text += message + "\n";
                txtConsole.ScrollToEnd();
            }
        }
    }
}
