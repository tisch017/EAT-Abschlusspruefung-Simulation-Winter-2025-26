using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IHK_Model
{
    public partial class Form1: Form
    {
        public Form1()
        {
            InitializeComponent();
            NotHalt.Checked = true;
        }

        private void NotHalt_CheckedChanged(object sender, EventArgs e)
        {
            // Prüfen, ob die CheckBox aktiviert ist (entspricht "gedrückt")
            if (NotHalt.Checked)
            {
                // Zustand: NOT-HALT ist gedrückt / aktiv
                // Wir machen den Hintergrund dunkler und ändern den Text,
                // um zu signalisieren, dass er entriegelt werden muss.
                NotHalt.BackColor = System.Drawing.Color.DarkRed;
                NotHalt.ForeColor = System.Drawing.Color.White;
                NotHalt.Text = "ENTRIEGELN";
                F9.Checked = false;
                S12_P12.BackColor = Color.Blue;
            }
            else
            {
                // Zustand: NOT-HALT ist nicht gedrückt / normal
                // Wir setzen die ursprünglichen Farben und den Text wieder ein.
                NotHalt.BackColor = System.Drawing.Color.Red;
                NotHalt.ForeColor = System.Drawing.Color.Yellow;
                NotHalt.Text = "NOT-HALT";
                S12_P12.BackColor = Color.LightSkyBlue; //Blinke in Color.Blue einfügen!!
            }
        }

        private void S12_P12_Click(object sender, EventArgs e)
        {
            if (!NotHalt.Checked && !F9.Checked)
            {
                F9.Checked = true;
                S12_P12.BackColor = Color.LightSkyBlue;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
