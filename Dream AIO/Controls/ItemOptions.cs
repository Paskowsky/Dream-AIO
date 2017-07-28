using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace Dream_AIO
{
    public partial class ItemOptions : UserControl
    {

        public string DropFolder { get { return comboBox1.Text; } set { comboBox1.Text = value; } }
        public string SubFolder { get { return textBox1.Text; } set { textBox1.Text = value; } }
        public string FileName { get { return textBox2.Text; } set { textBox2.Text = value; } }

        public bool Execute { get { return radioButton1.Checked; } set { radioButton1.Checked = value; } }

        internal DropOptions GetOptions()
        {
            DropOptions opt = new DropOptions();

            opt.DropFolder = DropFolder;
            opt.DirectoryName = SubFolder;
            opt.FileName = FileName;
            opt.Execute = Execute;

            return opt;
        }

        public ItemOptions()
        {
            InitializeComponent();
        }
    }
}
