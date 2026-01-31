using System;
using System.Drawing;
using System.Windows.Forms;

namespace EventSync_Manager
{
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
            InitializeCustomComponents();
        }

        private void InitializeCustomComponents()
        {
            this.Text = "Вход в систему";
            this.Size = new Size(500, 400);
            this.MinimumSize = new Size(450, 350);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;

            // Панель
            var mainPanel = new Panel();
            mainPanel.Size = new Size(300, 300);
            mainPanel.BackColor = Color.White;
            mainPanel.BorderStyle = BorderStyle.None;

            // Заголовок
            var titleLabel = new Label();
            titleLabel.Text = "Вход в систему";
            titleLabel.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            titleLabel.ForeColor = Color.FromArgb(51, 51, 51);
            titleLabel.TextAlign = ContentAlignment.MiddleCenter;
            titleLabel.Size = new Size(300, 40);
            titleLabel.Location = new Point(0, 10);
            mainPanel.Controls.Add(titleLabel);

            // Email
            var emailLabel = new Label();
            emailLabel.Text = "Email:";
            emailLabel.Font = new Font("Segoe UI", 12);
            emailLabel.Location = new Point(0, 70);
            mainPanel.Controls.Add(emailLabel);

            var emailTextBox = new TextBox();
            emailTextBox.Size = new Size(300, 30);
            emailTextBox.Location = new Point(0, 95);
            emailTextBox.Font = new Font("Segoe UI", 12);
            emailTextBox.Padding = new Padding(5);
            emailTextBox.BorderStyle = BorderStyle.FixedSingle;
            mainPanel.Controls.Add(emailTextBox);

            // Пароль
            var passwordLabel = new Label();
            passwordLabel.Text = "Пароль:";
            passwordLabel.Font = new Font("Segoe UI", 12);
            passwordLabel.Location = new Point(0, 140);
            mainPanel.Controls.Add(passwordLabel);

            var passwordTextBox = new TextBox();
            passwordTextBox.Size = new Size(300, 30);
            passwordTextBox.Location = new Point(0, 165);
            passwordTextBox.Font = new Font("Segoe UI", 12);
            passwordTextBox.Padding = new Padding(5);
            passwordTextBox.BorderStyle = BorderStyle.FixedSingle;
            passwordTextBox.UseSystemPasswordChar = true;
            mainPanel.Controls.Add(passwordTextBox);

            // Кнопка входа
            var loginButton = new Button();
            loginButton.Text = "Войти";
            loginButton.Size = new Size(300, 40);
            loginButton.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            loginButton.BackColor = Color.FromArgb(40, 167, 69);
            loginButton.ForeColor = Color.White;
            loginButton.FlatStyle = FlatStyle.Flat;
            loginButton.FlatAppearance.BorderSize = 0;
            loginButton.Cursor = Cursors.Hand;
            loginButton.Location = new Point(0, 210);
            loginButton.Click += LoginButton_Click;
            mainPanel.Controls.Add(loginButton);

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
            cancelButton.Location = new Point(0, 255);
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

        private void LoginButton_Click(object sender, EventArgs e)
        {
            var emailTextBox = (TextBox)this.Controls[0].Controls[2];
            var passwordTextBox = (TextBox)this.Controls[0].Controls[4];

            string email = emailTextBox.Text;
            string password = passwordTextBox.Text;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Пожалуйста, заполните все поля", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }



            // Добавить логику входа



            MessageBox.Show($"Вход выполнен для: {email}", "Успешно",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            this.Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}