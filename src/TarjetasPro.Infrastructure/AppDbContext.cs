using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TarjetasPro.Domain.Entities;
using TarjetasPro.Infrastructure.Identity;

namespace TarjetasPro.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Template> Templates => Set<Template>();
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<CardVisit> CardVisits => Set<CardVisit>();
    public DbSet<CardClick> CardClicks => Set<CardClick>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<TemplateCategory> TemplateCategories => Set<TemplateCategory>();
    public DbSet<PlanPromotion> PlanPromotions => Set<PlanPromotion>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<PlanSetting> PlanSettings => Set<PlanSetting>();
    public DbSet<ProductSetting> ProductSettings => Set<ProductSetting>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<Guest> Guests => Set<Guest>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(u => u.FullName).HasMaxLength(150);
            e.Property(u => u.IsActive).HasDefaultValue(true);
        });

        builder.Entity<TemplateCategory>(e =>
        {
            e.Property(c => c.Name).HasMaxLength(50).IsRequired();
            e.HasIndex(c => c.Name).IsUnique();
        });

        builder.Entity<Template>(e =>
        {
            e.Property(t => t.Name).HasMaxLength(100).IsRequired();
            e.Property(t => t.Product).HasMaxLength(30).IsRequired().HasDefaultValue(Products.Cards);
            e.Property(t => t.Code).HasMaxLength(40).IsRequired();
            e.HasIndex(t => t.Code).IsUnique();
            e.Property(t => t.Description).HasMaxLength(300);
            e.Property(t => t.Category).HasMaxLength(50).IsRequired().HasDefaultValue("General");
            e.Property(t => t.Profession).HasMaxLength(80).HasDefaultValue("");
            e.HasIndex(t => t.Category);
            e.HasIndex(t => t.Product);
            e.Property(t => t.FieldKeys).HasMaxLength(500);
        });

        builder.Entity<Card>(e =>
        {
            e.Property(c => c.Product).HasMaxLength(30).IsRequired().HasDefaultValue(Products.Cards);
            e.Property(c => c.Title).HasMaxLength(100).IsRequired();
            e.Property(c => c.Slug).HasMaxLength(30).IsRequired();
            e.HasIndex(c => c.Slug).IsUnique();
            e.HasIndex(c => c.UserId);
            e.HasQueryFilter(c => !c.IsDeleted);
            e.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(c => c.Plan).HasConversion<string>().HasMaxLength(20).HasDefaultValue(CardPlan.Sencilla);
            e.Property(c => c.ThemeJson).HasDefaultValue("{}");
            e.Property(c => c.GalleryJson).HasDefaultValue("[]");
            e.Property(c => c.VideoUrl).HasMaxLength(300);
            e.HasOne(c => c.Template).WithMany().HasForeignKey(c => c.TemplateId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PlanPromotion>(e =>
        {
            e.Property(p => p.Plan).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(p => p.Plan).IsUnique();
            e.Property(p => p.Kind).HasConversion<string>().HasMaxLength(20);
            e.Property(p => p.Label).HasMaxLength(60);
        });

        builder.Entity<ProductSetting>(e =>
        {
            e.Property(p => p.Key).HasMaxLength(30).IsRequired();
            e.HasIndex(p => p.Key).IsUnique();
            e.Property(p => p.Name).HasMaxLength(60).IsRequired();
            e.Property(p => p.Description).HasMaxLength(200);
        });

        builder.Entity<PlanSetting>(e =>
        {
            e.Property(p => p.Product).HasMaxLength(30).IsRequired();
            e.Property(p => p.Code).HasMaxLength(30).IsRequired();
            e.HasIndex(p => new { p.Product, p.Code }).IsUnique();
            e.Property(p => p.Plan).HasConversion<string>().HasMaxLength(20);
            e.Property(p => p.Name).HasMaxLength(40).IsRequired();
            e.Property(p => p.Tagline).HasMaxLength(80);
            e.Property(p => p.Features).HasMaxLength(1200);
        });

        builder.Entity<Coupon>(e =>
        {
            e.Property(c => c.Code).HasMaxLength(30).IsRequired();
            e.HasIndex(c => c.Code).IsUnique();
            e.Property(c => c.Plan).HasConversion<string>().HasMaxLength(20);
            e.Property(c => c.Note).HasMaxLength(120);
        });

        builder.Entity<Payment>(e =>
        {
            e.Property(p => p.CouponCode).HasMaxLength(30);
            e.Property(p => p.Provider).HasMaxLength(20).IsRequired();
            e.Property(p => p.ProviderSessionId).HasMaxLength(200);
            e.HasIndex(p => p.ProviderSessionId).IsUnique();
            e.Property(p => p.Currency).HasMaxLength(3).IsRequired();
            e.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(p => new { p.CardId, p.Status });
            e.HasIndex(p => p.UserId);
            e.HasIndex(p => p.ReceiptNumber).IsUnique().HasFilter("[ReceiptNumber] IS NOT NULL");
            e.Property(p => p.CardTitle).HasMaxLength(100);
            e.Property(p => p.Plan).HasConversion<string>().HasMaxLength(20).HasDefaultValue(CardPlan.Sencilla);
        });

        builder.Entity<CardVisit>(e =>
        {
            e.HasIndex(v => new { v.CardId, v.VisitedAt });
            e.HasOne<Card>().WithMany().HasForeignKey(v => v.CardId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Invitation>(e =>
        {
            e.Property(i => i.Title).HasMaxLength(100).IsRequired();
            e.Property(i => i.Slug).HasMaxLength(40).IsRequired();
            e.HasIndex(i => i.Slug).IsUnique();
            e.HasIndex(i => i.UserId);
            e.HasQueryFilter(i => !i.IsDeleted);
            e.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(i => i.Template).WithMany().HasForeignKey(i => i.TemplateId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(i => i.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(i => i.Guests).WithOne().HasForeignKey(g => g.InvitationId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Guest>(e =>
        {
            e.Property(g => g.Name).HasMaxLength(120).IsRequired();
            e.Property(g => g.Phone).HasMaxLength(20).IsRequired();
            e.Property(g => g.Token).HasMaxLength(32).IsRequired();
            e.HasIndex(g => g.Token).IsUnique();
            e.HasIndex(g => new { g.InvitationId, g.Status });
            e.Property(g => g.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(g => g.Message).HasMaxLength(300);
        });

        builder.Entity<CardClick>(e =>
        {
            e.Property(c => c.Kind).HasMaxLength(20).IsRequired();
            e.HasIndex(c => new { c.CardId, c.ClickedAt });
            e.HasOne<Card>().WithMany().HasForeignKey(c => c.CardId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<RefreshToken>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.TokenHash).HasMaxLength(100).IsRequired();
            e.HasIndex(t => t.TokenHash).IsUnique();
            e.HasIndex(t => t.UserId);
            e.Ignore(t => t.IsActive);
        });
    }
}
