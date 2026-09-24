using pharmacy.Helpers;
using pharmacy.Models;

namespace pharmacy.Forms
{
    public class FrmLogin : BaseForm
    {
        private readonly TextBox txtUsername = new();
        private readonly TextBox txtPassword = new();
        private readonly Button btnLogin;

        public User? LoggedInUser { get; private set; }

        public FrmLogin()
        {
            Text = "PharmacyMS Login";
            ClientSize = new Size(360, 260);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var lblTitle = new Label { Text = "Pharmacy Management System", Font = new Font("Segoe UI", 13f, FontStyle.Bold), ForeColor = Color.FromArgb(45, 62, 80), AutoSize = true, Location = new Point(30, 20) };
            var lblUser = new Label { Text = "Username", Location = new Point(40, 70), AutoSize = true };
            txtUsername.Location = new Point(40, 90); txtUsername.Width = 280;
            var lblPass = new Label { Text = "Password", Location = new Point(40, 125), AutoSize = true };
            txtPassword.Location = new Point(40, 145); txtPassword.Width = 280; txtPassword.UseSystemPasswordChar = true;
            btnLogin = MakeButton("Login", Color.FromArgb(41, 128, 185));
            btnLogin.Location = new Point(40, 190); btnLogin.Width = 280;
            btnLogin.Click += async (_, _) => await LoginAsync();

            Controls.AddRange([lblTitle, lblUser, txtUsername, lblPass, txtPassword, btnLogin]);
            AcceptButton = btnLogin;
            txtPassword.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await LoginAsync(); } };
        }

        private async Task LoginAsync()
        {
            var result = await AppServices.Auth.LoginAsync(txtUsername.Text, txtPassword.Text);
            if (!result.IsSuccess || result.Value is null)
            {
                Warn(result.Error ?? "Login failed.");
                return;
            }
            LoggedInUser = result.Value;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
