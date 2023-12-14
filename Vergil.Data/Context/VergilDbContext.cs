using Microsoft.EntityFrameworkCore;
using Vergil.Data.Models;

namespace Vergil.Data.Context;

public class VergilDbContext : DbContext
{

    public VergilDbContext(DbContextOptions<VergilDbContext> options) : base(options)
    {
    }
    
    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<Slot> Slots { get; set; }
}