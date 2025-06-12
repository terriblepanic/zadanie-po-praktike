using System;
using System.Windows.Forms;

namespace AutoRepairWorkshop
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            ShowScreen(new UcPersonnel());
        }

        private void ShowScreen(UserControl control)
        {
            mainPanel.Controls.Clear();
            control.Dock = DockStyle.Fill;
            mainPanel.Controls.Add(control);
        }

        private void личностиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowScreen(new UcPersonnel());
        }

        private void автомобилиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowScreen(new UcAvtomobili());
        }

        private void ремонтыToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowScreen(new UcRemont());
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }

        private void mainPanel_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
