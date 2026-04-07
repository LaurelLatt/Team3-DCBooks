using Microsoft.EntityFrameworkCore;
using Project498.WebApi.Models;

namespace Project498.WebApi.Data;

public class ComicsDbContext : DbContext
{
    public ComicsDbContext(DbContextOptions<ComicsDbContext> options) : base(options)
    {
    }

    public DbSet<Comic> Comics => Set<Comic>();
    public DbSet<Character> Characters => Set<Character>();
    public DbSet<ComicCharacter> ComicCharacters => Set<ComicCharacter>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Comic>(entity =>
        {
            entity.ToTable("Comics");
            entity.HasKey(e => e.ComicId);
            entity.Property(e => e.ComicId).HasColumnName("comic_id");
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200).HasColumnName("title");
            entity.Property(e => e.IssueNumber).IsRequired().HasColumnName("issue_number");
            entity.Property(e => e.YearPublished).IsRequired().HasColumnName("year_published");
            entity.Property(e => e.Publisher).IsRequired().HasMaxLength(100).HasColumnName("publisher");
            entity.Property(e => e.Status).IsRequired().HasColumnName("status");
            entity.Property(e => e.CheckedOutBy).HasColumnName("checked_out_by");
            
            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Comics_Status",
                "status IN ('available', 'checked_out')"
            ));
        });

        modelBuilder.Entity<Character>(entity =>
        {
            entity.ToTable("Characters");
            entity.HasKey(e => e.CharacterId);
            entity.Property(e => e.CharacterId).HasColumnName("character_id");
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100).HasColumnName("name");
            entity.Property(e => e.Alias).HasMaxLength(100).HasColumnName("alias");
            entity.Property(e => e.Description).HasColumnName("description");
        });

        modelBuilder.Entity<ComicCharacter>(entity =>
        {
            entity.ToTable("Comic_Characters");
            entity.HasKey(e => new { e.ComicId, e.CharacterId });
            entity.Property(e => e.ComicId).HasColumnName("comic_id");
            entity.Property(e => e.CharacterId).HasColumnName("character_id");

            entity.HasOne(e => e.Comic)
                  .WithMany(c => c.ComicCharacters)
                  .HasForeignKey(e => e.ComicId);

            entity.HasOne(e => e.Character)
                  .WithMany(c => c.ComicCharacters)
                  .HasForeignKey(e => e.CharacterId);
        });
    }
}