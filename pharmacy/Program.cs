using Microsoft.EntityFrameworkCore;
using pharmacy.Forms;
using pharmacy.Helpers;

namespace pharmacy
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(RunApp());
        }

        private static Form RunApp()
        {
            try
            {
                AppServices.InitAsync(ConnectionString()).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database connection failed:\n{ex.Message}", "PharmacyMS",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return new Form();
            }

            using var login = new FrmLogin();
            if (login.ShowDialog() != DialogResult.OK || login.LoggedInUser is null)
                return new Form();

            return new FrmMain(login.LoggedInUser);
        }

        private static string ConnectionString()
        {
            var cs = Environment.GetEnvironmentVariable("PHARMACY_DB");
            return string.IsNullOrWhiteSpace(cs)
                ? @"Server=(localdb)\MSSQLLocalDB;Database=PharmacyMS;Trusted_Connection=True;TrustServerCertificate=True;"
                : cs;
        }
    }
}
