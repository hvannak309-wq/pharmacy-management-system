using pharmacy.Helpers;
using pharmacy.Interfaces;
using pharmacy.Models;

namespace pharmacy.Forms.Batches
{
    public partial class FrmBatchList : BaseForm
    {
        private readonly IBatchService _batches;

        public FrmBatchList(IBatchService batches)
        {
            InitializeComponent();
            _batches = batches;
            GridStyler.Apply(dgv);
        }

        private async void chkHideDisposed_CheckedChanged(object? sender, EventArgs e) => await LoadDataAsync();

        private async void btnRefresh_Click(object? sender, EventArgs e) => await LoadDataAsync();

        private async void btnDispose_Click(object? sender, EventArgs e) => await DisposeAsync();

        private void dgv_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e) => ColorRow(e);

        private async void FrmBatchList_Load(object? sender, EventArgs e) => await LoadDataAsync();

        private async Task LoadDataAsync()
        {
            var result = await _batches.GetAllAsync();
            if (!result.IsSuccess || result.Value is null) return;
            var list = result.Value;
            if (chkHideDisposed.Checked) list = list.Where(b => !b.IsDisposed).ToList();

            var today = DateTime.Today;
            dgv.DataSource = list.OrderBy(b => b.ExpiryDate).Select(b => new
            {
                b.Id,
                Product = b.Product?.Name ?? $"#{b.ProductId}",
                b.BatchNumber,
                b.ExpiryDate,
                b.Quantity,
                b.PurchasePrice,
                b.IsDisposed,
                Status = b.IsDisposed ? "Disposed"
                    : b.ExpiryDate < today ? "EXPIRED"
                    : (b.ExpiryDate - today).TotalDays <= 30 ? "≤30d"
                    : (b.ExpiryDate - today).TotalDays <= 60 ? "≤60d"
                    : (b.ExpiryDate - today).TotalDays <= 90 ? "≤90d"
                    : "OK"
            }).ToList();
            if (dgv.Columns["Id"] != null) dgv.Columns["Id"].Visible = false;
        }

        private void ColorRow(DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.CellStyle is null) return;
            var status = dgv.Rows[e.RowIndex].Cells["Status"]?.Value?.ToString();
            if (status is "EXPIRED" or "≤30d" or "≤60d")
            {
                e.CellStyle.BackColor = status switch
                {
                    "EXPIRED" => Color.FromArgb(231, 76, 60),
                    "≤30d" => Color.FromArgb(243, 156, 18),
                    _ => Color.FromArgb(241, 196, 15)
                };
                e.CellStyle.ForeColor = Color.White;
            }
        }

        private async Task DisposeAsync()
        {
            if (dgv.CurrentRow?.Cells["Id"]?.Value is not int id) { Warn("Select a batch."); return; }
            if (!Confirm("Dispose selected batch?")) return;
            var result = await _batches.DisposeAsync(id);
            if (!result.IsSuccess) { Warn(result.Error ?? "Failed."); return; }
            await LoadDataAsync();
        }
    }

    public partial class FrmExpiryAlerts : BaseForm
    {
        private readonly IBatchService _batches;

        public FrmExpiryAlerts(IBatchService batches)
        {
            InitializeComponent();
            _batches = batches;
            GridStyler.Apply(dgv);
        }

        private async void btnRefresh_Click(object? sender, EventArgs e) => await LoadDataAsync();

        private async void btnDispose_Click(object? sender, EventArgs e) => await DisposeAsync();

        private void dgv_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.CellStyle is null) return;
            var bucket = dgv.Rows[e.RowIndex].Cells["Bucket"]?.Value?.ToString();
            if (bucket is "Expired" or "30d" or "60d")
            {
                e.CellStyle.BackColor = bucket switch
                {
                    "Expired" => Color.FromArgb(231, 76, 60),
                    "30d" => Color.FromArgb(243, 156, 18),
                    _ => Color.FromArgb(241, 196, 15)
                };
                e.CellStyle.ForeColor = Color.White;
            }
        }

        private async void FrmExpiryAlerts_Load(object? sender, EventArgs e) => await LoadDataAsync();

        private async Task LoadDataAsync()
        {
            var result = await _batches.GetExpiryAlertsAsync();
            if (!result.IsSuccess || result.Value is null) return;
            var today = DateTime.Today;
            var rows = result.Value.Select(b => new
            {
                b.Id,
                Product = b.Product?.Name ?? $"#{b.ProductId}",
                b.BatchNumber,
                b.ExpiryDate,
                DaysLeft = (b.ExpiryDate.Date - today).Days,
                b.Quantity,
                Bucket = b.ExpiryDate < today ? "Expired"
                    : (b.ExpiryDate - today).TotalDays <= 30 ? "30d"
                    : (b.ExpiryDate - today).TotalDays <= 60 ? "60d"
                    : "90d"
            }).OrderBy(r => r.DaysLeft).ToList();

            dgv.DataSource = rows;
            if (dgv.Columns["Id"] != null) dgv.Columns["Id"].Visible = false;
            lblSummary.Text = $"Expired: {rows.Count(r => r.Bucket == "Expired")}  |  ≤30d: {rows.Count(r => r.Bucket == "30d")}  |  ≤60d: {rows.Count(r => r.Bucket == "60d")}  |  ≤90d: {rows.Count(r => r.Bucket == "90d")}";
        }

        private async Task DisposeAsync()
        {
            if (dgv.CurrentRow?.Cells["Id"]?.Value is not int id) { Warn("Select a batch."); return; }
            if (!Confirm("Dispose selected batch?")) return;
            var result = await _batches.DisposeAsync(id);
            if (!result.IsSuccess) { Warn(result.Error ?? "Failed."); return; }
            await LoadDataAsync();
        }
    }
}
