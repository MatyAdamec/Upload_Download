using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using upload_download.Models;

namespace upload_download.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public DbSet<StoredFile> StoredFiles { get; set; }
    public DbSet<Thumbnail> Thumbnails { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Thumbnail>().HasKey(t => new { t.FileId, t.Type });
    }
}

