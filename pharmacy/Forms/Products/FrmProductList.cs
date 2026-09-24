using Microsoft.EntityFrameworkCore;
using pharmacy.Helpers;
using pharmacy.Interfaces;
using pharmacy.Models;

namespace pharmacy.Forms.Products
{
    public class FrmProductList : BaseForm
    {
        private readonly IProductRepository _products;
        private readonly IRepository<Category> _categories;
        private readonly DataGridView dgv = new();
        private readonly TextBox txtSearch = new();

        public FrmProductList(IProductRepository products, IRepository<Category> categories)
        {
            _products = products;
            _categories = categories;
            Text = "Products";
            ClientSize = new Size(950, 500);
            MinimumSize = new Size(600, 350);

            var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(8, 8, 8, 0), WrapContents = false };
            var lbl = new Label { Text = "Search:", AutoSize = true, Margin = new Padding(0, 10, 4, 0) };
            txtSearch.Width = 280;
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

    public class FrmProductEdit : BaseForm
    {
        private readonly Product _p;
        private readonly TextBox txtName = new();
        private readonly TextBox txtBarcode = new();
        private readonly NumericUpDown numUnit = new() { Maximum = 1_000_000, DecimalPlaces = 2, Increment = 0.25m };
        private readonly NumericUpDown numCost = new() { Maximum = 1_000_000, DecimalPlaces = 2, Increment = 0.25m };
        private readonly NumericUpDown numReorder = new() { Maximum = 100_000 };
        private readonly CheckBox chkRx = new() { Text = "Prescription required" };
        private readonly ComboBox cmbCategory = new() { DropDownStyle = ComboBoxStyle.DropDownList };

        public FrmProductEdit(Product p, List<Category> categories)
        {
            _p = p;
            Text = p.Id == 0 ? "New Product" : "Edit Product";
            ClientSize = new Size(480, 460);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;

            cmbCategory.DataSource = categories;
            cmbCategory.DisplayMember = nameof(Category.Name);
            cmbCategory.ValueMember = nameof(Category.Id);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                ColumnCount = 1,
                RowCount = 13
            };
            for (var i = 0; i < 12; i += 2)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            }
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

            void Row(string label, Control c, int labelRow)
            {
                layout.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, labelRow);
                c.Dock = DockStyle.Fill;
                layout.Controls.Add(c, 0, labelRow + 1);
            }

            Row("Category", cmbCategory, 0);
            Row("Name", txtName, 2);
            Row("Barcode", txtBarcode, 4);
            Row("Unit Price", numUnit, 6);
            Row("Cost Price", numCost, 8);
            Row("Reorder Level", numReorder, 10);
            layout.Controls.Add(chkRx, 0, 12);

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

            txtName.Text = p.Name;
            txtBarcode.Text = p.Barcode;
            numUnit.Value = p.UnitPrice;
            numCost.Value = p.CostPrice;
            numReorder.Value = p.ReorderLevel;
            chkRx.Checked = p.IsPrescriptionRequired;
            if (p.CategoryId > 0) cmbCategory.SelectedValue = p.CategoryId;
        }

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
