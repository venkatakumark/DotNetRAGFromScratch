using Microsoft.EntityFrameworkCore;
using MyLlmApp.Models;

namespace MyLlmApp.Data;

public class AppDbContext : DbContext
{
    public DbSet<Employee> Employees =>
        Set<Employee>();

    protected override void OnConfiguring(
        DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite(
            "Data Source=employees.db");
    }
}