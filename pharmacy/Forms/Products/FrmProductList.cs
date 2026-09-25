using Microsoft.EntityFrameworkCore;
using pharmacy.Helpers;
using pharmacy.Interfaces;
using pharmacy.Models;

namespace pharmacy.Forms.Products
{
    public partial class FrmProductList : BaseForm
    {
        private readonly IProductRepository _products;
        private readonly IRepository<Category> _categories;

        public FrmProductList(IProductRepository products, IRepository<Category> categories)
        {
            InitializeComponent();
            _products = products;
            _categories = categories;
            GridStyler.Apply(dgv);
        }

        private async void txtSearch_TextChanged(object? sender, EventArgs e) => await LoadDataAsync();

        private void btnNew_Click(object? sender, EventArgs e) => Edit(null);

        private void btnEdit_Click(object? sender, EventArgs e) => EditSelected();

        private async void btnDelete_Click(object? sender, EventArgs e) => await DeleteAsync();

        private void dgv_CellDoubleClick(object? sender, DataGridViewCellEventArgs e) => EditSelected();

        private void FrmProductList_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F2) Edit(null);
            if (e.KeyCode == Keys.F4) txtSearch.Focus();
            if (e.KeyCode == Keys.Delete) _ = DeleteAsync();
        }

        private async void FrmProductList_Load(object? sender, EventArgs e) => await LoadDataAsync();

        private async Task LoadDataAsync()
        {
            var term = txtSearch.Text.Trim();
            var list = term.Length > 0 ? await _products.SearchAsync(term) : await _products.GetWithStockAsync();
            dgv.DataSource = list.Select(p => new
            {
                p.Id,
                p.Name,
                p.Barcode,
                Category = p.Category?.Name,
                p.UnitPrice,
                p.CostPrice,
                Stock = p.Stock,
                p.ReorderLevel,
                p.IsPrescriptionRequired
            }).ToList();
            if (dgv.Columns["Id"] != null) dgv.Columns["Id"].Visible = false;
        }

        private void EditSelected()
        {
            if (dgv.CurrentRow?.Cells["Id"]?.Value is int id) Edit(id);
        }

        private async void Edit(int? id)
        {
            var p = id is null ? new Product() : await _products.GetByIdAsync(id.Value) ?? new Product();
            var cats = await _categories.GetAllAsync();
            if (cats.Count == 0) { Warn("Create a category first (Products → Categories)."); return; }
            var dlg = new FrmProductEdit(p, cats);
            if (dlg.ShowDialog(this) != DialogResult.OK) { dlg.Dispose(); return; }
            dlg.Dispose();
            if (id is null) await _products.AddAsync(p);
            else _products.Update(p);
            await AppServices.Db.SaveChangesAsync();
            await LoadDataAsync();
        }

        private async Task DeleteAsync()
        {
            if (dgv.CurrentRow?.Cells["Id"]?.Value is not int id) { Warn("Select a product."); return; }
            var used = await AppServices.Db.SaleDetails.AnyAsync(d => d.ProductId == id)
                || await AppServices.Db.PurchaseDetails.AnyAsync(d => d.ProductId == id);
            if (used) { Warn("Product has sales/purchase history. Cannot delete."); return; }
            if (AppServices.Db.Batches.Any(b => b.ProductId == id && b.Quantity > 0 && !b.IsDisposed))
            {
                if (!Confirm("Product still has stock batches. Delete anyway (batches stay orphaned — dispose them first instead)?")) return;
            }
            else if (!Confirm("Delete product?")) return;

            var batches = await AppServices.Db.Batches.Where(b => b.ProductId == id).ToListAsync();
            AppServices.Db.Batches.RemoveRange(batches);
            var p = await _products.GetByIdAsync(id);
            if (p is null) return;
            _products.Remove(p);
            await AppServices.Db.SaveChangesAsync();
            await LoadDataAsync();
        }
    }

    public partial class FrmProductEdit : BaseForm
    {
        private readonly Product _p;

        public FrmProductEdit(Product p, List<Category> categories)
        {
            InitializeComponent();
            _p = p;
            Text = p.Id == 0 ? "New Product" : "Edit Product";
            cmbCategory.DataSource = categories;
            cmbCategory.DisplayMember = nameof(Category.Name);
            cmbCategory.ValueMember = nameof(Category.Id);
            txtName.Text = p.Name;
            txtBarcode.Text = p.Barcode;
            numUnit.Value = p.UnitPrice;
            numCost.Value = p.CostPrice;
            numReorder.Value = p.ReorderLevel;
            chkRx.Checked = p.IsPrescriptionRequired;
            if (p.CategoryId > 0) cmbCategory.SelectedValue = p.CategoryId;
        }

        private void btnCancel_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void btnSave_Click(object? sender, EventArgs e) => Save();

        private void FrmProductEdit_Shown(object? sender, EventArgs e) => txtName.Focus();

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text)) { Warn("Name required."); return; }
            if (cmbCategory.SelectedItem is not Category cat) { Warn("Category required."); return; }
            if (numCost.Value > numUnit.Value && !Confirm("Cost price higher than unit price. Save anyway?")) return;

            _p.Name = txtName.Text.Trim();
            _p.Barcode = string.IsNullOrWhiteSpace(txtBarcode.Text) ? null : txtBarcode.Text.Trim();
            _p.CategoryId = cat.Id;
            _p.Category = cat;
            _p.UnitPrice = numUnit.Value;
            _p.CostPrice = numCost.Value;
            _p.ReorderLevel = (int)numReorder.Value;
            _p.IsPrescriptionRequired = chkRx.Checked;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
