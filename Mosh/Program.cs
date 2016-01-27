using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Mosh
{
    static class Program
    {
        public static RegistryKey BaseKey
        {
            get { return Registry.CurrentUser.CreateSubKey("Software\\Mosh Runner"); }
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
