using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EventSync_Field
{
    
    public partial class MainForm : Form
    {
        Button btnRegister;
        Button btnLogin;
        public MainForm()
        {
            this.Text = "EventSync Field";
            this.Size = new Size(400, 300);

            btnRegister = new Button();
            btnRegister.Size = new Size(100, 70);
            btnRegister.Location = new Point(100, 90);
            btnRegister.Text = "Регистрация";
            btnRegister.Click += BtnRegister_Click;

            btnLogin = new Button();
            btnLogin.Size = new Size(100, 70);
            btnLogin.Location = new Point(200, 90);
            btnLogin.Text = "Вход";
            btnLogin.Click += BtnLogin_Click;

            this.Controls.Add(btnRegister);
            this.Controls.Add(btnLogin);
        }
        private void BtnRegister_Click(object sender, EventArgs e)
        {
            RegistrationForm registrationForm = new RegistrationForm();
            registrationForm.ShowDialog();
        }
        private void BtnLogin_Click(object sender, EventArgs e)
        {
            LoginForm loginForm = new LoginForm();
            loginForm.ShowDialog();
        }
    }
}
