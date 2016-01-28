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
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length == 1)
            {
                int pos = args[0].IndexOf('@');
                if (pos != -1)
                {
                    string userName = args[0].Substring(0, pos);
                    string hostName = args[0].Substring(pos + 1);

                    MoshSession.Start(null, hostName, userName);
                    return;
                }
            }

            Application.Run(new MainForm());
        }
    }
}
