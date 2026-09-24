using Microsoft.EntityFrameworkCore;
using pharmacy.Helpers;
using pharmacy.Interfaces;
using pharmacy.Models;

namespace pharmacy.Forms.Categories
{
    public class FrmCategoryList : BaseForm
    {
        private readonly IRepository<Category> _repo;
        private readonly DataGridView dgv = new();
        private readonly TextBox txtSearch = new();

        public FrmCategoryList(IRepository<Category> repo)
        {
            _repo = repo;
            Text = "Categories";
            ClientSize = new Size(700, 450);
            MinimumSize = new Size(500, 300);

            var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(8, 8, 8, 0), WrapContents = false };
            var lbl = new Label { Text = "Search:", AutoSize = true, Margin = new Padding(0, 10, 4, 0) };
            txtSearch.Width = 220;
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

            KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.F2) Edit(null);
                if (e.KeyCode == Keys.F4) txtSearch.Focus();
                if (e.KeyCode == Keys.Delete) _ = DeleteAsync();
            };

            Controls.AddRange([dgv, toolbar]);
            Load += async (_, _) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            var all = await _repo.GetAllAsync();
            var term = txtSearch.Text.Trim();
            if (term.Length > 0) all = all.Where(c => c.Name.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
            dgv.DataSource = all.Select(c => new { c.Id, c.Name, c.Description }).ToList();
            if (dgv.Columns["Id"] != null) dgv.Columns["Id"].Visible = false;
        }

        private void EditSelected()
        {
            if (dgv.CurrentRow?.Cells["Id"]?.Value is int id) Edit(id);
        }

        private async void Edit(int? id)
        {
            var cat = id is null ? new Category() : await _repo.GetByIdAsync(id.Value) ?? new Category();
            var dlg = new FrmCategoryEdit(cat);
            if (dlg.ShowDialog(this) != DialogResult.OK) { dlg.Dispose(); return; }
            dlg.Dispose();
            if (id is null) await _repo.AddAsync(cat);
            else _repo.Update(cat);
            await AppServices.Db.SaveChangesAsync();
            await LoadDataAsync();
        }

        private async Task DeleteAsync()
        {
            if (dgv.CurrentRow?.Cells["Id"]?.Value is not int id) { Warn("Select a category."); return; }
            if (AppServices.Db.Products.Any(p => p.CategoryId == id)) { Warn("Category has products. Remove them first."); return; }
            if (!Confirm("Delete category?")) return;
            var cat = await _repo.GetByIdAsync(id);
            if (cat is null) return;
            _repo.Remove(cat);
            await AppServices.Db.SaveChangesAsync();
            await LoadDataAsync();
        }
    }

    public class FrmCategoryEdit : BaseForm
    {
        private readonly Category _cat;
        private readonly TextBox txtName = new();
        private readonly TextBox txtDesc = new();

        public FrmCategoryEdit(Category cat)
        {
            _cat = cat;
            Text = cat.Id == 0 ? "New Category" : "Edit Category";
            ClientSize = new Size(440, 280);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                ColumnCount = 1,
                RowCount = 5
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

            layout.Controls.Add(new Label { Text = "Name", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
            txtName.Dock = DockStyle.Fill;
            txtName.Text = cat.Name;
            layout.Controls.Add(txtName, 0, 1);

            layout.Controls.Add(new Label { Text = "Description", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
            txtDesc.Dock = DockStyle.Fill;
            txtDesc.Multiline = true;
            txtDesc.ScrollBars = ScrollBars.Vertical;
            txtDesc.Text = cat.Description ?? "";
            layout.Controls.Add(txtDesc, 0, 3);

            var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 8, 0, 0) };
            var btnCancel = MakeButton("Cancel", Color.FromArgb(149, 165, 166));
            btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
            var btnSave = MakeButton("Save", Color.FromArgb(39, 174, 96));
            btnSave.Click += (_, _) => Save();
            btnPanel.Controls.Add(btnCancel);
            btnPanel.Controls.Add(btnSave);
            layout.Controls.Add(btnPanel, 0, 4);

            Controls.Add(layout);
            AcceptButton = btnSave;
            CancelButton = btnCancel;
            Shown += (_, _) => txtName.Focus();
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text)) { Warn("Name required."); return; }
            _cat.Name = txtName.Text.Trim();
            _cat.Description = txtDesc.Text.Trim();
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
