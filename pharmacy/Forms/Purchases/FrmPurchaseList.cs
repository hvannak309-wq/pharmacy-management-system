using pharmacy.DTOs;
using pharmacy.Forms.Products;
using pharmacy.Forms.Suppliers;
using pharmacy.Helpers;
using pharmacy.Interfaces;
using pharmacy.Models;

namespace pharmacy.Forms.Purchases
{
    public partial class FrmPurchaseList : BaseForm
    {
        private readonly IPurchaseService _purchases;

        public FrmPurchaseList(IPurchaseService purchases)
        {
            InitializeComponent();
            _purchases = purchases;
            GridStyler.Apply(dgv);
        }

        private async void btnRefresh_Click(object? sender, EventArgs e) => await LoadDataAsync();

        private async void btnView_Click(object? sender, EventArgs e) => await ViewAsync();

        private async void dgv_CellDoubleClick(object? sender, DataGridViewCellEventArgs e) => await ViewAsync();

        private async void FrmPurchaseList_Load(object? sender, EventArgs e) => await LoadDataAsync();

        private async Task LoadDataAsync()
        {
            var result = await _purchases.GetRecentAsync(200);
            if (!result.IsSuccess || result.Value is null) return;
            dgv.DataSource = result.Value.Select(p => new
            {
                p.Id,
                p.PurchaseDate,
                Supplier = p.Supplier?.Name,
                p.TotalAmount,
                Status = p.Status.ToString(),
                By = p.User?.FullName
            }).ToList();
            if (dgv.Columns["Id"] != null) dgv.Columns["Id"].Visible = false;
        }

        private async Task ViewAsync()
        {
            if (dgv.CurrentRow?.Cells["Id"]?.Value is not int id) return;
            var result = await _purchases.GetByIdAsync(id);
            if (result.IsSuccess && result.Value is not null)
            {
                var dlg = new FrmPurchaseView(result.Value);
                dlg.ShowDialog(this);
                dlg.Dispose();
            }
        }
    }

    public partial class FrmPurchaseView : BaseForm
    {
        public FrmPurchaseView(Purchase p)
        {
            InitializeComponent();
            Text = $"Purchase #{p.Id}";
            info.Text = $"Date: {p.PurchaseDate:g}\nSupplier: {p.Supplier?.Name}\nBy: {p.User?.FullName}\nStatus: {p.Status}\nTotal: {p.TotalAmount:0.00}";
            GridStyler.Apply(dgv);
            dgv.DataSource = p.Details.Select(d => new
            {
                d.Product?.Name,
                Batch = d.Batch?.BatchNumber,
                d.Batch?.ExpiryDate,
                d.Quantity,
                d.UnitCost,
                d.Subtotal
            }).ToList();
        }

        private void btnClose_Click(object? sender, EventArgs e) => Close();
    }

    public partial class FrmPurchaseNew : BaseForm
    {
        private readonly IPurchaseService _purchases;
        private readonly IProductRepository _products;
        private readonly IRepository<Supplier> _suppliers;
        private readonly IRepository<Category> _categories;
        private readonly User _user;

        private readonly List<PurchaseLineDto> _lines = new();
        private List<Product> _productList = new();

        public FrmPurchaseNew(IPurchaseService purchases, IProductRepository products, IRepository<Supplier> suppliers, IRepository<Category> categories, User user)
        {
            InitializeComponent();
            _purchases = purchases;
            _products = products;
            _suppliers = suppliers;
            _categories = categories;
            _user = user;

            void Row(string label, Control c, int row, Button? extra = null)
            {
                form.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
                c.Dock = DockStyle.Fill;
                form.Controls.Add(c, 1, row);
                if (extra is not null) form.Controls.Add(extra, 2, row);
            }

            Row("Supplier", cmbSupplier, 0, btnNewSupplier);
            Row("Product", cmbProduct, 1, btnNewProduct);
            Row("Batch #", txtBatch, 2);
            Row("Expiry", dtExpiry, 3);
            Row("Qty / Cost", qtyCost, 4);

            GridStyler.Apply(dgv);
        }

        private async void btnNewSupplier_Click(object? sender, EventArgs e) => await NewSupplierAsync();

        private async void btnNewProduct_Click(object? sender, EventArgs e) => await NewProductAsync();

        private void cmbProduct_SelectedIndexChanged(object? sender, EventArgs e) => SuggestBatch();

        private void btnAddLine_Click(object? sender, EventArgs e) => AddLine();

        private void btnRemoveLine_Click(object? sender, EventArgs e) => RemoveLine();

        private async void btnSave_Click(object? sender, EventArgs e) => await SaveAsync();

        private async void FrmPurchaseNew_Load(object? sender, EventArgs e) => await InitAsync();

        private async Task InitAsync()
        {
            await LoadSuppliersAsync();
            await LoadProductsAsync();
            if (cmbSupplier.Items.Count == 0) Warn("Create a supplier first (Partners → Suppliers).");
            if (_productList.Count == 0) Warn("Create a product first (Products → Product List).");
        }

        private async Task LoadSuppliersAsync()
        {
            var sups = await _suppliers.GetAllAsync();
            var active = sups.Where(s => s.IsActive).ToList();
            cmbSupplier.DataSource = null;
            cmbSupplier.DataSource = active;
            cmbSupplier.DisplayMember = nameof(Supplier.Name);
            cmbSupplier.ValueMember = nameof(Supplier.Id);
        }

        private async Task LoadProductsAsync()
        {
            _productList = await _products.GetWithStockAsync();
            cmbProduct.DataSource = null;
            cmbProduct.DataSource = _productList;
            cmbProduct.DisplayMember = nameof(Product.Name);
            cmbProduct.ValueMember = nameof(Product.Id);
        }

        private void SuggestBatch()
        {
            if (cmbProduct.SelectedItem is not Product p) return;
            txtBatch.Text = $"B{p.Id:D3}-{DateTime.Now:yyMMddHHmm}";
        }

        private async Task NewSupplierAsync()
        {
            var s = new Supplier();
            var dlg = new FrmSupplierEdit(s);
            if (dlg.ShowDialog(this) != DialogResult.OK) { dlg.Dispose(); return; }
            dlg.Dispose();
            await _suppliers.AddAsync(s);
            await AppServices.Db.SaveChangesAsync();
            await LoadSuppliersAsync();
            cmbSupplier.SelectedValue = s.Id;
        }

        private async Task NewProductAsync()
        {
            var cats = await _categories.GetAllAsync();
            if (cats.Count == 0) { Warn("Create a category first (Products → Categories)."); return; }
            var p = new Product();
            var dlg = new FrmProductEdit(p, cats);
            if (dlg.ShowDialog(this) != DialogResult.OK) { dlg.Dispose(); return; }
            dlg.Dispose();
            await _products.AddAsync(p);
            await AppServices.Db.SaveChangesAsync();
            await LoadProductsAsync();
            cmbProduct.SelectedValue = p.Id;
            SuggestBatch();
        }

        private void AddLine()
        {
            if (cmbProduct.SelectedItem is not Product p) { Warn("Select product."); return; }
            if (string.IsNullOrWhiteSpace(txtBatch.Text)) { Warn("Batch number required."); return; }
            if (dtExpiry.Value.Date < DateTime.Today) { Warn("Expiry in the past."); return; }

            _lines.Add(new PurchaseLineDto
            {
                ProductId = p.Id,
                BatchNumber = txtBatch.Text.Trim(),
                ExpiryDate = dtExpiry.Value.Date,
                Quantity = (int)numQty.Value,
                UnitCost = numCost.Value
            });
            RefreshLines();
            txtBatch.Clear();
            numQty.Value = 1;
        }

        private void RemoveLine()
        {
            if (dgv.CurrentRow?.Index is not int i || i < 0 || i >= _lines.Count) return;
            _lines.RemoveAt(i);
            RefreshLines();
        }

        private void RefreshLines()
        {
            dgv.DataSource = null;
            dgv.DataSource = _lines.Select((l, i) => new
            {
                Line = i + 1,
                Product = _productList.FirstOrDefault(p => p.Id == l.ProductId)?.Name,
                l.BatchNumber,
                l.ExpiryDate,
                l.Quantity,
                l.UnitCost,
                Subtotal = l.Quantity * l.UnitCost
            }).ToList();
            lblTotal.Text = $"Total: {_lines.Sum(l => l.Quantity * l.UnitCost):0.00}";
        }

        private async Task SaveAsync()
        {
            if (cmbSupplier.SelectedItem is not Supplier sup) { Warn("Supplier required."); return; }
            if (_lines.Count == 0) { Warn("Add at least one line."); return; }

            var dto = new PurchaseCreateDto
            {
                SupplierId = sup.Id,
                UserId = _user.Id,
                Lines = _lines
            };

            var result = await _purchases.CreateAsync(dto);
            if (!result.IsSuccess) { Warn(result.Error ?? "Save failed."); return; }

            Alert($"Purchase #{result.Value} saved. Stock updated.");
            _lines.Clear();
            RefreshLines();
        }
    }
}
