using BankEmployeeManagement.Models;
using Microsoft.EntityFrameworkCore;
using BankEmployeeManagement.Data;

namespace BankEmployeeManagement
{
    public class SeedData
    {
        public static void Initialize(AppDbContext context)
        {
            if (context.Roles.Any()) return;

            // Добавляем роли
            var roles = new[]
            {
                new Role { NameRole = "Administrator" },
                new Role { NameRole = "HeadBranch" }
            };
            context.Roles.AddRange(roles);

            // Добавляем филиалы
            var branches = new[]
            {
                new Branch { Name = "Main Branch", Address = "123 Bank Street" },
                new Branch { Name = "Secondary Branch", Address = "456 Finance Avenue" }
            };
            context.Branches.AddRange(branches);

            // Сохраняем изменения
            context.SaveChanges();
        }
    }
}
