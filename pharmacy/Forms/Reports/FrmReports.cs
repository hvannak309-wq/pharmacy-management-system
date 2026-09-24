using pharmacy.Forms.Sales;
using pharmacy.Helpers;
using pharmacy.Interfaces;
using pharmacy.Models;

namespace pharmacy.Forms.Reports
{
    public class FrmSalesReport : BaseForm
    {
        private readonly IReportService _reports;
        private readonly ISaleService _sales;
        private readonly DateTimePicker dtFrom = new() { Format = DateTimePickerFormat.Short };
        private readonly DateTimePicker dtTo = new() { Format = DateTimePickerFormat.Short };
        private readonly DataGridView dgv = new();
        private readonly Label lblTotal = new();

        public FrmSalesReport(IReportService reports, ISaleService sales)
        {
            _reports = reports;
            _sales = sales;
            Text = "Sales Report";
            ClientSize = new Size(900, 500);
            dtFrom.Value = DateTime.Today.AddDays(-30);
            dtTo.Value = DateTime.Today;

            var bar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 48,
                Padding = new Padding(8, 12, 8, 0),
                WrapContents = false
            };
            var lblFrom = new Label { Text = "From:", AutoSize = true, Margin = new Padding(0, 6, 4, 0) };
            dtFrom.Width = 120;
            dtFrom.Margin = new Padding(0, 4, 12, 0);
            var lblTo = new Label { Text = "To:", AutoSize = true, Margin = new Padding(0, 6, 4, 0) };
            dtTo.Width = 120;
            dtTo.Margin = new Padding(0, 4, 16, 0);
            dtFrom.ValueChanged += async (_, _) => await RunAsync();
            dtTo.ValueChanged += async (_, _) => await RunAsync();

            lblTotal.AutoSize = true;
            lblTotal.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            lblTotal.Margin = new Padding(0, 4, 0, 0);

            bar.Controls.AddRange([lblFrom, dtFrom, lblTo, dtTo, lblTotal]);

            dgv.Dock = DockStyle.Fill;
            GridStyler.Apply(dgv);
            dgv.CellDoubleClick += async (_, _) => await ViewSaleAsync();

            Controls.Add(dgv);
            Controls.Add(bar);
            Load += async (_, _) => await RunAsync();
        }

        private async Task RunAsync()
        {
            var result = await _reports.GetSalesReportAsync(dtFrom.Value.Date, dtTo.Value.Date);
            if (!result.IsSuccess || result.Value is null) { Warn(result.Error ?? "Failed."); return; }
            var sales = result.Value;
            dgv.DataSource = sales.Select(s => new
            {
                s.Id,
                s.SaleDate,
                Cashier = s.User?.FullName,
                Customer = s.Customer?.Name ?? "Walk-in",
                Items = s.Details.Sum(d => d.Quantity),
                s.TotalAmount,
                s.Discount,
                s.PaidAmount,
                Payment = s.PaymentMethod.ToString()
            }).ToList();
            if (dgv.Columns["Id"] != null) dgv.Columns["Id"].Visible = false;
            lblTotal.Text = $"Sales: {sales.Count}  Total: {sales.Sum(s => s.TotalAmount):0.00}";
        }

        private async Task ViewSaleAsync()
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

    public class FrmStockReport : BaseForm
    {
        private readonly IReportService _reports;
        private readonly DataGridView dgv = new();
        private List<Product> _rows = new();

        public FrmStockReport(IReportService reports)
        {
            _reports = reports;
            Text = "Stock Report";
            ClientSize = new Size(900, 500);

            var btnRun = MakeButton("Refresh", Color.FromArgb(41, 128, 185));
            btnRun.Location = new Point(12, 12);
            btnRun.Click += async (_, _) => await RunAsync();

            dgv.Location = new Point(12, 55);
            dgv.Size = new Size(870, 430);
            GridStyler.Apply(dgv);
            dgv.CellDoubleClick += (_, _) => ViewProduct();
            dgv.CellFormatting += (_, e) =>
            {
                if (e.RowIndex < 0) return;
                var stock = Convert.ToInt32(dgv.Rows[e.RowIndex].Cells["Stock"]?.Value ?? 0);
                var reorder = Convert.ToInt32(dgv.Rows[e.RowIndex].Cells["ReorderLevel"]?.Value ?? 0);
                if (stock <= reorder && e.CellStyle is not null)
                {
                    e.CellStyle.BackColor = Color.FromArgb(231, 76, 60);
                    e.CellStyle.ForeColor = Color.White;
                }
            };

            Controls.AddRange([btnRun, dgv]);
            Load += async (_, _) => await RunAsync();
        }

        private async Task RunAsync()
        {
            var result = await _reports.GetStockReportAsync();
            if (!result.IsSuccess || result.Value is null) { Warn(result.Error ?? "Failed."); return; }
            _rows = result.Value;
            dgv.DataSource = _rows.Select(p => new
            {
                p.Id,
                p.Name,
                p.Barcode,
                Category = p.Category?.Name,
                p.UnitPrice,
                p.CostPrice,
                Stock = p.Stock,
                p.ReorderLevel,
                ActiveBatches = p.Batches.Count(b => !b.IsDisposed && b.Quantity > 0)
            }).ToList();
            if (dgv.Columns["Id"] != null) dgv.Columns["Id"].Visible = false;
        }

        private void ViewProduct()
        {
            if (dgv.CurrentRow?.Index is not int i || i < 0 || i >= _rows.Count) return;
            var p = _rows[i];
            var batches = p.Batches
                .Where(b => !b.IsDisposed)
                .OrderBy(b => b.ExpiryDate)
                .Select(b => $"  {b.BatchNumber}  exp {b.ExpiryDate:yyyy-MM-dd}  qty {b.Quantity}")
                .ToList();
            var text = $"{p.Name}\nBarcode: {p.Barcode}\nCategory: {p.Category?.Name}\n" +
                       $"Stock: {p.Stock}  Reorder: {p.ReorderLevel}\n" +
                       $"Price: {p.UnitPrice:0.00}  Cost: {p.CostPrice:0.00}\n\n" +
                       $"Batches:\n{(batches.Count > 0 ? string.Join("\n", batches) : "  (none)")}";
            MessageBox.Show(text, "Product Detail", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    public class FrmProfitReport : BaseForm
    {
        private readonly IReportService _reports;
        private readonly DateTimePicker dtFrom = new() { Format = DateTimePickerFormat.Short };
        private readonly DateTimePicker dtTo = new() { Format = DateTimePickerFormat.Short };
        private readonly DataGridView dgv = new();
        private readonly Label lblTotal = new();

        public FrmProfitReport(IReportService reports)
        {
            _reports = reports;
            Text = "Profit Report (Revenue − Cost)";
            ClientSize = new Size(900, 500);
            dtFrom.Value = DateTime.Today.AddDays(-30);
            dtTo.Value = DateTime.Today;

            var bar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 48,
                Padding = new Padding(8, 12, 8, 0),
                WrapContents = false
            };
            var lblFrom = new Label { Text = "From:", AutoSize = true, Margin = new Padding(0, 6, 4, 0) };
            dtFrom.Width = 120;
            dtFrom.Margin = new Padding(0, 4, 12, 0);
            var lblTo = new Label { Text = "To:", AutoSize = true, Margin = new Padding(0, 6, 4, 0) };
            dtTo.Width = 120;
            dtTo.Margin = new Padding(0, 4, 16, 0);
            dtFrom.ValueChanged += async (_, _) => await RunAsync();
            dtTo.ValueChanged += async (_, _) => await RunAsync();

            lblTotal.AutoSize = true;
            lblTotal.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            lblTotal.ForeColor = Color.FromArgb(39, 174, 96);
            lblTotal.Margin = new Padding(0, 4, 0, 0);

            bar.Controls.AddRange([lblFrom, dtFrom, lblTo, dtTo, lblTotal]);

            dgv.Dock = DockStyle.Fill;
            GridStyler.Apply(dgv);

            Controls.Add(dgv);
            Controls.Add(bar);
            Load += async (_, _) => await RunAsync();
        }

        private async Task RunAsync()
        {
            var result = await _reports.GetProfitReportAsync(dtFrom.Value.Date, dtTo.Value.Date);
            if (!result.IsSuccess || result.Value is null) { Warn(result.Error ?? "Failed."); return; }
            var rows = result.Value;
            dgv.DataSource = rows;
            lblTotal.Text = $"Revenue: {rows.Sum(r => r.Revenue):0.00}  Cost: {rows.Sum(r => r.Cost):0.00}  Profit: {rows.Sum(r => r.Profit):0.00}";
        }
    }

    public class FrmExpiryReport : BaseForm
    {
        private readonly IBatchService _batches;
        private readonly DataGridView dgv = new();
        private List<Batch> _rows = new();

        public FrmExpiryReport(IBatchService batches)
        {
            _batches = batches;
            Text = "Expiry Report";
            ClientSize = new Size(900, 500);

            var btnRun = MakeButton("Refresh", Color.FromArgb(41, 128, 185));
            btnRun.Location = new Point(12, 12);
            btnRun.Click += async (_, _) => await RunAsync();

            dgv.Location = new Point(12, 55);
            dgv.Size = new Size(870, 430);
            GridStyler.Apply(dgv);
            dgv.CellDoubleClick += (_, _) => ViewBatch();

            Controls.AddRange([btnRun, dgv]);
            Load += async (_, _) => await RunAsync();
        }

        private async Task RunAsync()
        {
            var result = await _batches.GetExpiryAlertsAsync();
            if (!result.IsSuccess || result.Value is null) { Warn(result.Error ?? "Failed."); return; }
            var today = DateTime.Today;
            _rows = result.Value.OrderBy(b => b.ExpiryDate).ToList();
            dgv.DataSource = _rows.Select(b => new
            {
                Product = b.Product?.Name ?? $"#{b.ProductId}",
                b.BatchNumber,
                b.ExpiryDate,
                DaysLeft = (b.ExpiryDate.Date - today).Days,
                b.Quantity
            }).ToList();
        }

        private void ViewBatch()
        {
            if (dgv.CurrentRow?.Index is not int i || i < 0 || i >= _rows.Count) return;
            var b = _rows[i];
            var days = (b.ExpiryDate.Date - DateTime.Today).Days;
            var text = $"Product: {b.Product?.Name ?? $"#{b.ProductId}"}\n" +
                       $"Batch: {b.BatchNumber}\nExpiry: {b.ExpiryDate:yyyy-MM-dd} ({days} days)\n" +
                       $"Quantity: {b.Quantity}\nDisposed: {(b.IsDisposed ? "Yes" : "No")}";
            MessageBox.Show(text, "Batch Detail", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
