using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Voxmat
{
    public partial class SettingsForm : Form
    {
        public Form1 MainForm;
        public SettingsForm(Form1 mainForm)
        {
            MainForm = mainForm;
            InitializeComponent();
        }
    }
}
