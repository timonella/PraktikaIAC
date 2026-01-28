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
    public partial class LoginForm : Form
    {
        Button btnLogin;
        Button btnCancel;
        TextBox txtEmail;
        TextBox txtPassword;

        public LoginForm()
        {
            txtEmail = new TextBox();
            txtEmail.Size = new Size(200, 100);
            txtEmail.Location = new Point(75, 25);
            txtEmail.Text = "Введите Email";
            txtEmail.Click += TxtEmail_Click;

            txtPassword = new TextBox();
            txtPassword.Size = new Size(200, 100);
            txtPassword.Location = new Point(75, 75);
            txtPassword.Text = "Введите пароль";
            txtPassword.Click += TxtPassword_Click;

            btnLogin = new Button();
            btnLogin.Size = new Size(125, 50);
            btnLogin.Location = new Point(75, 150);
            btnLogin.Text = "Войти";
            btnLogin.Click += BtnLogin_Click;

            btnCancel = new Button();
            btnCancel.Size = new Size(125, 50);
            btnCancel.Location = new Point(200, 150);
            btnCancel.Text = "Отмена";
            btnCancel.Click += BtnCancel_Click;

            this.Controls.Add(btnLogin);
            this.Controls.Add(btnCancel);
            this.Controls.Add(txtEmail);
            this.Controls.Add(txtPassword);

            this.Text = "Вход";
            this.Size = new Size(400, 300);
        }
        private void TxtPassword_Click(object sender, EventArgs e)
        {
            if (txtPassword.Text == "Введите пароль") txtPassword.Text = "";
            txtPassword.PasswordChar = '*';
        }
        private void TxtEmail_Click(object sender, EventArgs e)
        {
            if (txtEmail.Text == "Введите Email") txtEmail.Text = "";
        }
        private void BtnLogin_Click(object sender, EventArgs e)
        {
            //Логика входа
        }
        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
