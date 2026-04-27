using Microsoft.EntityFrameworkCore;
using Portfolio.Api.Infrastructure.Persistence.Entities;

namespace Portfolio.Api.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<IngestedFileRecord> Files => Set<IngestedFileRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Username).HasMaxLength(100).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(512).IsRequired();
            entity.Property(user => user.Role).HasMaxLength(50).IsRequired();
            entity.Property(user => user.CreatedAtUtc).IsRequired();
            entity.HasIndex(user => user.Username).IsUnique();
        });

        modelBuilder.Entity<IngestedFileRecord>(entity =>
        {
            entity.ToTable("files");
            entity.HasKey(file => file.Id);
            entity.Property(file => file.FileName).HasMaxLength(255).IsRequired();
            entity.Property(file => file.SourceName).HasMaxLength(255).IsRequired();
            entity.Property(file => file.ContentType).HasMaxLength(255).IsRequired();
            entity.Property(file => file.SizeBytes).IsRequired();
            entity.Property(file => file.ChunkCount).IsRequired();
            entity.Property(file => file.IngestedAtUtc).IsRequired();

            entity.HasOne(file => file.UploadedByUser)
                .WithMany(user => user.IngestedFiles)
                .HasForeignKey(file => file.UploadedByUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(file => file.UploadedByUserId);
            entity.HasIndex(file => file.IngestedAtUtc);
        });
    }
}