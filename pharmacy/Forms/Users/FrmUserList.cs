using pharmacy.Enums;
using pharmacy.Helpers;
using pharmacy.Interfaces;
using pharmacy.Models;

namespace pharmacy.Forms.Users
{
    public partial class FrmUserList : BaseForm
    {
        private readonly IUserRepository _users;
        private readonly IAuthService _auth;

        public FrmUserList(IUserRepository users, IAuthService auth)
        {
            InitializeComponent();
            _users = users;
            _auth = auth;
            GridStyler.Apply(dgv);
        }

        private void btnNew_Click(object? sender, EventArgs e) => Edit(null);

        private void btnEdit_Click(object? sender, EventArgs e) => EditSelected();

        private async void btnDeactivate_Click(object? sender, EventArgs e) => await DeactivateAsync();

        private async void btnRemove_Click(object? sender, EventArgs e) => await RemoveAsync();

        private void dgv_CellDoubleClick(object? sender, DataGridViewCellEventArgs e) => EditSelected();

        private void FrmUserList_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F2) Edit(null);
            if (e.KeyCode == Keys.Delete && e.Shift) _ = RemoveAsync();
            else if (e.KeyCode == Keys.Delete) _ = DeactivateAsync();
        }

        private async void FrmUserList_Load(object? sender, EventArgs e) => await LoadDataAsync();

        private async Task LoadDataAsync()
        {
            var all = await _users.GetAllAsync();
            dgv.DataSource = all.Select(u => new { u.Id, u.Username, u.FullName, Role = u.Role.ToString(), u.IsActive }).ToList();
            if (dgv.Columns["Id"] != null) dgv.Columns["Id"].Visible = false;
        }

        private void EditSelected()
        {
            if (dgv.CurrentRow?.Cells["Id"]?.Value is int id) Edit(id);
        }

        private async void Edit(int? id)
        {
            var u = id is null ? new User { Role = UserRole.Cashier, IsActive = true } : await _users.GetByIdAsync(id.Value) ?? new User();
            var dlg = new FrmUserEdit(u);
            if (dlg.ShowDialog(this) != DialogResult.OK) { dlg.Dispose(); return; }
            var password = dlg.Password;
            dlg.Dispose();

            var result = id is null
                ? await _auth.CreateUserAsync(u, password)
                : await _auth.UpdateUserAsync(u, string.IsNullOrEmpty(password) ? null : password);

            if (!result.IsSuccess) { Warn(result.Error ?? "Save failed."); return; }
            await LoadDataAsync();
        }

        private async Task DeactivateAsync()
        {
            if (dgv.CurrentRow?.Cells["Id"]?.Value is not int id) { Warn("Select a user."); return; }
            if (id == 1) { Warn("Cannot deactivate the default admin."); return; }
            if (!Confirm("Deactivate user? They will no longer be able to log in.")) return;
            var u = await _users.GetByIdAsync(id);
            if (u is null) return;
            u.IsActive = false;
            _users.Update(u);
            await AppServices.Db.SaveChangesAsync();
            await LoadDataAsync();
        }

        private async Task RemoveAsync()
        {
            if (dgv.CurrentRow?.Cells["Id"]?.Value is not int id) { Warn("Select a user."); return; }
            if (id == 1) { Warn("Cannot remove the default admin."); return; }
            if (AppServices.Db.Sales.Any(s => s.UserId == id) ||
                AppServices.Db.Purchases.Any(p => p.UserId == id))
            {
                Warn("User has sales or purchase history. Deactivate instead to keep records.");
                return;
            }
            if (!Confirm("Permanently remove this account? This cannot be undone.")) return;
            var u = await _users.GetByIdAsync(id);
            if (u is null) return;
            _users.Remove(u);
            await AppServices.Db.SaveChangesAsync();
            await LoadDataAsync();
        }
    }

    public partial class FrmUserEdit : BaseForm
    {
        private readonly User _u;

        public string Password => txtPassword.Text;

        public FrmUserEdit(User u)
        {
            InitializeComponent();
            _u = u;
            Text = u.Id == 0 ? "New User" : "Edit User";
            cmbRole.DataSource = Enum.GetValues<UserRole>();
            chkActive.Checked = u.IsActive;
            txtUsername.Text = u.Username;
            txtFullName.Text = u.FullName;
            cmbRole.SelectedItem = u.Role;
        }

        private void btnCancel_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void btnSave_Click(object? sender, EventArgs e) => Save();

        private void FrmUserEdit_Shown(object? sender, EventArgs e) => txtUsername.Focus();

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text)) { Warn("Username required."); return; }
            if (string.IsNullOrWhiteSpace(txtFullName.Text)) { Warn("Full name required."); return; }
            if (_u.Id == 0 && string.IsNullOrEmpty(txtPassword.Text)) { Warn("Password required for new user."); return; }

            _u.Username = txtUsername.Text.Trim();
            _u.FullName = txtFullName.Text.Trim();
            _u.Role = (UserRole)cmbRole.SelectedItem!;
            _u.IsActive = chkActive.Checked;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
