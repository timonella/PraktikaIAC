using System;
using System.Drawing;
using System.Windows.Forms;

namespace EventSync_Manager
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            InitializeCustomComponents();
        }

        private void InitializeCustomComponents()
        {
            this.Text = "EventSync Manager";
            this.Size = new Size(500, 400);
            this.MinimumSize = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;

            // Панель
            var mainPanel = new Panel();
            mainPanel.Size = new Size(200, 150);
            mainPanel.BackColor = Color.White;
            mainPanel.BorderStyle = BorderStyle.None;

            // Кнопка регистрации
            var btnRegister = new Button();
            btnRegister.Text = "Регистрация";
            btnRegister.Size = new Size(200, 50);
            btnRegister.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            btnRegister.BackColor = Color.FromArgb(0, 120, 212);
            btnRegister.ForeColor = Color.White;
            btnRegister.FlatStyle = FlatStyle.Flat;
            btnRegister.FlatAppearance.BorderSize = 0;
            btnRegister.Cursor = Cursors.Hand;
            btnRegister.Location = new Point(0, 0);
            btnRegister.Click += BtnRegister_Click;
            mainPanel.Controls.Add(btnRegister);

            // Кнопка входа
            var btnLogin = new Button();
            btnLogin.Text = "Вход";
            btnLogin.Size = new Size(200, 50);
            btnLogin.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            btnLogin.BackColor = Color.FromArgb(40, 167, 69);
            btnLogin.ForeColor = Color.White;
            btnLogin.FlatStyle = FlatStyle.Flat;
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Cursor = Cursors.Hand;
            btnLogin.Location = new Point(0, 60);
            btnLogin.Click += BtnLogin_Click;
            mainPanel.Controls.Add(btnLogin);

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

        private void BtnRegister_Click(object sender, EventArgs e)
        {
            var registerForm = new RegisterForm();
            registerForm.ShowDialog();
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            var loginForm = new LoginForm();
            loginForm.ShowDialog();
        }
    }
}
