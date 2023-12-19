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
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string email    = txtEmail.Text;
            string password = txtPassword.Text;
    
            if(email == "" || password == "")
            {
                MessageBox.Show("Wrong input data");
                return;
            }

            if(!MainForm.ExistsUser(email, password))
            {
                MessageBox.Show("Wrong credentials");
                return;
            }

            txtEmail.Text = "";
            txtPassword.Text = "";

            var frm = new MainForm(email);
            frm.ShowDialog();
            this.Close();
        }
    }
}