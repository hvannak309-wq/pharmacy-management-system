namespace pharmacy.Forms
{
    public class BaseForm : Form
    {
        public BaseForm()
        {
            Font = new Font("Segoe UI", 9.75f);
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;
        }

        protected void StyleHeader(Label lbl)
        {
            lbl.Font = new Font("Segoe UI", 14f, FontStyle.Bold);
            lbl.ForeColor = Color.FromArgb(45, 62, 80);
        }

        protected Button MakeButton(string text, Color back)
        {
            var b = new Button
            {
                Text = text,
                BackColor = back,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.75f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Height = 36,
                Width = 110
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        protected void OkCancel(params Button[] buttons)
        {
            foreach (var b in buttons) AcceptButton = b;
        }

        protected static void Alert(string message, string title = "PharmacyMS")
            => MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);

        protected static void Warn(string message, string title = "PharmacyMS")
            => MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);

        protected static bool Confirm(string message)
            => MessageBox.Show(message, "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
    }
}
