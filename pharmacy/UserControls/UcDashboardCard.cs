using pharmacy.Helpers;

namespace pharmacy.UserControls
{
    public class UcDashboardCard : UserControl
    {
        private readonly Label lblSales = CardLabel();
        private readonly Label lblLowStock = CardLabel();
        private readonly Label lblExpiring = CardLabel();
        private readonly Label lblExpired = CardLabel();

        public event Action<int>? CardOpened;

        public UcDashboardCard()
        {
            BackColor = Color.FromArgb(236, 240, 241);
            Height = 140;
            Dock = DockStyle.Top;

            Controls.Add(MakeCard("Today's Sales", lblSales, 0));
            Controls.Add(MakeCard("Low Stock", lblLowStock, 1));
            Controls.Add(MakeCard("Expiring ≤90d", lblExpiring, 2));
            Controls.Add(MakeCard("Expired", lblExpired, 3));
        }

        private static Label CardLabel() => new()
        {
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            ForeColor = Color.FromArgb(41, 128, 185),
            Dock = DockStyle.Bottom,
            Height = 50,
            TextAlign = ContentAlignment.MiddleCenter,
            Text = "—"
        };

        private Panel MakeCard(string title, Label value, int index)
        {
            var card = new Panel
            {
                BackColor = Color.White,
                Location = new Point(index * 230, 10),
                Size = new Size(215, 110),
                Cursor = Cursors.Hand
            };
            var lbl = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9.75f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 100, 100),
                Dock = DockStyle.Top,
                Height = 35,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            var copy = new Label
            {
                Font = value.Font,
                ForeColor = value.ForeColor,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "—",
                Name = $"val{index}",
                Cursor = Cursors.Hand
            };
            void Open(object? s, EventArgs e) => CardOpened?.Invoke(index);
            card.DoubleClick += Open;
            lbl.DoubleClick += Open;
            copy.DoubleClick += Open;
            card.Controls.Add(copy);
            card.Controls.Add(lbl);
            return card;
        }

        public async Task RefreshAsync()
        {
            var result = await AppServices.Dashboard.GetAsync();
            if (!result.IsSuccess || result.Value is null) return;
            var d = result.Value;
            SetVal(0, $"{d.TodaySales:C}  ·  {d.TodaySaleCount} sales");
            SetVal(1, d.LowStockCount.ToString());
            SetVal(2, d.ExpiringSoonCount.ToString());
            SetVal(3, d.ExpiredCount.ToString());
        }

        private void SetVal(int index, string text)
        {
            var card = Controls.OfType<Panel>().ElementAtOrDefault(index);
            var lbl = card?.Controls.OfType<Label>().FirstOrDefault(l => l.Name == $"val{index}");
            if (lbl is not null) lbl.Text = text;
        }
    }
}
