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

            var hosts = Dns.GetHostAddresses(_host.Text);
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

            // Ask for a key.

            string output;

            using (var plink = new Process())
            {
                plink.StartInfo.CreateNoWindow = true;
                plink.StartInfo.UseShellExecute = false;
                plink.StartInfo.FileName = "plink.exe";
                plink.StartInfo.Arguments = String.Format("-batch -ssh -t -l \"{0}\" \"{1}\" mosh-server new -c 256 -s -l LANG=en_US.UTF-8", _user.Text, _host.Text);
                plink.StartInfo.RedirectStandardInput = true;
                plink.StartInfo.RedirectStandardOutput = true;
                plink.StartInfo.RedirectStandardError = true;

                var sb = new StringBuilder();

                plink.OutputDataReceived += (s, ea) => sb.AppendLine(ea.Data);
                plink.ErrorDataReceived += (s, ea) => sb.AppendLine(ea.Data);

                plink.Start();
                plink.BeginOutputReadLine();
                plink.BeginErrorReadLine();
                plink.WaitForExit();

                output = sb.ToString();
            }

            var match = Regex.Match(output, "MOSH CONNECT (\\d+?) ([A-Za-z0-9/+]{22})\\s*");

            if (!match.Success)
            {
                MessageBox.Show(this, "Cannot start session\r\n\r\n" + output, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var port = match.Groups[1].Value;
            var sessionKey = match.Groups[2].Value;

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
                    Arguments = String.Format("-c config.ini -e \"{0}\" MOSH_KEY=\"{1}\" MOSH_PREDICTION_DISPLAY=adaptive MOSH_NO_TERM_INIT=1 usr/bin/mosh-client.exe {2} {3}", env, sessionKey, host, port)
                }
            };

            mintty.Start();

            Close();
        }
    }
}
