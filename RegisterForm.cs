using System;
using System.Drawing;
using System.Windows.Forms;

namespace EventSync_Manager
{
    public partial class RegisterForm : Form
    {
        public RegisterForm()
        {
            InitializeComponent();
            InitializeCustomComponents();
        }

        private void InitializeCustomComponents()
        {
            this.Text = "Регистрация";
            this.Size = new Size(500, 500);
            this.MinimumSize = new Size(450, 450);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White; 

            // Панель
            var mainPanel = new Panel();
            mainPanel.Size = new Size(300, 400);
            mainPanel.BackColor = Color.White;
            mainPanel.BorderStyle = BorderStyle.None;

            // Заголовок
            var titleLabel = new Label();
            titleLabel.Text = "Регистрация пользователя";
            titleLabel.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            titleLabel.ForeColor = Color.FromArgb(51, 51, 51);
            titleLabel.TextAlign = ContentAlignment.MiddleCenter;
            titleLabel.Size = new Size(300, 50);
            titleLabel.Location = new Point(0, 10);
            mainPanel.Controls.Add(titleLabel);

            // Email
            var emailLabel = new Label();
            emailLabel.Text = "Email:";
            emailLabel.Font = new Font("Segoe UI", 12);
            emailLabel.Location = new Point(0, 80);
            mainPanel.Controls.Add(emailLabel);

            var emailTextBox = new TextBox();
            emailTextBox.Size = new Size(300, 30);
            emailTextBox.Location = new Point(0, 105);
            emailTextBox.Font = new Font("Segoe UI", 12);
            emailTextBox.Padding = new Padding(5);
            emailTextBox.BorderStyle = BorderStyle.FixedSingle;
            mainPanel.Controls.Add(emailTextBox);

            // Пароль
            var passwordLabel = new Label();
            passwordLabel.Text = "Пароль:";
            passwordLabel.Font = new Font("Segoe UI", 12);
            passwordLabel.Location = new Point(0, 150);
            mainPanel.Controls.Add(passwordLabel);

            var passwordTextBox = new TextBox();
            passwordTextBox.Size = new Size(300, 30);
            passwordTextBox.Location = new Point(0, 175);
            passwordTextBox.Font = new Font("Segoe UI", 12);
            passwordTextBox.Padding = new Padding(5);
            passwordTextBox.BorderStyle = BorderStyle.FixedSingle;
            passwordTextBox.UseSystemPasswordChar = true;
            mainPanel.Controls.Add(passwordTextBox);

            // ФИО
            var fullNameLabel = new Label();
            fullNameLabel.Text = "ФИО:";
            fullNameLabel.Font = new Font("Segoe UI", 12);
            fullNameLabel.Location = new Point(0, 220);
            mainPanel.Controls.Add(fullNameLabel);

            var fullNameTextBox = new TextBox();
            fullNameTextBox.Size = new Size(300, 30);
            fullNameTextBox.Location = new Point(0, 245);
            fullNameTextBox.Font = new Font("Segoe UI", 12);
            fullNameTextBox.Padding = new Padding(5);
            fullNameTextBox.BorderStyle = BorderStyle.FixedSingle;
            mainPanel.Controls.Add(fullNameTextBox);

            // Кнопка регистрации
            var registerButton = new Button();
            registerButton.Text = "Зарегистрироваться";
            registerButton.Size = new Size(300, 40);
            registerButton.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            registerButton.BackColor = Color.FromArgb(0, 120, 212);
            registerButton.ForeColor = Color.White;
            registerButton.FlatStyle = FlatStyle.Flat;
            registerButton.FlatAppearance.BorderSize = 0;
            registerButton.Cursor = Cursors.Hand;
            registerButton.Location = new Point(0, 290);
            registerButton.Click += RegisterButton_Click;
            mainPanel.Controls.Add(registerButton);

            // Кнопка отмены
            var cancelButton = new Button();
            cancelButton.Text = "Отмена";
            cancelButton.Size = new Size(300, 40);
            cancelButton.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            cancelButton.BackColor = Color.FromArgb(108, 117, 125);
            cancelButton.ForeColor = Color.White;
            cancelButton.FlatStyle = FlatStyle.Flat;
            cancelButton.FlatAppearance.BorderSize = 0;
            cancelButton.Cursor = Cursors.Hand;
            cancelButton.Location = new Point(0, 340);
            cancelButton.Click += CancelButton_Click;
            mainPanel.Controls.Add(cancelButton);

            this.Controls.Add(mainPanel);
            CenterPanel(mainPanel);
            this.Resize += (sender, e) => CenterPanel(mainPanel);
        }

        private void CenterPanel(Panel panel)
        {
            panel.Location = new Point(
                (this.ClientSize.Width - panel.Width) / 2,
                (this.ClientSize.Height - panel.Height) / 2
            );
        }

        private void RegisterButton_Click(object sender, EventArgs e)
        {
            var emailTextBox = (TextBox)this.Controls[0].Controls[2];
            var passwordTextBox = (TextBox)this.Controls[0].Controls[4];
            var fullNameTextBox = (TextBox)this.Controls[0].Controls[6];

            string email = emailTextBox.Text;
            string password = passwordTextBox.Text;
            string fullName = fullNameTextBox.Text;

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(fullName))
            {
                MessageBox.Show("Пожалуйста, заполните все поля", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }



            // Добавить логику регистрации



            MessageBox.Show($"Регистрация для: {fullName}\nEmail: {email}", "Успешно",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            this.Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}