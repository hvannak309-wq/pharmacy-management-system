using pharmacy.DTOs;
using pharmacy.Helpers;
using pharmacy.Interfaces;
using pharmacy.Models;

namespace pharmacy.Forms.Sales
{
    public partial class FrmPos : BaseForm
    {
        private readonly ISaleService _sales;
        private readonly IProductRepository _products;
        private readonly IRepository<Customer> _customers;
        private readonly User _user;

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
            InitializeComponent();
            _sales = sales;
            _products = products;
            _customers = customers;
            _user = user;
            GridStyler.Apply(dgvSearch);
            GridStyler.Apply(dgvCart);
            cmbPayment.DataSource = Enum.GetValues<Enums.PaymentMethod>();
        }

        private async void txtSearch_TextChanged(object? sender, EventArgs e) => await SearchAsync();

        private void txtSearch_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                if (dgvSearch.Rows.Count > 0) AddSelected();
            }
        }

        private void dgvSearch_CellDoubleClick(object? sender, DataGridViewCellEventArgs e) => AddSelected();

        private void dgvSearch_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && dgvSearch.CurrentRow is not null)
            {
                e.SuppressKeyPress = true;
                AddSelected();
            }
        }

        private void btnAdd_Click(object? sender, EventArgs e) => AddSelected();

        private void numDiscount_ValueChanged(object? sender, EventArgs e) => UpdateTotal();

        private void btnRemove_Click(object? sender, EventArgs e) => RemoveSelected();

        private void btnClear_Click(object? sender, EventArgs e)
        {
            _cart.Clear();
            RefreshCart();
        }

        private async void btnCheckout_Click(object? sender, EventArgs e) => await CheckoutAsync();

        private async void FrmPos_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F4) { txtSearch.Focus(); txtSearch.SelectAll(); }
            if (e.KeyCode == Keys.F8) await CheckoutAsync();
        }

        private async void FrmPos_Shown(object? sender, EventArgs e)
        {
            await LoadCustomersAsync();
            await LoadAllProductsAsync();
            txtSearch.Focus();
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

    public partial class FrmSaleList : BaseForm
    {
        private readonly ISaleService _sales;

        public FrmSaleList(ISaleService sales)
        {
            InitializeComponent();
            _sales = sales;
            GridStyler.Apply(dgv);
        }

        private async void btnRefresh_Click(object? sender, EventArgs e) => await LoadDataAsync();

        private async void btnView_Click(object? sender, EventArgs e) => await ViewAsync();

        private async void dgv_CellDoubleClick(object? sender, DataGridViewCellEventArgs e) => await ViewAsync();

        private async void FrmSaleList_Load(object? sender, EventArgs e) => await LoadDataAsync();

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

    public partial class FrmSaleView : BaseForm
    {
        public FrmSaleView(Sale sale)
        {
            InitializeComponent();
            Text = $"Sale #{sale.Id}";
            info.Text = $"Date: {sale.SaleDate:g}\nCustomer: {sale.Customer?.Name ?? "Walk-in"}\nCashier: {sale.User?.FullName}\n" +
                       $"Subtotal: {sale.Details.Sum(d => d.Subtotal):0.00}  Discount: {sale.Discount:0.00}  Total: {sale.TotalAmount:0.00}  Paid: {sale.PaidAmount:0.00}\n" +
                       $"Payment: {sale.PaymentMethod}";
            GridStyler.Apply(dgv);
            dgv.DataSource = sale.Details.Select(d => new
            {
                d.Product?.Name,
                Batch = d.Batch?.BatchNumber,
                d.Quantity,
                d.UnitPrice,
                d.Subtotal
            }).ToList();
        }

        private void btnClose_Click(object? sender, EventArgs e) => Close();
    }
}
