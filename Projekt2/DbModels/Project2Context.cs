using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;

namespace Projekt2.DbModels
{
    public partial class Project2Context : DbContext
    {
        public Project2Context()
        {
        }

        public Project2Context(DbContextOptions<Project2Context> options)
            : base(options)
        {
        }

        public virtual DbSet<Account> Account { get; set; }
        public virtual DbSet<AccountGroup> AccountGroup { get; set; }
        public virtual DbSet<AccountYear> AccountYear { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Get db-connection-string from appsettings.json and set it for DbContext:

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();
            optionsBuilder.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>(entity =>
            {
                entity.HasKey(e => new { e.Type, e.FunctionId, e.SubjectId, e.Number });

                entity.Property(e => e.Type).HasMaxLength(2);

                entity.Property(e => e.FunctionId).HasMaxLength(8);

                entity.Property(e => e.SubjectId).HasMaxLength(8);

                entity.Property(e => e.FunctionType)
                    .IsRequired()
                    .HasMaxLength(2);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(256);

                entity.HasOne(d => d.Function)
                    .WithMany(p => p.AccountFunction)
                    .HasForeignKey(d => new { d.FunctionType, d.FunctionId })
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Account_Function");

                entity.HasOne(d => d.AccountGroup)
                    .WithMany(p => p.AccountAccountGroup)
                    .HasForeignKey(d => new { d.Type, d.SubjectId })
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Account_Subject");
            });

            modelBuilder.Entity<AccountGroup>(entity =>
            {
                entity.HasKey(e => new { e.Type, e.Id });

                entity.Property(e => e.Type).HasMaxLength(2);

                entity.Property(e => e.Id).HasMaxLength(8);

                entity.Property(e => e.Description).HasMaxLength(2000);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(256);

                entity.Property(e => e.ParentId).HasMaxLength(8);

                entity.HasOne(d => d.AccountGroupNavigation)
                    .WithMany(p => p.InverseAccountGroupNavigation)
                    .HasForeignKey(d => new { d.Type, d.ParentId })
                    .HasConstraintName("FK_AccountGroup_Parent");
            });

            modelBuilder.Entity<AccountYear>(entity =>
            {
                entity.HasKey(e => new { e.Type, e.FunctionId, e.SubjectId, e.Number, e.Year });

                entity.Property(e => e.Type).HasMaxLength(2);

                entity.Property(e => e.FunctionId).HasMaxLength(8);

                entity.Property(e => e.SubjectId).HasMaxLength(8);

                entity.Property(e => e.ExpensesBudget).HasColumnType("decimal(12, 2)");

                entity.Property(e => e.ExpensesEffective).HasColumnType("decimal(12, 2)");

                entity.Property(e => e.FunctionType)
                    .IsRequired()
                    .HasMaxLength(2);

                entity.Property(e => e.IncomeBudget).HasColumnType("decimal(12, 2)");

                entity.Property(e => e.IncomeEffective).HasColumnType("decimal(12, 2)");

                entity.HasOne(d => d.Account)
                    .WithMany(p => p.AccountYear)
                    .HasForeignKey(d => new { d.Type, d.FunctionId, d.SubjectId, d.Number })
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_AccountYear_Account");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
