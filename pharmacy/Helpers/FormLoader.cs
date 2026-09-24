namespace pharmacy.Helpers
{
    public static class FormLoader
    {
        public static void ShowChild(Panel container, Form form)
        {
            container.Controls.Clear();
            form.TopLevel = false;
            form.FormBorderStyle = FormBorderStyle.None;
            form.Dock = DockStyle.Fill;
            container.Controls.Add(form);
            form.Show();
        }
    }
}
