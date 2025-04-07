using Microsoft.EntityFrameworkCore;
using BankEmployeeManagement.Models;

namespace BankEmployeeManagement.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Branch> Branches { get; set; } = null!;
        public DbSet<Employee> Employees { get; set; } = null!;
        public DbSet<EmployeeHistory> EmployeeHistory { get; set; }
        public DbSet<BranchHistory> BranchHistory { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Taske> Tasks { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Настройка уникального индекса для Username
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique()
                .HasDatabaseName("IX_User_Username");

            // Установка связи Role → User (один ко многим)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany()
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Cascade); // Удаление пользователя при удалении роли

            // Установка связи Branch → Employee (один ко многим)
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Branch)
                .WithMany(b => b.Employees)
                .HasForeignKey(e => e.BranchId)
                .OnDelete(DeleteBehavior.SetNull); // При удалении филиала связь разрывается, но сотрудник остается

            // Ограничение длины строки для полей, если необходимо
            modelBuilder.Entity<Role>()
                .Property(r => r.NameRole)
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<Employee>()
                .Property(e => e.Phone)
                .HasMaxLength(15); // Для номера телефона

            modelBuilder.Entity<Employee>()
                .Property(e => e.Email)
                .HasMaxLength(255); // Для адреса электронной почты

            // Установка уникального индекса на название филиала
            modelBuilder.Entity<Branch>()
                .HasIndex(b => b.Name)
                .IsUnique()
                .HasDatabaseName("IX_Branch_Name");

            modelBuilder.Entity<Employee>()
                .Property(e => e.Salary)
                .IsRequired() // Поле обязательно
                .HasPrecision(10, 2);
                
            // Настройка каскадного удаления и уникальных связей
            modelBuilder.Entity<Branch>()
                .HasMany(b => b.Employees)
                .WithOne(e => e.Branch)
                .HasForeignKey(e => e.BranchId)
                .OnDelete(DeleteBehavior.Cascade); // Каскадное удаление сотрудников при удалении филиала

            modelBuilder.Entity<Taske>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Title).IsRequired().HasMaxLength(100);
                entity.Property(t => t.Description).HasMaxLength(500);
                entity.Property(t => t.Status).IsRequired().HasMaxLength(20);
                entity.Property(t => t.Priority).IsRequired();
                entity.Property(t => t.CreatedBy).IsRequired().HasMaxLength(50);
                entity.HasOne(t => t.AssignedEmployee)
                      .WithMany()
                      .HasForeignKey(t => t.AssignedEmployeeId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(t => t.Branch)
                      .WithMany()
                      .HasForeignKey(t => t.BranchId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }

    }
}
