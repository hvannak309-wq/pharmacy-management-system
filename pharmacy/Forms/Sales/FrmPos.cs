using pharmacy.DTOs;
using pharmacy.Helpers;
using pharmacy.Interfaces;
using pharmacy.Models;

namespace pharmacy.Forms.Sales
{
    public class FrmPos : BaseForm
    {
        private readonly ISaleService _sales;
        private readonly IProductRepository _products;
        private readonly IRepository<Customer> _customers;
        private readonly User _user;

        private readonly TextBox txtSearch = new();
        private readonly DataGridView dgvSearch = new();
        private readonly DataGridView dgvCart = new();
        private readonly NumericUpDown numQty = new() { Minimum = 1, Maximum = 9999, Value = 1 };
        private readonly Label lblTotal = new();
        private readonly ComboBox cmbPayment = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ComboBox cmbCustomer = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly NumericUpDown numDiscount = new() { Maximum = 100000, DecimalPlaces = 2 };
        private readonly NumericUpDown numPaid = new() { Maximum = 100000, DecimalPlaces = 2 };

        private readonly List<CartLine> _cart = new();

        private sealed class CartLine
        {
            public int ProductId { get; init; }
            public string Name { get; init; } = "";
            public int Quantity { get; set; }
            public decimal UnitPrice { get; init; }
            public decimal Subtotal => Quantity * UnitPrice;
        }

        public FrmPos(ISaleService sales, IProductRepository products, IRepository<Customer> customers, User user)
        {
            _sales = sales;
            _products = products;
            _customers = customers;
            _user = user;
            Text = "Point of Sale";
            ClientSize = new Size(1100, 680);
            MinimumSize = new Size(900, 560);

            var topBar = new Panel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(8, 12, 8, 0) };
            var lblSearch = new Label { Text = "Scan / search (F4):", Location = new Point(8, 14), AutoSize = true };
            txtSearch.Location = new Point(150, 10);
            txtSearch.Width = 360;
            txtSearch.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtSearch.TextChanged += async (_, _) => await SearchAsync();
            txtSearch.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    if (dgvSearch.Rows.Count > 0) AddSelected();
                }
            };
            topBar.Controls.AddRange([lblSearch, txtSearch]);

            var split = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(4)
            };
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            var left = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 };
            left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            left.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            left.RowStyles.Add(new RowStyle(SizeType.Absolute, 140));

            dgvSearch.Dock = DockStyle.Fill;
            GridStyler.Apply(dgvSearch);
            dgvSearch.CellDoubleClick += (_, _) => AddSelected();
            dgvSearch.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter && dgvSearch.CurrentRow is not null)
                {
                    e.SuppressKeyPress = true;
                    AddSelected();
                }
            };

            var addPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 6, 0, 0) };
            var lblQty = new Label { Text = "Qty:", AutoSize = true, Margin = new Padding(0, 8, 4, 0) };
            numQty.Width = 70;
            var btnAdd = MakeButton("Add (Enter)", Color.FromArgb(41, 128, 185));
            btnAdd.Margin = new Padding(8, 2, 0, 0);
            btnAdd.Click += (_, _) => AddSelected();
            addPanel.Controls.AddRange([lblQty, numQty, btnAdd]);

            var bottomLeft = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 4, Padding = new Padding(0, 4, 8, 0) };
            bottomLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            bottomLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            bottomLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            bottomLeft.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            bottomLeft.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            bottomLeft.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            bottomLeft.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            bottomLeft.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            cmbPayment.Width = 120;
            cmbPayment.DataSource = Enum.GetValues<Enums.PaymentMethod>();
            cmbCustomer.Width = 160;
            numDiscount.Width = 100;
            numDiscount.ValueChanged += (_, _) => UpdateTotal();
            numPaid.Width = 100;

            bottomLeft.Controls.Add(new Label { Text = "Payment:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
            bottomLeft.Controls.Add(cmbPayment, 1, 0);
            bottomLeft.Controls.Add(new Label { Text = "Customer:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
            bottomLeft.Controls.Add(cmbCustomer, 1, 1);
            bottomLeft.Controls.Add(new Label { Text = "Discount:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
            bottomLeft.Controls.Add(numDiscount, 1, 2);
            bottomLeft.Controls.Add(new Label { Text = "Paid:", AutoSize = true, Anchor = AnchorStyles.Left }, 2, 2);
            bottomLeft.Controls.Add(numPaid, 3, 2);

            lblTotal.Text = "Total: 0.00";
            lblTotal.Font = new Font("Segoe UI", 18f, FontStyle.Bold);
            lblTotal.ForeColor = Color.FromArgb(39, 174, 96);
            lblTotal.Dock = DockStyle.Fill;
            lblTotal.TextAlign = ContentAlignment.MiddleRight;
            bottomLeft.Controls.Add(lblTotal, 0, 3);
            bottomLeft.SetColumnSpan(lblTotal, 4);

            left.Controls.Add(dgvSearch, 0, 0);
            left.Controls.Add(addPanel, 0, 1);
            left.Controls.Add(bottomLeft, 0, 2);

            var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 };
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));

            dgvCart.Dock = DockStyle.Fill;
            GridStyler.Apply(dgvCart);

            var cartBtns = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 6, 0, 0) };
            var btnRemove = MakeButton("Remove Line", Color.FromArgb(231, 76, 60));
            btnRemove.Margin = new Padding(0, 2, 8, 0);
            btnRemove.Click += (_, _) => RemoveSelected();
            var btnClear = MakeButton("Clear Cart", Color.FromArgb(149, 165, 166));
            btnClear.Margin = new Padding(0, 2, 0, 0);
            btnClear.Click += (_, _) => { _cart.Clear(); RefreshCart(); };
            cartBtns.Controls.AddRange([btnRemove, btnClear]);

            var checkoutPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 4, 0, 0) };
            var btnCheckout = MakeButton("Checkout (F8)", Color.FromArgb(39, 174, 96));
            btnCheckout.Size = new Size(220, 48);
            btnCheckout.Click += async (_, _) => await CheckoutAsync();
            checkoutPanel.Controls.Add(btnCheckout);

            right.Controls.Add(dgvCart, 0, 0);
            right.Controls.Add(cartBtns, 0, 1);
            right.Controls.Add(checkoutPanel, 0, 2);

            split.Controls.Add(left, 0, 0);
            split.Controls.Add(right, 1, 0);

            Controls.Add(split);
            Controls.Add(topBar);

            KeyDown += async (_, e) =>
            {
                if (e.KeyCode == Keys.F4) { txtSearch.Focus(); txtSearch.SelectAll(); }
                if (e.KeyCode == Keys.F8) await CheckoutAsync();
            };

            Shown += async (_, _) =>
            {
                await LoadCustomersAsync();
                await LoadAllProductsAsync();
                txtSearch.Focus();
            };
        }

        private async Task LoadCustomersAsync()
        {
            cmbCustomer.Items.Clear();
            cmbCustomer.Items.Add("Walk-in");
            var list = await _customers.GetAllAsync();
            foreach (var c in list) cmbCustomer.Items.Add($"{c.Id}|{c.Name}");
            cmbCustomer.SelectedIndex = 0;
        }

        private async Task LoadAllProductsAsync()
        {
            var list = await _products.GetWithStockAsync();
            BindProducts(list.Take(100));
        }

        private async Task SearchAsync()
        {
            var term = txtSearch.Text.Trim();
            if (term.Length == 0) { await LoadAllProductsAsync(); return; }
            var list = await _products.SearchAsync(term);
            BindProducts(list.Take(50));
        }

        private void BindProducts(IEnumerable<Product> products)
        {
            dgvSearch.DataSource = products.Select(p => new
            {
                p.Id,
                p.Name,
                p.Barcode,
                p.UnitPrice,
                Stock = p.Stock
            }).ToList();
            if (dgvSearch.Columns["Id"] != null) dgvSearch.Columns["Id"].Visible = false;
        }

        private void AddSelected()
        {
            if (dgvSearch.CurrentRow?.Cells["Id"]?.Value is not int id) return;
            var name = dgvSearch.CurrentRow.Cells["Name"].Value?.ToString() ?? "";
            var price = Convert.ToDecimal(dgvSearch.CurrentRow.Cells["UnitPrice"].Value);
            var qty = (int)numQty.Value;

            var existing = _cart.FirstOrDefault(c => c.ProductId == id);
            if (existing is not null) existing.Quantity += qty;
            else _cart.Add(new CartLine { ProductId = id, Name = name, Quantity = qty, UnitPrice = price });

            numQty.Value = 1;
            RefreshCart();
            txtSearch.Clear();
            txtSearch.Focus();
        }

        private void RemoveSelected()
        {
            if (dgvCart.CurrentRow?.Index is int i && i >= 0 && i < _cart.Count)
            {
                _cart.RemoveAt(i);
                RefreshCart();
            }
        }

        private void RefreshCart()
        {
            dgvCart.DataSource = null;
            dgvCart.DataSource = _cart.Select((c, i) => new { Line = i + 1, c.ProductId, c.Name, c.Quantity, c.UnitPrice, c.Subtotal }).ToList();
            if (dgvCart.Columns["ProductId"] != null) dgvCart.Columns["ProductId"].Visible = false;
            if (dgvCart.Columns["Line"] != null) dgvCart.Columns["Line"].Width = 50;
            UpdateTotal();
        }

        private decimal Total => _cart.Sum(c => c.Subtotal) - numDiscount.Value;

        private void UpdateTotal() => lblTotal.Text = $"Total: {Math.Max(0, Total):0.00}";

        private async Task CheckoutAsync()
        {
            if (_cart.Count == 0) { Warn("Cart empty."); return; }

            int? customerId = null;
            if (cmbCustomer.SelectedItem is string s && s.Contains('|'))
                customerId = int.Parse(s.Split('|')[0]);

            var paid = numPaid.Value;
            if (paid <= 0) paid = Math.Max(0, Total);

            var dto = new CheckoutDto
            {
                UserId = _user.Id,
                CustomerId = customerId,
                Discount = numDiscount.Value,
                PaidAmount = paid,
                PaymentMethod = (Enums.PaymentMethod)(cmbPayment.SelectedItem ?? Enums.PaymentMethod.Cash),
                Lines = _cart.Select(c => new CheckoutLineDto
                {
                    ProductId = c.ProductId,
                    Quantity = c.Quantity,
                    UnitPrice = c.UnitPrice
                }).ToList()
            };

            var result = await _sales.CheckoutAsync(dto);
            if (!result.IsSuccess)
            {
                Warn(result.Error ?? "Checkout failed.");
                return;
            }

            var change = paid - Total;
            Alert($"Sale #{result.Value} complete.\nTotal: {Total:0.00}\nPaid: {paid:0.00}\nChange: {change:0.00}");
            _cart.Clear();
            numDiscount.Value = 0;
            numPaid.Value = 0;
            RefreshCart();
        }
    }

    public class FrmSaleList : BaseForm
    {
        private readonly ISaleService _sales;
        private readonly DataGridView dgv = new();

        public FrmSaleList(ISaleService sales)
        {
            _sales = sales;
            Text = "Sales";
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
            var result = await _sales.GetRecentAsync(200);
            if (!result.IsSuccess || result.Value is null) return;
            dgv.DataSource = result.Value.Select(s => new
            {
                s.Id,
                s.SaleDate,
                s.TotalAmount,
                s.Discount,
                s.PaidAmount,
                Payment = s.PaymentMethod.ToString(),
                Customer = s.Customer?.Name ?? "Walk-in",
                Cashier = s.User?.FullName
            }).ToList();
            if (dgv.Columns["Id"] != null) dgv.Columns["Id"].Visible = false;
        }

        private async Task ViewAsync()
        {
            if (dgv.CurrentRow?.Cells["Id"]?.Value is not int id) return;
            var result = await _sales.GetByIdAsync(id);
            if (result.IsSuccess && result.Value is not null)
            {
                var dlg = new FrmSaleView(result.Value);
                dlg.ShowDialog(this);
                dlg.Dispose();
            }
        }
    }

    public class FrmSaleView : BaseForm
    {
        public FrmSaleView(Sale sale)
        {
            Text = $"Sale #{sale.Id}";
            ClientSize = new Size(700, 460);
            MinimumSize = new Size(500, 350);

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(12) };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

            var info = new Label
            {
                Dock = DockStyle.Fill,
                Text = $"Date: {sale.SaleDate:g}\nCustomer: {sale.Customer?.Name ?? "Walk-in"}\nCashier: {sale.User?.FullName}\n" +
                       $"Subtotal: {sale.Details.Sum(d => d.Subtotal):0.00}  Discount: {sale.Discount:0.00}  Total: {sale.TotalAmount:0.00}  Paid: {sale.PaidAmount:0.00}\n" +
                       $"Payment: {sale.PaymentMethod}"
            };

            var dgv = new DataGridView { Dock = DockStyle.Fill };
            GridStyler.Apply(dgv);
            dgv.DataSource = sale.Details.Select(d => new
            {
                d.Product?.Name,
                Batch = d.Batch?.BatchNumber,
                d.Quantity,
                d.UnitPrice,
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
}
