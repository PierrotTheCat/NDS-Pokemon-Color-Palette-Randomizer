using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace NitroExplorer {
    public partial class frmAbout : Form {
        public frmAbout() {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e) {
            this.Hide();
            this.Dispose();
        }
    }
}
