using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using CentralMed.Models;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace CentralMed.Data
{
    public class AppDbContext : DbContext
    {
        private static bool _tableChecked = false;

        public AppDbContext() : base("name=MyDbContext")
        {
            Database.SetInitializer<AppDbContext>(null);

            if (!_tableChecked)
            {
                try
                {
                    this.Database.ExecuteSqlCommand("ALTER TABLE [BillingRecord] ADD COLUMN [Discount] DECIMAL(18,2) NOT NULL DEFAULT 0;");
                }
                catch { }

                try
                {
                    this.Database.ExecuteSqlCommand("ALTER TABLE [BillingRecord] RENAME COLUMN [Scheme] TO [Schedule];");
                }
                catch { }

                try
                {
                    this.BillingRecords.Take(1).ToList();
                }
                catch (Exception)
                {
                    try
                    {
                        string createSql = @"
                            CREATE TABLE IF NOT EXISTS [BillingRecord] (
                                [Id] INTEGER PRIMARY KEY AUTOINCREMENT,
                                [Patient] NVARCHAR(255),
                                [Prescriber] NVARCHAR(255),
                                [BillNo] NVARCHAR(50),
                                [BillDate] DATETIME NOT NULL,
                                [Qty] INTEGER NOT NULL,
                                [DrugName] NVARCHAR(255),
                                [Schedule] NVARCHAR(255),
                                [Manufacturer] NVARCHAR(255),
                                [BatchNo] NVARCHAR(100),
                                [ExpiryDate] NVARCHAR(100),
                                [Rate] DECIMAL(18,2) NOT NULL,
                                [Discount] DECIMAL(18,2) NOT NULL DEFAULT 0,
                                [Amount] DECIMAL(18,2) NOT NULL
                            );
                        ";
                        this.Database.ExecuteSqlCommand(createSql);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                    }
                }
                _tableChecked = true;
            }
        }

        public DbSet<BillingRecord> BillingRecords { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            base.OnModelCreating(modelBuilder);
        }
    }
}
