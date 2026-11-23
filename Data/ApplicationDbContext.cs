using Microsoft.EntityFrameworkCore;
using Sentiment_Analysis.Models;

namespace Sentiment_Analysis.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<AspectResult> AspectResults => Set<AspectResult>();
    public DbSet<SentimentResult> SentimentResults => Set<SentimentResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserName).IsRequired().HasMaxLength(80);
            entity.Property(e => e.CommentText).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Sentiment).HasMaxLength(20);
            entity.Property(e => e.PrimaryEmotion).HasMaxLength(50);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Sentiment);
        });

        modelBuilder.Entity<AspectResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Aspect).IsRequired().HasMaxLength(60);
            entity.Property(e => e.Sentiment).IsRequired().HasMaxLength(20);
            entity.Property(e => e.MatchedPhrase).HasMaxLength(250);

            entity.HasOne(e => e.Comment)
                  .WithMany(c => c.AspectResults)
                  .HasForeignKey(e => e.CommentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.Aspect);
            entity.HasIndex(e => e.Sentiment);
        });

        modelBuilder.Entity<SentimentResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ModelEngine).HasMaxLength(100);
            entity.Property(e => e.DetectedEmotions).HasMaxLength(500);
            entity.Property(e => e.NormalizedTokens).HasMaxLength(1000);

            entity.HasOne(e => e.Comment)
                  .WithOne(c => c.SentimentResult)
                  .HasForeignKey<SentimentResult>(e => e.CommentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
