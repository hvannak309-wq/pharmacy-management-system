using Microsoft.EntityFrameworkCore;
using pharmacy.Helpers;
using pharmacy.Interfaces;
using pharmacy.Models;

namespace pharmacy.Forms.Categories
{
    public partial class FrmCategoryList : BaseForm
    {
        private readonly IRepository<Category> _repo;

        public FrmCategoryList(IRepository<Category> repo)
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

        private void FrmCategoryList_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F2) Edit(null);
            if (e.KeyCode == Keys.F4) txtSearch.Focus();
            if (e.KeyCode == Keys.Delete) _ = DeleteAsync();
        }

        private async void FrmCategoryList_Load(object? sender, EventArgs e) => await LoadDataAsync();

        private async Task LoadDataAsync()
        {
            var all = await _repo.GetAllAsync();
            var term = txtSearch.Text.Trim();
            if (term.Length > 0) all = all.Where(c => c.Name.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
            dgv.DataSource = all.Select(c => new { c.Id, c.Name, c.Description }).ToList();
            if (dgv.Columns["Id"] != null) dgv.Columns["Id"].Visible = false;
        }

        private void EditSelected()
        {
            if (dgv.CurrentRow?.Cells["Id"]?.Value is int id) Edit(id);
        }

        private async void Edit(int? id)
        {
            var cat = id is null ? new Category() : await _repo.GetByIdAsync(id.Value) ?? new Category();
            var dlg = new FrmCategoryEdit(cat);
            if (dlg.ShowDialog(this) != DialogResult.OK) { dlg.Dispose(); return; }
            dlg.Dispose();
            if (id is null) await _repo.AddAsync(cat);
            else _repo.Update(cat);
            await AppServices.Db.SaveChangesAsync();
            await LoadDataAsync();
        }

        private async Task DeleteAsync()
        {
            if (dgv.CurrentRow?.Cells["Id"]?.Value is not int id) { Warn("Select a category."); return; }
            if (AppServices.Db.Products.Any(p => p.CategoryId == id)) { Warn("Category has products. Remove them first."); return; }
            if (!Confirm("Delete category?")) return;
            var cat = await _repo.GetByIdAsync(id);
            if (cat is null) return;
            _repo.Remove(cat);
            await AppServices.Db.SaveChangesAsync();
            await LoadDataAsync();
        }
    }

    public partial class FrmCategoryEdit : BaseForm
    {
        private readonly Category _cat;

        public FrmCategoryEdit(Category cat)
        {
            InitializeComponent();
            _cat = cat;
            Text = cat.Id == 0 ? "New Category" : "Edit Category";
            txtName.Text = cat.Name;
            txtDesc.Text = cat.Description ?? "";
        }

        private void btnCancel_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void btnSave_Click(object? sender, EventArgs e) => Save();

        private void FrmCategoryEdit_Shown(object? sender, EventArgs e) => txtName.Focus();

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text)) { Warn("Name required."); return; }
            _cat.Name = txtName.Text.Trim();
            _cat.Description = txtDesc.Text.Trim();
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
