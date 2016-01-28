using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Mosh
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            using (var key = Program.BaseKey)
            {
                _host.Text = key.GetValue("Host") as string;
                _user.Text = key.GetValue("User") as string;
            }

            UpdateEnabled();
        }

        private void _host_TextChanged(object sender, EventArgs e)
        {
            UpdateEnabled();
        }

        private void _user_TextChanged(object sender, EventArgs e)
        {
            UpdateEnabled();
        }

        private void UpdateEnabled()
        {
            _connect.Enabled = _host.Text.Length > 0 && _user.Text.Length > 0;
        }

        private void _cancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void _connect_Click(object sender, EventArgs e)
        {
            // Find the IP address of the host.

            string hostName = _host.Text;
            int? port = null;

            int pos = hostName.IndexOf(':');
            if (pos != -1)
            {
                int value;
                if (!int.TryParse(hostName.Substring(pos + 1), out value))
                {
                    MessageBox.Show(this, "Invalid host name", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                MessageBox.Show(this, "Cannot resolve host", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var moshKey = GetKey(hostName, port, null);

            while (moshKey == null)
            {
                string password;

                using (var form = new PasswordForm())
                {
                    if (form.ShowDialog(this) != DialogResult.OK)
                        return;

                    password = form.Password;
                }

                moshKey = GetKey(hostName, port, password);

                if (moshKey == null)
                    MessageBox.Show(this, "Invalid user name or password", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            using (var key = Program.BaseKey)
            {
                key.SetValue("Host", _host.Text);
                key.SetValue("User", _user.Text);
            }

            string basePath = Path.GetDirectoryName(GetType().Assembly.Location);
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

            Close();
        }

        private MoshKey GetKey(string hostName, int? port, string password)
        {
            // Ask for a key.

            string arguments = ShellEncode("-batch", "-ssh", "-t", "-l", _user.Text, hostName);
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

        private string ShellEncode(params string[] args)
        {
            return ShellEncode((IEnumerable<string>)args);
        }

        private string ShellEncode(IEnumerable<string> args)
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

        private string ShellEncode(string arg)
        {
            if (String.IsNullOrEmpty(arg))
                return "";

            return "\"" + arg.Replace("\"", "\"\"") + "\"";
        }
    }
}
