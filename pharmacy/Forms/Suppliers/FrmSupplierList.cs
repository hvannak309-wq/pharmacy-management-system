using Microsoft.EntityFrameworkCore;
using pharmacy.Helpers;
using pharmacy.Interfaces;
using pharmacy.Models;

namespace pharmacy.Forms.Suppliers
{
    public partial class FrmSupplierList : BaseForm
    {
        private readonly IRepository<Supplier> _repo;

        public FrmSupplierList(IRepository<Supplier> repo)
        {
            InitializeComponent();
            _repo = repo;
            GridStyler.Apply(dgv);
        }

        private async void txtSearch_TextChanged(object? sender, EventArgs e) => await LoadDataAsync();

        private void btnNew_Click(object? sender, EventArgs e) => Edit(null);

        private void btnEdit_Click(object? sender, EventArgs e) => EditSelected();

        private async void btnDelete_Click(object? sender, EventArgs e) => await DeleteAsync();

        private void dgv_CellDoubleClick(object? sender, DataGridViewCellEventArgs e) => EditSelected();

        private void FrmSupplierList_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F2) Edit(null);
            if (e.KeyCode == Keys.F4) txtSearch.Focus();
            if (e.KeyCode == Keys.Delete) _ = DeleteAsync();
        }

        private async void FrmSupplierList_Load(object? sender, EventArgs e) => await LoadDataAsync();

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

    public partial class FrmSupplierEdit : BaseForm
    {
        private readonly Supplier _s;

        public FrmSupplierEdit(Supplier s)
        {
            InitializeComponent();
            _s = s;
            Text = s.Id == 0 ? "New Supplier" : "Edit Supplier";
            chkActive.Checked = s.IsActive;
            txtName.Text = s.Name; txtPhone.Text = s.Phone; txtEmail.Text = s.Email; txtAddress.Text = s.Address;
        }

        private void btnCancel_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void btnSave_Click(object? sender, EventArgs e) => Save();

        private void FrmSupplierEdit_Shown(object? sender, EventArgs e) => txtName.Focus();

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

    public partial class FrmCustomerList : BaseForm
    {
        private readonly IRepository<Customer> _repo;

        public FrmCustomerList(IRepository<Customer> repo)
        {
            InitializeComponent();
            _repo = repo;
            GridStyler.Apply(dgv);
        }

        private async void txtSearch_TextChanged(object? sender, EventArgs e) => await LoadDataAsync();

        private void btnNew_Click(object? sender, EventArgs e) => Edit(null);

        private void btnEdit_Click(object? sender, EventArgs e) => EditSelected();

        private async void btnDelete_Click(object? sender, EventArgs e) => await DeleteAsync();

        private void dgv_CellDoubleClick(object? sender, DataGridViewCellEventArgs e) => EditSelected();

        private void FrmCustomerList_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F2) Edit(null);
            if (e.KeyCode == Keys.F4) txtSearch.Focus();
            if (e.KeyCode == Keys.Delete) _ = DeleteAsync();
        }

        private async void FrmCustomerList_Load(object? sender, EventArgs e) => await LoadDataAsync();

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

    public partial class FrmCustomerEdit : BaseForm
    {
        private readonly Customer _c;

        public FrmCustomerEdit(Customer c)
        {
            InitializeComponent();
            _c = c;
            Text = c.Id == 0 ? "New Customer" : "Edit Customer";
            txtName.Text = c.Name; txtPhone.Text = c.Phone; txtEmail.Text = c.Email; txtAddress.Text = c.Address;
        }

        private void btnCancel_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void btnSave_Click(object? sender, EventArgs e) => Save();

        private void FrmCustomerEdit_Shown(object? sender, EventArgs e) => txtName.Focus();

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
