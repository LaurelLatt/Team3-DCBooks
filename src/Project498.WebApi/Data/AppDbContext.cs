using Microsoft.EntityFrameworkCore;
using Project498.WebApi.Models;

namespace Project498.WebApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Checkout> Checkouts => Set<Checkout>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100).HasColumnName("first_name");
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100).HasColumnName("last_name");
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100).HasColumnName("username");
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100).HasColumnName("email");
            entity.Property(e => e.Password).IsRequired().HasColumnName("password");
        });

        modelBuilder.Entity<Checkout>(entity =>
        {
            entity.ToTable("Checkouts");
            entity.HasKey(e => e.CheckoutId);
            entity.Property(e => e.CheckoutId).HasColumnName("checkout_id");
            entity.Property(e => e.UserId).IsRequired().HasColumnName("user_id");
            entity.Property(e => e.ComicId).IsRequired().HasColumnName("comic_id");
            entity.Property(e => e.CheckoutDate).IsRequired().HasColumnName("checkout_date");
            entity.Property(e => e.DueDate).IsRequired().HasColumnName("due_date");
            entity.Property(e => e.ReturnDate).HasColumnName("return_date");
            entity.Property(e => e.Status).IsRequired().HasColumnName("status");

            entity.HasOne(e => e.User)
                  .WithMany(u => u.Checkouts)
                  .HasForeignKey(e => e.UserId);
        });
    }
}