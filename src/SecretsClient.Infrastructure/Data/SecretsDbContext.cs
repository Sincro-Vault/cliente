using Microsoft.EntityFrameworkCore;
using SecretsClient.Core.Domain.Entities;
using SecretsClient.Core.Domain.ValueObjects;

namespace SecretsClient.Infrastructure.Data
{
    public class SecretsDbContext : DbContext
    {
        public SecretsDbContext(DbContextOptions<SecretsDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Secret> Secrets { get; set; }
        public DbSet<SecretFragment> Fragments { get; set; }
        public DbSet<GeoPolicy> GeoPolicies { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Username).IsUnique();
            });

            modelBuilder.Entity<Secret>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Id)
                    .HasConversion(
                        v => v.Value,
                        v => SecretId.From(v));

                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                
                entity.HasOne<User>()
                    .WithMany(p => p.Secrets)
                    .HasForeignKey(d => d.UserId);
            });

            modelBuilder.Entity<SecretFragment>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.SecretId)
                    .HasConversion(
                        v => v.Value,
                        v => SecretId.From(v));

                entity.HasOne<Secret>()
                    .WithMany(p => p.Fragments)
                    .HasForeignKey(d => d.SecretId);
                
                entity.Property(e => e.EncryptedFragment).IsRequired();
            });

            modelBuilder.Entity<GeoPolicy>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.HasOne<Secret>()
                    .WithMany(p => p.GeoPolicies)
                    .HasForeignKey(d => d.SecretId);
            });
        }
    }
}
