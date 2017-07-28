using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Dream_AIO
{
    public partial class BinderDialog : Form
    {
        public string FileName { get { return textBox1.Text; } set { textBox1.Text = value; } }


        public string DropFolder { get { return itemOptions1.DropFolder; } set { itemOptions1.DropFolder = value; } }
        public string SubFolder { get { return itemOptions1.SubFolder; } set { itemOptions1.SubFolder = value; } }

        //{ get { return textBox1.Text; } set { textBox1.Text = value; } }
        public string DropFileName { get { return itemOptions1.FileName; } set { itemOptions1.FileName = value; } }

        //{ get { return textBox2.Text; } set { textBox2.Text = value; } }

        public bool Execute { get { return itemOptions1.Execute; } set { itemOptions1.Execute = value; } }

        internal BinderSetting GetSettings()
        {
            BinderSetting settings = new BinderSetting();

            settings.Options = itemOptions1.GetOptions();

            settings.FileName = FileName;

            return settings;
        }

        public BinderDialog()
        {
            InitializeComponent();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    FileName = ofd.FileName;
                    itemOptions1.FileName = Path.GetFileName(FileName);
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(DropFileName))
            {
                MessageBox.Show("Empty file name", "error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            DialogResult = DialogResult.OK;
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
