using pharmacy.Enums;
using pharmacy.Forms.Batches;
using pharmacy.Forms.Categories;
using pharmacy.Forms.Products;
using pharmacy.Forms.Purchases;
using pharmacy.Forms.Reports;
using pharmacy.Forms.Sales;
using pharmacy.Forms.Suppliers;
using pharmacy.Forms.Users;
using pharmacy.Helpers;
using pharmacy.Models;
using pharmacy.UserControls;

namespace pharmacy.Forms
{
    public class FrmMain : BaseForm
    {
        private readonly User _user;
        private readonly Panel pnlContent = new();
        private readonly MenuStrip menu = new();
        private readonly StatusStrip status = new();
        private readonly ToolStripStatusLabel lblUser = new();
        private readonly UcDashboardCard ucDashboard = new();
        private readonly DataGridView dgvRecent = new();
        private readonly Label lblRecentHeader = new();
        private List<Sale> _recentSales = new();
        private bool _dashboardBuilt;

        public FrmMain(User user)
        {
            _user = user;
            Text = $"PharmacyMS — {user.FullName} ({user.Role})";
            WindowState = FormWindowState.Maximized;
            IsMdiContainer = true;

            BuildMenu();
            pnlContent.Dock = DockStyle.Fill;
            pnlContent.BackColor = Color.FromArgb(236, 240, 241);
            pnlContent.Padding = new Padding(10);

            lblUser.Text = $"{user.FullName} | {user.Role}";
            status.Items.Add(lblUser);
            status.Dock = DockStyle.Bottom;

            Controls.Add(pnlContent);
            Controls.Add(status);
            Controls.Add(menu);
            MainMenuStrip = menu;

            KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.F2) OpenForm(new FrmPos(AppServices.Sales, AppServices.ProductRepo, AppServices.CustomerRepo, _user));
                if (e.KeyCode == Keys.F4) Activate();
            };

            ShowDashboard();
            ucDashboard.CardOpened += OpenDashboardCard;
        }

        private void OpenDashboardCard(int card)
        {
            switch (card)
            {
                case 0: OpenForm(new FrmSaleList(AppServices.Sales)); break;
                case 1: OpenForm(new FrmStockReport(AppServices.Reports)); break;
                case 2:
                case 3: OpenForm(new FrmExpiryReport(AppServices.Batches)); break;
            }
        }

        private void BuildMenu()
        {
            menu.Dock = DockStyle.Top;

            var mDashboard = new ToolStripMenuItem("Dashboard");
            mDashboard.Click += (_, _) => ShowDashboard();

            var mProducts = new ToolStripMenuItem("Products");
            mProducts.DropDownItems.Add("Product List", null, (_, _) => OpenForm(new FrmProductList(AppServices.ProductRepo, AppServices.CategoryRepo)));
            mProducts.DropDownItems.Add("Categories", null, (_, _) => OpenForm(new FrmCategoryList(AppServices.CategoryRepo)));
            mProducts.DropDownItems.Add("Batches", null, (_, _) => OpenForm(new FrmBatchList(AppServices.Batches)));
            mProducts.DropDownItems.Add("Expiry Alerts", null, (_, _) => OpenForm(new FrmExpiryAlerts(AppServices.Batches)));

            var mPartners = new ToolStripMenuItem("Partners");
            mPartners.DropDownItems.Add("Suppliers", null, (_, _) => OpenForm(new FrmSupplierList(AppServices.SupplierRepo)));
            mPartners.DropDownItems.Add("Customers", null, (_, _) => OpenForm(new FrmCustomerList(AppServices.CustomerRepo)));

            var mPurchases = new ToolStripMenuItem("Purchases");
            mPurchases.DropDownItems.Add("Purchase List", null, (_, _) => OpenForm(new FrmPurchaseList(AppServices.Purchases)));
            mPurchases.DropDownItems.Add("New Purchase", null, (_, _) => OpenForm(new FrmPurchaseNew(AppServices.Purchases, AppServices.ProductRepo, AppServices.SupplierRepo, AppServices.CategoryRepo, _user)));

            var mSales = new ToolStripMenuItem("Sales");
            mSales.DropDownItems.Add("POS (F2)", null, (_, _) => OpenForm(new FrmPos(AppServices.Sales, AppServices.ProductRepo, AppServices.CustomerRepo, _user)));
            mSales.DropDownItems.Add("Sale List", null, (_, _) => OpenForm(new FrmSaleList(AppServices.Sales)));

            var mReports = new ToolStripMenuItem("Reports");
            mReports.DropDownItems.Add("Sales Report", null, (_, _) => OpenForm(new FrmSalesReport(AppServices.Reports, AppServices.Sales)));
            mReports.DropDownItems.Add("Stock Report", null, (_, _) => OpenForm(new FrmStockReport(AppServices.Reports)));
            mReports.DropDownItems.Add("Profit Report", null, (_, _) => OpenForm(new FrmProfitReport(AppServices.Reports)));
            mReports.DropDownItems.Add("Expiry Report", null, (_, _) => OpenForm(new FrmExpiryReport(AppServices.Batches)));

            var mUsers = new ToolStripMenuItem("Users");
            mUsers.DropDownItems.Add("User List", null, (_, _) => OpenForm(new FrmUserList(AppServices.UserRepo, AppServices.Auth)));

            var mLogout = new ToolStripMenuItem("Logout");
            mLogout.Click += (_, _) => Close();

            menu.Items.Add(mDashboard);

            if (_user.Role == UserRole.Cashier)
            {
                menu.Items.Add(mSales);
                menu.Items.Add(mLogout);
                return;
            }

            menu.Items.Add(mProducts);
            menu.Items.Add(mPartners);
            menu.Items.Add(mPurchases);
            menu.Items.Add(mSales);
            menu.Items.Add(mReports);
            if (_user.Role == UserRole.Admin) menu.Items.Add(mUsers);
            menu.Items.Add(mLogout);
        }

        private void ShowDashboard()
        {
            pnlContent.Controls.Clear();
            if (!_dashboardBuilt)
            {
                lblRecentHeader.Text = "Recent Sales (double-click to view)";
                lblRecentHeader.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
                lblRecentHeader.ForeColor = Color.FromArgb(45, 62, 80);
                lblRecentHeader.Dock = DockStyle.Top;
                lblRecentHeader.Height = 32;
                lblRecentHeader.Padding = new Padding(4, 8, 0, 0);

                dgvRecent.Dock = DockStyle.Fill;
                GridStyler.Apply(dgvRecent);
                dgvRecent.CellDoubleClick += async (_, _) => await ViewRecentSaleAsync();

                _dashboardBuilt = true;
            }

            ucDashboard.Dock = DockStyle.Top;
            ucDashboard.Height = 140;
            pnlContent.Controls.Add(dgvRecent);
            pnlContent.Controls.Add(lblRecentHeader);
            pnlContent.Controls.Add(ucDashboard);
            _ = RefreshDashboardAsync();
        }

        private async Task RefreshDashboardAsync()
        {
            await ucDashboard.RefreshAsync();
            var result = await AppServices.Sales.GetRecentAsync(10);
            if (!result.IsSuccess || result.Value is null) return;
            _recentSales = result.Value;
            dgvRecent.DataSource = _recentSales.Select(s => new
            {
                s.Id,
                s.SaleDate,
                Cashier = s.User?.FullName,
                Customer = s.Customer?.Name ?? "Walk-in",
                s.TotalAmount,
                s.Discount,
                s.PaidAmount,
                Payment = s.PaymentMethod.ToString()
            }).ToList();
            if (dgvRecent.Columns["Id"] != null) dgvRecent.Columns["Id"].Visible = false;
        }

        private async Task ViewRecentSaleAsync()
        {
            if (dgvRecent.CurrentRow?.Cells["Id"]?.Value is not int id) return;
            var result = await AppServices.Sales.GetByIdAsync(id);
            if (result.IsSuccess && result.Value is not null)
            {
                var dlg = new FrmSaleView(result.Value);
                dlg.ShowDialog(this);
                dlg.Dispose();
            }
        }

        private void OpenForm(Form form) => FormLoader.ShowChild(pnlContent, form);
    }
}
