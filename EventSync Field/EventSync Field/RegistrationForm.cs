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
    public partial class RegistrationForm : Form
    {
        TextBox txtEmail;
        TextBox txtPassword;
        TextBox txtFIO;
        ComboBox cmbOrganization;
        Button btnRegister;
        Button btnCancel;

        public RegistrationForm()
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

            txtFIO = new TextBox();
            txtFIO.Size = new Size(200, 100);
            txtFIO.Location = new Point(75, 125);
            txtFIO.Text = "Введите ФИО";
            txtFIO.Click += TxtFIO_Click;

            cmbOrganization = new ComboBox();
            cmbOrganization.Size = new Size(200, 100);
            cmbOrganization.Location = new Point(75, 175);
            cmbOrganization.Text = "Выберите организацию";
            cmbOrganization.Items.Add("Организация 1");
            cmbOrganization.Items.Add("Организация 2");
            cmbOrganization.Items.Add("Организация 3");
            //Заменить на нужные организации

            btnRegister = new Button();
            btnRegister.Size = new Size(125, 50);
            btnRegister.Location = new Point(75, 200);
            btnRegister.Text = "Зарегистрироваться";
            btnRegister.Click += BtnRegister_Click;

            btnCancel = new Button();
            btnCancel.Size = new Size(125, 50);
            btnCancel.Location = new Point(200, 200);
            btnCancel.Text = "Отмена";
            btnCancel.Click += BtnCancel_Click;

            this.Text = "Регистрация";
            this.Size = new Size(400, 300);

            this.Controls.Add(txtEmail);
            this.Controls.Add(txtPassword);
            this.Controls.Add(txtFIO);
            this.Controls.Add(cmbOrganization);
            this.Controls.Add(btnRegister);
            this.Controls.Add(btnCancel);
        }
        private void TxtFIO_Click(object sender, EventArgs e)
        {
            if (txtFIO.Text == "Введите ФИО") txtFIO.Text = "";
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
        private void BtnRegister_Click(object sender, EventArgs e)
        {
            if (cmbOrganization.SelectedItem == null)
            {
                MessageBox.Show("Выберите организацию");
                return;
            }
            //Логика регистрации. Данные пользователя в txtEmail.Text, txtPassword.Text, txtFIO.Text, cmbOrganization.SelectedItem
        }
        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
