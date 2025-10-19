using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace dev0RotStrings
{
    public partial class Form1 : Form
    {
        private readonly string[] _args;

        public Form1(string[] args)
        {
            _args = args ?? Array.Empty<string>();
            InitializeComponent();
            Text = "dev0RotStrings";
            ShowUrlAndCredit();
            SetupListView();
            if (_args.Length > 0 && File.Exists(_args[0]))
            {
                LoadFile(_args[0]);
            }
        }

        private void ShowUrlAndCredit()
        {
            linkLabelUrl.Text = "https://github.com/backdoor831246";
            linkLabelUrl.Links.Clear();
            linkLabelUrl.Links.Add(0, linkLabelUrl.Text.Length, linkLabelUrl.Text);
            linkLabelUrl.LinkClicked += (s, e) =>
            {
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Link.LinkData?.ToString() ?? e.Link.ToString()) { UseShellExecute = true }); }
                catch { /* fail silently */ }
            };
            labelCredit.Text = "developed by dev0Rot";
        }

        private void SetupListView()
        {
            listViewStrings.View = View.Details;
            listViewStrings.FullRowSelect = true;
            listViewStrings.Columns.Clear();
            listViewStrings.Columns.Add("Offset", 90);
            listViewStrings.Columns.Add("Type", 80);
            listViewStrings.Columns.Add("String", 700);
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog { Filter = "All files (*.*)|*.*", Title = "Open file to extract strings" };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                LoadFile(ofd.FileName);
            }
        }

        private void LoadFile(string path)
        {
            try
            {
                toolStripStatusLabelFile.Text = path;
                listViewStrings.Items.Clear();
                var bytes = File.ReadAllBytes(path);
                var ascii = ExtractAsciiStrings(bytes, 4);
                var uni = ExtractUtf16LeStrings(bytes, 4);

                foreach (var s in ascii)
                {
                    var item = new ListViewItem($"0x{s.Offset:X8}");
                    item.SubItems.Add("ASCII");
                    item.SubItems.Add(s.Value);
                    listViewStrings.Items.Add(item);
                }

                foreach (var s in uni)
                {
                    var item = new ListViewItem($"0x{s.Offset:X8}");
                    item.SubItems.Add("UTF-16LE");
                    item.SubItems.Add(s.Value);
                    listViewStrings.Items.Add(item);
                }

                toolStripStatusLabelCount.Text = $"{listViewStrings.Items.Count} strings";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке файла:\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private IEnumerable<(int Offset, string Value)> ExtractAsciiStrings(byte[] data, int minLen)
        {
            var results = new List<(int, string)>();
            var sb = new StringBuilder();
            int start = -1;
            for (int i = 0; i < data.Length; i++)
            {
                byte b = data[i];
                if (b >= 0x20 && b <= 0x7E) // printable ASCII
                {
                    if (start < 0) start = i;
                    sb.Append((char)b);
                }
                else
                {
                    if (start >= 0 && sb.Length >= minLen)
                    {
                        results.Add((start, sb.ToString()));
                    }
                    sb.Clear();
                    start = -1;
                }
            }
            if (start >= 0 && sb.Length >= minLen)
                results.Add((start, sb.ToString()));
            return results;
        }

        private IEnumerable<(int Offset, string Value)> ExtractUtf16LeStrings(byte[] data, int minLen)
        {
            var results = new List<(int, string)>();
            int i = 0;
            while (i + 1 < data.Length)
            {
                int start = i;
                var sb = new StringBuilder();
                while (i + 1 < data.Length)
                {
                    ushort ch = (ushort)(data[i] | (data[i + 1] << 8));
                    if (ch >= 0x20 && ch <= 0x7E)
                    {
                        sb.Append((char)ch);
                        i += 2;
                    }
                    else
                    {
                        break;
                    }
                }
                if (sb.Length >= minLen)
                {
                    results.Add((start, sb.ToString()));
                }
                i = Math.Max(i + 2, start + 1);
            }
            return results;
        }

        private void txtFilter_TextChanged(object sender, EventArgs e)
        {
            string filter = txtFilter.Text.ToLower();
            listViewStrings.BeginUpdate();
            foreach (ListViewItem item in listViewStrings.Items)
            {
                if (string.IsNullOrEmpty(filter) || item.SubItems[2].Text.ToLower().Contains(filter))
                    item.ForeColor = System.Drawing.SystemColors.ControlText;
                else
                    item.ForeColor = System.Drawing.Color.Gray;
            }
            listViewStrings.EndUpdate();
        }


        private void btnCopy_Click(object sender, EventArgs e)
        {
            if (listViewStrings.SelectedItems.Count == 0) return;
            var sb = new StringBuilder();
            foreach (ListViewItem it in listViewStrings.SelectedItems)
            {
                sb.AppendLine($"{it.SubItems[0].Text}\t{it.SubItems[1].Text}\t{it.SubItems[2].Text}");
            }
            Clipboard.SetText(sb.ToString());
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (listViewStrings.Items.Count == 0) return;
            using var sfd = new SaveFileDialog { Filter = "Text file|*.txt", FileName = "strings.txt" };
            if (sfd.ShowDialog() != DialogResult.OK) return;
            using var sw = new StreamWriter(sfd.FileName, false, Encoding.UTF8);
            foreach (ListViewItem it in listViewStrings.Items)
            {
                sw.WriteLine($"{it.SubItems[0].Text}\t{it.SubItems[1].Text}\t{it.SubItems[2].Text}");
            }
            MessageBox.Show("Сохранено.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            if (File.Exists(toolStripStatusLabelFile.Text))
                LoadFile(toolStripStatusLabelFile.Text);
        }

        private void listViewStrings_DoubleClick(object sender, EventArgs e)
        {
            if (listViewStrings.SelectedItems.Count == 0) return;
            var text = listViewStrings.SelectedItems[0].SubItems[2].Text;
            MessageBox.Show(text, "String", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
