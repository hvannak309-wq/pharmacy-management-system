using pharmacy.DTOs;
using pharmacy.Forms.Products;
using pharmacy.Forms.Suppliers;
using pharmacy.Helpers;
using pharmacy.Interfaces;
using pharmacy.Models;

namespace pharmacy.Forms.Purchases
{
    public class FrmPurchaseList : BaseForm
    {
        private readonly IPurchaseService _purchases;
        private readonly DataGridView dgv = new();

        public FrmPurchaseList(IPurchaseService purchases)
        {
            _purchases = purchases;
            Text = "Purchases";
            ClientSize = new Size(900, 480);
            MinimumSize = new Size(600, 350);

            var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(8, 8, 8, 0), WrapContents = false };
            var btnRefresh = MakeButton("Refresh", Color.FromArgb(41, 128, 185));
            btnRefresh.Margin = new Padding(0, 2, 8, 0);
            btnRefresh.Click += async (_, _) => await LoadDataAsync();

            var btnView = MakeButton("View", Color.FromArgb(39, 174, 96));
            btnView.Margin = new Padding(0, 2, 8, 0);
            btnView.Click += async (_, _) => await ViewAsync();

            toolbar.Controls.AddRange([btnRefresh, btnView]);
            dgv.Dock = DockStyle.Fill;
            GridStyler.Apply(dgv);
            dgv.CellDoubleClick += async (_, _) => await ViewAsync();

            Controls.AddRange([dgv, toolbar]);
            Load += async (_, _) => await LoadDataAsync();
        }

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

    public class FrmPurchaseView : BaseForm
    {
        public FrmPurchaseView(Purchase p)
        {
            Text = $"Purchase #{p.Id}";
            ClientSize = new Size(700, 440);
            MinimumSize = new Size(500, 350);

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(12) };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

            var info = new Label
            {
                Dock = DockStyle.Fill,
                Text = $"Date: {p.PurchaseDate:g}\nSupplier: {p.Supplier?.Name}\nBy: {p.User?.FullName}\nStatus: {p.Status}\nTotal: {p.TotalAmount:0.00}"
            };

            var dgv = new DataGridView { Dock = DockStyle.Fill };
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

            var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            var btnClose = MakeButton("Close", Color.FromArgb(149, 165, 166));
            btnClose.Click += (_, _) => Close();
            btnPanel.Controls.Add(btnClose);

            root.Controls.Add(info, 0, 0);
            root.Controls.Add(dgv, 0, 1);
            root.Controls.Add(btnPanel, 0, 2);
            Controls.Add(root);
            AcceptButton = btnClose;
        }
    }

    public class FrmPurchaseNew : BaseForm
    {
        private readonly IPurchaseService _purchases;
        private readonly IProductRepository _products;
        private readonly IRepository<Supplier> _suppliers;
        private readonly IRepository<Category> _categories;
        private readonly User _user;

        private readonly ComboBox cmbSupplier = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ComboBox cmbProduct = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly TextBox txtBatch = new();
        private readonly DateTimePicker dtExpiry = new() { Format = DateTimePickerFormat.Short };
        private readonly NumericUpDown numQty = new() { Minimum = 1, Maximum = 99999, Value = 1 };
        private readonly NumericUpDown numCost = new() { Maximum = 1000000, DecimalPlaces = 2, Increment = 0.25m };
        private readonly DataGridView dgv = new();
        private readonly Label lblTotal = new();

        private readonly List<PurchaseLineDto> _lines = new();
        private List<Product> _productList = new();

        public FrmPurchaseNew(IPurchaseService purchases, IProductRepository products, IRepository<Supplier> suppliers, IRepository<Category> categories, User user)
        {
            _purchases = purchases;
            _products = products;
            _suppliers = suppliers;
            _categories = categories;
            _user = user;
            Text = "New Purchase";
            ClientSize = new Size(1000, 560);
            MinimumSize = new Size(750, 450);

            var split = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(8)
            };
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 420));
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var form = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 6, ColumnCount = 3, Padding = new Padding(0, 0, 12, 0) };
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
            for (var i = 0; i < 5; i++) form.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            form.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            void Row(string label, Control c, int row, Button? extra = null)
            {
                form.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
                c.Dock = DockStyle.Fill;
                form.Controls.Add(c, 1, row);
                if (extra is not null) form.Controls.Add(extra, 2, row);
            }

            var btnNewSupplier = MakeButton("+ New", Color.FromArgb(41, 128, 185));
            btnNewSupplier.Dock = DockStyle.Fill;
            btnNewSupplier.Click += async (_, _) => await NewSupplierAsync();

            var btnNewProduct = MakeButton("+ New", Color.FromArgb(41, 128, 185));
            btnNewProduct.Dock = DockStyle.Fill;
            btnNewProduct.Click += async (_, _) => await NewProductAsync();

            dtExpiry.Value = DateTime.Today.AddYears(1);
            Row("Supplier", cmbSupplier, 0, btnNewSupplier);
            Row("Product", cmbProduct, 1, btnNewProduct);
            Row("Batch #", txtBatch, 2);
            Row("Expiry", dtExpiry, 3);

            numQty.Width = 70;
            numCost.Width = 110;
            var qtyCost = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 4, 0, 0) };
            qtyCost.Controls.AddRange([numQty, numCost]);
            Row("Qty / Cost", qtyCost, 4);

            var lineButtons = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 6, 0, 0) };
            var btnAddLine = MakeButton("Add Line", Color.FromArgb(41, 128, 185));
            btnAddLine.Margin = new Padding(0, 0, 8, 0);
            btnAddLine.Click += (_, _) => AddLine();
            var btnRemoveLine = MakeButton("Remove Line", Color.FromArgb(192, 57, 43));
            btnRemoveLine.Click += (_, _) => RemoveLine();
            lineButtons.Controls.AddRange([btnAddLine, btnRemoveLine]);
            form.Controls.Add(lineButtons, 1, 5);

            cmbProduct.SelectedIndexChanged += (_, _) => SuggestBatch();

            var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

            dgv.Dock = DockStyle.Fill;
            GridStyler.Apply(dgv);

            lblTotal.Font = new Font("Segoe UI", 14f, FontStyle.Bold);
            lblTotal.ForeColor = Color.FromArgb(41, 128, 185);
            lblTotal.Dock = DockStyle.Fill;
            lblTotal.TextAlign = ContentAlignment.MiddleLeft;

            right.Controls.Add(dgv, 0, 0);
            right.Controls.Add(lblTotal, 0, 1);

            split.Controls.Add(form, 0, 0);
            split.Controls.Add(right, 1, 0);

            var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 56, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
            var btnSave = MakeButton("Save Purchase", Color.FromArgb(39, 174, 96));
            btnSave.Size = new Size(180, 44);
            btnSave.Click += async (_, _) => await SaveAsync();
            bottom.Controls.Add(btnSave);

            Controls.Add(split);
            Controls.Add(bottom);
            Load += async (_, _) => await InitAsync();
        }

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
