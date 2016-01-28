using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Mosh
{
    internal static class MoshSession
    {
        public static void Start(IWin32Window owner, string fullHostName, string userName)
        {
            // Find the IP address of the host.

            string hostName = fullHostName;
            int? port = null;

            int pos = hostName.IndexOf(':');
            if (pos != -1)
            {
                int value;
                if (!int.TryParse(hostName.Substring(pos + 1), out value))
                {
                    MessageBox.Show(owner, "Invalid host name", "Mosh", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                port = value;
                hostName = hostName.Substring(0, pos);
            }


            var hosts = Dns.GetHostAddresses(hostName);
            IPAddress host = null;

            foreach (var item in hosts)
            {
                if (item.AddressFamily == AddressFamily.InterNetwork)
                {
                    host = item;
                    break;
                }
            }

            if (host == null)
            {
                MessageBox.Show(owner, "Cannot resolve host", "Mosh", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var moshKey = GetKey(hostName, userName, port, null);

            while (moshKey == null)
            {
                string password;

                using (var form = new PasswordForm())
                {
                    if (owner == null)
                        form.StartPosition = FormStartPosition.CenterScreen;

                    if (form.ShowDialog(owner) != DialogResult.OK)
                        return;

                    password = form.Password;
                }

                moshKey = GetKey(hostName, userName, port, password);

                if (moshKey == null)
                    MessageBox.Show(owner, "Invalid user name or password", "Mosh", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            using (var key = Program.BaseKey)
            {
                key.SetValue("Host", fullHostName);
                key.SetValue("User", userName);
            }

            string basePath = Path.GetDirectoryName(typeof(MoshSession).Assembly.Location);
            string env = Path.Combine(Path.Combine(Path.Combine(basePath, "usr"), "bin"), "env.exe");
            string minTtyPath = Path.Combine(Path.Combine(Path.Combine(basePath, "usr"), "bin"), "mintty.exe");
            string config = Path.Combine(basePath, "config.ini");

            if (!File.Exists(config))
                File.WriteAllText(config, "");

            var mintty = new Process
            {
                StartInfo =
                {
                    UseShellExecute = false,
                    // WorkingDirectory = Path.GetDirectoryName(GetType().Assembly.Location),
                    FileName = minTtyPath,
                    // Arguments = String.Format("env MOSH_KEY=\"{0}\" MOSH_PREDICTION_DISPLAY=adaptive MOSH_NO_TERM_INIT=1 ./mosh-client {1} {2}", sessionKey, host, port)
                    Arguments = String.Format("-c config.ini -e \"{0}\" MOSH_KEY=\"{1}\" MOSH_PREDICTION_DISPLAY=adaptive MOSH_NO_TERM_INIT=1 usr/bin/mosh-client.exe {2} {3}", env, moshKey.Key, host, moshKey.Port)
                }
            };

            mintty.Start();
        }

        private static MoshKey GetKey(string hostName, string userName, int? port, string password)
        {
            // Ask for a key.

            string arguments = ShellEncode("-batch", "-ssh", "-t", "-l", userName, hostName);
            if (port.HasValue)
                arguments += " " + ShellEncode("-P", port.Value.ToString());
            if (password != null)
                arguments += " " + ShellEncode("-pw", password);
            arguments += " " + ShellEncode("mosh-server", "new", "-c", "256", "-s", "-l", "LANG=en_US.UTF-8");

            string output;

            using (var plink = new Process())
            {
                plink.StartInfo.CreateNoWindow = true;
                plink.StartInfo.UseShellExecute = false;
                plink.StartInfo.FileName = "plink.exe";
                plink.StartInfo.Arguments = arguments;
                plink.StartInfo.RedirectStandardInput = true;
                plink.StartInfo.RedirectStandardOutput = true;
                plink.StartInfo.RedirectStandardError = true;

                var sb = new StringBuilder();

                plink.OutputDataReceived += (s, ea) =>
                {
                    if (ea.Data != null)
                        sb.AppendLine(ea.Data);
                };
                plink.ErrorDataReceived += (s, ea) =>
                {
                    if (ea.Data != null)
                        sb.AppendLine(ea.Data);
                };

                plink.Start();
                plink.BeginOutputReadLine();
                plink.BeginErrorReadLine();
                plink.WaitForExit();

                output = sb.ToString();
            }

            var match = Regex.Match(output, "MOSH CONNECT (\\d+?) ([A-Za-z0-9/+]{22})\\s*");

            if (match.Success)
            {
                var moshPort = match.Groups[1].Value;
                var sessionKey = match.Groups[2].Value;

                return new MoshKey(moshPort, sessionKey);
            }

            return null;
        }

        private class MoshKey
        {
            public string Port { get; private set; }
            public string Key { get; private set; }

            public MoshKey(string port, string key)
            {
                Port = port;
                Key = key;
            }
        }

        private static string ShellEncode(params string[] args)
        {
            return ShellEncode((IEnumerable<string>)args);
        }

        private static string ShellEncode(IEnumerable<string> args)
        {
            if (args == null)
                return String.Empty;

            var sb = new StringBuilder();

            foreach (string arg in args)
            {
                if (sb.Length > 0)
                    sb.Append(" ");

                sb.Append(ShellEncode(arg));
            }

            return sb.ToString();
        }

        private static string ShellEncode(string arg)
        {
            if (String.IsNullOrEmpty(arg))
                return "";

            return "\"" + arg.Replace("\"", "\"\"") + "\"";
        }
    }
}
