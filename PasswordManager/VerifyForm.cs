using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PasswordManager
{
    public partial class VerifyForm : Form
    {
        private readonly string email;
        private bool verified = false;

        public bool Verified { get { return verified; } }

        public VerifyForm(string userEmail)
        {
            email = userEmail;
            InitializeComponent();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            string password = txtPassword.Text;
            if (password == "")
            {
                MessageBox.Show("Wrong input data");
                return;
            }

            if (!MainForm.ExistsUser(email, password))
            {
                MessageBox.Show("Wrong credentials");
                return;
            }

            txtPassword.Text = "";
            verified = true;
            this.Close();
        }
    }
}
