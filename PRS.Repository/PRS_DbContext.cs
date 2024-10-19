using Microsoft.EntityFrameworkCore;
using PRS.Core;

namespace PRS.Repository
{
    public class PRS_DbContext : DbContext
    {
        #region DbSet
        public DbSet<ModelTrainDataSet> ModelTrainDataSets { get; set; }
        #endregion

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Server=.\MSSQLSERVER22DEV;Database=EPrescribingDB;User ID=sa;Password=SqlDev@19;TrustServerCertificate=true;Connection Timeout=3600;");
        }
        #region Configuration
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ModelTrainDataSet>(
                eb =>
                {
                    eb.HasNoKey();
                    eb.ToSqlQuery("EXEC [dbo].[GetModelTrainDataSet]");
                });
        }
        #endregion
    }
}

