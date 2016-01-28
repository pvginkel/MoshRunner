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
            MoshSession.Start(this, _host.Text, _user.Text);

            Close();
        }
    }
}
