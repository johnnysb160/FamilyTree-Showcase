using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Relative> Relatives => Set<Relative>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        var r = mb.Entity<Relative>();
        r.Ignore(x => x.Years);
        r.Ignore(x => x.Age);
        r.HasOne(x => x.Father).WithMany().HasForeignKey(x => x.FatherId).OnDelete(DeleteBehavior.Restrict);
        r.HasOne(x => x.Mother).WithMany().HasForeignKey(x => x.MotherId).OnDelete(DeleteBehavior.Restrict);
        r.HasOne(x => x.Spouse).WithMany().HasForeignKey(x => x.SpouseId).OnDelete(DeleteBehavior.Restrict);
        r.HasIndex(x => x.Name);
        r.HasData(SeedData.Relatives);
    }
}
