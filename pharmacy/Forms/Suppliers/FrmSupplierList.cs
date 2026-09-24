using Microsoft.EntityFrameworkCore;
using pharmacy.Helpers;
using pharmacy.Interfaces;
using pharmacy.Models;

namespace pharmacy.Forms.Suppliers
{
    public class FrmSupplierList : BaseForm
    {
        private readonly IRepository<Supplier> _repo;
        private readonly DataGridView dgv = new();
        private readonly TextBox txtSearch = new();

        public FrmSupplierList(IRepository<Supplier> repo)
        {
            _repo = repo;
            Text = "Suppliers";
            ClientSize = new Size(800, 450);
            MinimumSize = new Size(550, 320);

            var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(8, 8, 8, 0), WrapContents = false };
            var lbl = new Label { Text = "Search:", AutoSize = true, Margin = new Padding(0, 10, 4, 0) };
            txtSearch.Width = 220;
            txtSearch.Margin = new Padding(0, 6, 12, 0);
            txtSearch.TextChanged += async (_, _) => await LoadDataAsync();

            var btnNew = MakeButton("New (F2)", Color.FromArgb(41, 128, 185));
            btnNew.Margin = new Padding(0, 2, 8, 0);
            btnNew.Click += (_, _) => Edit(null);

            var btnEdit = MakeButton("Edit", Color.FromArgb(39, 174, 96));
            btnEdit.Margin = new Padding(0, 2, 8, 0);
            btnEdit.Click += (_, _) => EditSelected();

            var btnDelete = MakeButton("Delete", Color.FromArgb(231, 76, 60));
            btnDelete.Margin = new Padding(0, 2, 8, 0);
            btnDelete.Click += async (_, _) => await DeleteAsync();

            toolbar.Controls.AddRange([lbl, txtSearch, btnNew, btnEdit, btnDelete]);
            dgv.Dock = DockStyle.Fill;
            GridStyler.Apply(dgv);
            dgv.CellDoubleClick += (_, _) => EditSelected();
            KeyDown += (_, e) => { if (e.KeyCode == Keys.F2) Edit(null); if (e.KeyCode == Keys.F4) txtSearch.Focus(); if (e.KeyCode == Keys.Delete) _ = DeleteAsync(); };

            Controls.AddRange([dgv, toolbar]);
            Load += async (_, _) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            var all = await _repo.GetAllAsync();
            var term = txtSearch.Text.Trim();
            if (term.Length > 0) all = all.Where(s => s.Name.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
            dgv.DataSource = all.Select(s => new { s.Id, s.Name, s.Phone, s.Email, s.Address, s.IsActive }).ToList();
            if (dgv.Columns["Id"] != null) dgv.Columns["Id"].Visible = false;
        }

        private void EditSelected()
        {
            if (dgv.CurrentRow?.Cells["Id"]?.Value is int id) Edit(id);
        }

        private async void Edit(int? id)
        {
            var s = id is null ? new Supplier() : await _repo.GetByIdAsync(id.Value) ?? new Supplier();
            var dlg = new FrmSupplierEdit(s);
            if (dlg.ShowDialog(this) != DialogResult.OK) { dlg.Dispose(); return; }
            dlg.Dispose();
            if (id is null) await _repo.AddAsync(s);
            else _repo.Update(s);
            await AppServices.Db.SaveChangesAsync();
            await LoadDataAsync();
        }

        private async Task DeleteAsync()
        {
            if (dgv.CurrentRow?.Cells["Id"]?.Value is not int id) { Warn("Select a supplier."); return; }
            if (AppServices.Db.Purchases.Any(p => p.SupplierId == id)) { Warn("Supplier has purchases. Cannot delete."); return; }
            if (!Confirm("Delete supplier?")) return;
            var s = await _repo.GetByIdAsync(id);
            if (s is null) return;
            _repo.Remove(s);
            await AppServices.Db.SaveChangesAsync();
            await LoadDataAsync();
        }
    }

    public class FrmSupplierEdit : BaseForm
    {
        private readonly Supplier _s;
        private readonly TextBox txtName = new();
        private readonly TextBox txtPhone = new();
        private readonly TextBox txtEmail = new();
        private readonly TextBox txtAddress = new();
        private readonly CheckBox chkActive = new() { Text = "Active (uncheck to deactivate)" };

        public FrmSupplierEdit(Supplier s)
        {
            _s = s;
            Text = s.Id == 0 ? "New Supplier" : "Edit Supplier";
            ClientSize = new Size(460, 400);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                ColumnCount = 1,
                RowCount = 9
            };
            for (var i = 0; i < 8; i++) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

            void AddLabel(string text, int row)
            {
                var l = new Label { Text = text, AutoSize = true, Anchor = AnchorStyles.Left };
                layout.Controls.Add(l, 0, row);
            }
            void AddControl(Control c, int row)
            {
                c.Dock = DockStyle.Fill;
                layout.Controls.Add(c, 0, row);
            }

            AddLabel("Name", 0); AddControl(txtName, 1);
            AddLabel("Phone", 2); AddControl(txtPhone, 3);
            AddLabel("Email", 4); AddControl(txtEmail, 5);
            AddLabel("Address", 6); AddControl(txtAddress, 7);
            chkActive.Checked = s.IsActive;
            layout.Controls.Add(chkActive, 0, 8);

            var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 8, 0, 0) };
            var btnCancel = MakeButton("Cancel", Color.FromArgb(149, 165, 166));
            btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
            var btnSave = MakeButton("Save", Color.FromArgb(39, 174, 96));
            btnSave.Click += (_, _) => Save();
            btnPanel.Controls.Add(btnCancel);
            btnPanel.Controls.Add(btnSave);

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(0) };
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            root.Controls.Add(layout, 0, 0);
            root.Controls.Add(btnPanel, 0, 1);
            Controls.Add(root);

            AcceptButton = btnSave;
            CancelButton = btnCancel;
            Shown += (_, _) => txtName.Focus();

            txtName.Text = s.Name; txtPhone.Text = s.Phone; txtEmail.Text = s.Email; txtAddress.Text = s.Address;
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text)) { Warn("Name required."); return; }
            _s.Name = txtName.Text.Trim();
            _s.Phone = txtPhone.Text.Trim();
            _s.Email = txtEmail.Text.Trim();
            _s.Address = txtAddress.Text.Trim();
            _s.IsActive = chkActive.Checked;
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    public class FrmCustomerList : BaseForm
    {
        private readonly IRepository<Customer> _repo;
        private readonly DataGridView dgv = new();
        private readonly TextBox txtSearch = new();

        public FrmCustomerList(IRepository<Customer> repo)
        {
            _repo = repo;
            Text = "Customers";
            ClientSize = new Size(800, 450);
            MinimumSize = new Size(550, 320);

            var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(8, 8, 8, 0), WrapContents = false };
            var lbl = new Label { Text = "Search:", AutoSize = true, Margin = new Padding(0, 10, 4, 0) };
            txtSearch.Width = 220;
            txtSearch.Margin = new Padding(0, 6, 12, 0);
            txtSearch.TextChanged += async (_, _) => await LoadDataAsync();

            var btnNew = MakeButton("New (F2)", Color.FromArgb(41, 128, 185));
            btnNew.Margin = new Padding(0, 2, 8, 0);
            btnNew.Click += (_, _) => Edit(null);

            var btnEdit = MakeButton("Edit", Color.FromArgb(39, 174, 96));
            btnEdit.Margin = new Padding(0, 2, 8, 0);
            btnEdit.Click += (_, _) => EditSelected();

            var btnDelete = MakeButton("Delete", Color.FromArgb(231, 76, 60));
            btnDelete.Margin = new Padding(0, 2, 8, 0);
            btnDelete.Click += async (_, _) => await DeleteAsync();

            toolbar.Controls.AddRange([lbl, txtSearch, btnNew, btnEdit, btnDelete]);
            dgv.Dock = DockStyle.Fill;
            GridStyler.Apply(dgv);
            dgv.CellDoubleClick += (_, _) => EditSelected();
            KeyDown += (_, e) => { if (e.KeyCode == Keys.F2) Edit(null); if (e.KeyCode == Keys.F4) txtSearch.Focus(); if (e.KeyCode == Keys.Delete) _ = DeleteAsync(); };

            Controls.AddRange([dgv, toolbar]);
            Load += async (_, _) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            var all = await _repo.GetAllAsync();
            var term = txtSearch.Text.Trim();
            if (term.Length > 0) all = all.Where(c => c.Name.Contains(term, StringComparison.OrdinalIgnoreCase) || (c.Phone != null && c.Phone.Contains(term))).ToList();
            dgv.DataSource = all.Select(c => new { c.Id, c.Name, c.Phone, c.Email, c.Address }).ToList();
            if (dgv.Columns["Id"] != null) dgv.Columns["Id"].Visible = false;
        }

        private void EditSelected()
        {
            if (dgv.CurrentRow?.Cells["Id"]?.Value is int id) Edit(id);
        }

        private async void Edit(int? id)
        {
            var c = id is null ? new Customer() : await _repo.GetByIdAsync(id.Value) ?? new Customer();
            var dlg = new FrmCustomerEdit(c);
            if (dlg.ShowDialog(this) != DialogResult.OK) { dlg.Dispose(); return; }
            dlg.Dispose();
            if (id is null) await _repo.AddAsync(c);
            else _repo.Update(c);
            await AppServices.Db.SaveChangesAsync();
            await LoadDataAsync();
        }

        private async Task DeleteAsync()
        {
            if (dgv.CurrentRow?.Cells["Id"]?.Value is not int id) { Warn("Select a customer."); return; }
            if (AppServices.Db.Sales.Any(s => s.CustomerId == id)) { Warn("Customer has sales. Cannot delete."); return; }
            if (!Confirm("Delete customer?")) return;
            var c = await _repo.GetByIdAsync(id);
            if (c is null) return;
            _repo.Remove(c);
            await AppServices.Db.SaveChangesAsync();
            await LoadDataAsync();
        }
    }

    public class FrmCustomerEdit : BaseForm
    {
        private readonly Customer _c;
        private readonly TextBox txtName = new();
        private readonly TextBox txtPhone = new();
        private readonly TextBox txtEmail = new();
        private readonly TextBox txtAddress = new();

        public FrmCustomerEdit(Customer c)
        {
            _c = c;
            Text = c.Id == 0 ? "New Customer" : "Edit Customer";
            ClientSize = new Size(460, 360);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                ColumnCount = 1,
                RowCount = 8
            };
            for (var i = 0; i < 8; i++) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));

            void AddLabel(string text, int row) => layout.Controls.Add(new Label { Text = text, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            void AddControl(Control c, int row) { c.Dock = DockStyle.Fill; layout.Controls.Add(c, 0, row); }

            AddLabel("Name", 0); AddControl(txtName, 1);
            AddLabel("Phone", 2); AddControl(txtPhone, 3);
            AddLabel("Email", 4); AddControl(txtEmail, 5);
            AddLabel("Address", 6); txtAddress.Height = 60; AddControl(txtAddress, 7);

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
            Shown += (_, _) => txtName.Focus();
            txtName.Text = c.Name; txtPhone.Text = c.Phone; txtEmail.Text = c.Email; txtAddress.Text = c.Address;
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text)) { Warn("Name required."); return; }
            _c.Name = txtName.Text.Trim();
            _c.Phone = txtPhone.Text.Trim();
            _c.Email = txtEmail.Text.Trim();
            _c.Address = txtAddress.Text.Trim();
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
