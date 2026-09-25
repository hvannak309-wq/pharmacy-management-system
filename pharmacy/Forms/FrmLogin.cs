using pharmacy.Helpers;
using pharmacy.Models;

namespace pharmacy.Forms
{
    public partial class FrmLogin : BaseForm
    {
        public User? LoggedInUser { get; private set; }

        public FrmLogin() => InitializeComponent();

        private async void btnLogin_Click(object? sender, EventArgs e) => await LoginAsync();

        private async void txtPassword_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            await LoginAsync();
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
