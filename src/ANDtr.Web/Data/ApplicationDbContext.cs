using ANDtr.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ANDtr.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Notice> Notices => Set<Notice>();

    public DbSet<CustomerOrganisation> CustomerOrganisations => Set<CustomerOrganisation>();

    public DbSet<RiskReview> RiskReviews => Set<RiskReview>();

    public DbSet<ReviewNotice> ReviewNotices => Set<ReviewNotice>();

    public DbSet<Comment> Comments => Set<Comment>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.FullName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.IsActive).HasDefaultValue(true);

            entity.HasOne(u => u.Organisation)
                .WithMany(o => o.Users)
                .HasForeignKey(u => u.OrganisationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<CustomerOrganisation>(entity =>
        {
            entity.Property(o => o.Name).IsRequired().HasMaxLength(100);
            entity.Property(o => o.ContactEmail).IsRequired().HasMaxLength(256);
            entity.Property(o => o.Notes).HasMaxLength(500);

            entity.HasIndex(o => o.Name).IsUnique();
        });

        builder.Entity<Notice>(entity =>
        {
            entity.Property(n => n.ProductCode).IsRequired();
            entity.Property(n => n.ProductCodeName).IsRequired();
            entity.Property(n => n.RegulationNumber).IsRequired();
            entity.Property(n => n.WebAddress).IsRequired();
            entity.Property(n => n.RecallNumber).IsRequired();
            entity.Property(n => n.ProductDescription).IsRequired();
            entity.Property(n => n.FirmName).IsRequired();
            entity.Property(n => n.ManufacturerRecallReason).IsRequired();

            entity.HasIndex(n => n.RecallNumber);
            entity.HasIndex(n => new { n.RecallNumber, n.ProductCode }).IsUnique();
        });

        builder.Entity<RiskReview>(entity =>
        {
            entity.Property(r => r.Name).IsRequired();
            entity.Property(r => r.OwnerId).IsRequired();

            entity.HasOne(r => r.Owner)
                .WithMany()
                .HasForeignKey(r => r.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Organisation)
                .WithMany(o => o.RiskReviews)
                .HasForeignKey(r => r.OrganisationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ReviewNotice>(entity =>
        {
            entity.HasIndex(rn => new { rn.RiskReviewId, rn.NoticeId }).IsUnique();

            entity.HasOne(rn => rn.RiskReview)
                .WithMany(r => r.ReviewNotices)
                .HasForeignKey(rn => rn.RiskReviewId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(rn => rn.Notice)
                .WithMany(n => n.ReviewNotices)
                .HasForeignKey(rn => rn.NoticeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Comment>(entity =>
        {
            entity.Property(c => c.AuthorId).IsRequired();
            entity.Property(c => c.Body).IsRequired();

            entity.HasIndex(c => c.RiskReviewId);

            entity.HasOne(c => c.RiskReview)
                .WithMany(r => r.Comments)
                .HasForeignKey(c => c.RiskReviewId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.ReviewNotice)
                .WithMany(rn => rn.Comments)
                .HasForeignKey(c => c.ReviewNoticeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.Author)
                .WithMany()
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Notification>(entity =>
        {
            entity.Property(n => n.UserId).IsRequired();
            entity.Property(n => n.Message).IsRequired();

            entity.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(n => n.RiskReview)
                .WithMany(r => r.Notifications)
                .HasForeignKey(n => n.RiskReviewId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ActivityLog>(entity =>
        {
            entity.Property(a => a.UserId).IsRequired();
            entity.Property(a => a.Action).IsRequired();

            entity.HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.RiskReview)
                .WithMany(r => r.ActivityLogs)
                .HasForeignKey(a => a.RiskReviewId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
