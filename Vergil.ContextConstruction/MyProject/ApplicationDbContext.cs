using Microsoft.EntityFrameworkCore;
using MyProject.Models;

namespace MyProject;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Slot> Slots { get; set; }
}
