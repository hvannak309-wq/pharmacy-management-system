using pharmacy.Enums;
using pharmacy.Helpers;
using pharmacy.Interfaces;
using pharmacy.Models;

namespace pharmacy.Forms.Users
{
    public class FrmUserList : BaseForm
    {
        private readonly IUserRepository _users;
        private readonly IAuthService _auth;
        private readonly DataGridView dgv = new();

        public FrmUserList(IUserRepository users, IAuthService auth)
        {
            _users = users;
            _auth = auth;
            Text = "Users";
            ClientSize = new Size(780, 450);
            MinimumSize = new Size(550, 320);

            var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(8, 8, 8, 0), WrapContents = false };

            var btnNew = MakeButton("New (F2)", Color.FromArgb(41, 128, 185));
            btnNew.Margin = new Padding(0, 2, 8, 0);
            btnNew.Click += (_, _) => Edit(null);

            var btnEdit = MakeButton("Edit", Color.FromArgb(39, 174, 96));
            btnEdit.Margin = new Padding(0, 2, 8, 0);
            btnEdit.Click += (_, _) => EditSelected();

            var btnDeactivate = MakeButton("Deactivate", Color.FromArgb(231, 76, 60));
            btnDeactivate.Width = 120;
            btnDeactivate.Margin = new Padding(0, 2, 8, 0);
            btnDeactivate.Click += async (_, _) => await DeactivateAsync();

            var btnRemove = MakeButton("Remove Account", Color.FromArgb(192, 57, 43));
            btnRemove.Width = 140;
            btnRemove.Margin = new Padding(0, 2, 8, 0);
            btnRemove.Click += async (_, _) => await RemoveAsync();

            toolbar.Controls.AddRange([btnNew, btnEdit, btnDeactivate, btnRemove]);
            dgv.Dock = DockStyle.Fill;
            GridStyler.Apply(dgv);
            dgv.CellDoubleClick += (_, _) => EditSelected();
            KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.F2) Edit(null);
                if (e.KeyCode == Keys.Delete && e.Shift) _ = RemoveAsync();
                else if (e.KeyCode == Keys.Delete) _ = DeactivateAsync();
            };

            Controls.AddRange([dgv, toolbar]);
            Load += async (_, _) => await LoadDataAsync();
        }

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

    public class FrmUserEdit : BaseForm
    {
        private readonly User _u;
        private readonly TextBox txtUsername = new();
        private readonly TextBox txtFullName = new();
        private readonly TextBox txtPassword = new();
        private readonly ComboBox cmbRole = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly CheckBox chkActive = new() { Text = "Active (can log in)" };

        public string Password => txtPassword.Text;

        public FrmUserEdit(User u)
        {
            _u = u;
            Text = u.Id == 0 ? "New User" : "Edit User";
            ClientSize = new Size(440, 400);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;

            cmbRole.DataSource = Enum.GetValues<UserRole>();

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                ColumnCount = 1,
                RowCount = 9
            };
            for (var i = 0; i < 8; i++) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

            void Row(string label, Control c, int labelRow)
            {
                layout.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, labelRow);
                c.Dock = DockStyle.Fill;
                layout.Controls.Add(c, 0, labelRow + 1);
            }

            Row("Username", txtUsername, 0);
            Row("Full Name", txtFullName, 2);
            Row("Password (blank keeps current)", txtPassword, 4);
            txtPassword.UseSystemPasswordChar = true;
            Row("Role", cmbRole, 6);
            chkActive.Checked = u.IsActive;
            layout.Controls.Add(chkActive, 0, 8);

            var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 8, 0, 0) };
            var btnCancel = MakeButton("Cancel", Color.FromArgb(149, 165, 166));
            btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
            var btnSave = MakeButton("Save", Color.FromArgb(39, 174, 96));
            btnSave.Click += (_, _) => Save();
            btnPanel.Controls.Add(btnCancel);
            btnPanel.Controls.Add(btnSave);

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            root.Controls.Add(layout, 0, 0);
            root.Controls.Add(btnPanel, 0, 1);
            Controls.Add(root);

            AcceptButton = btnSave;
            CancelButton = btnCancel;
            Shown += (_, _) => txtUsername.Focus();

            txtUsername.Text = u.Username;
            txtFullName.Text = u.FullName;
            cmbRole.SelectedItem = u.Role;
        }

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
