using LeveLEO.Features.AttributeGroups.Models;
using LeveLEO.Features.Brands.Models;
using LeveLEO.Features.Categories.Models;
using LeveLEO.Features.Identity.Models;
using LeveLEO.Features.Inventory.Models;
using LeveLEO.Features.Orders.Models;
using LeveLEO.Features.Payments.Models;
using LeveLEO.Features.ProductAttributes.Models;
using LeveLEO.Features.ProductAttributeValues.Models;
using LeveLEO.Features.Products.Models;
using LeveLEO.Features.Promotions.Models;
using LeveLEO.Features.Promotions.Models.Coupons;
using LeveLEO.Features.Shipping.Models;
using LeveLEO.Features.ShoppingCarts.Models;
using LeveLEO.Features.UserProductRelations.Models;
using LeveLEO.Infrastructure.Common;
using LeveLEO.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LeveLEO.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)

{
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<CouponAssignment> CouponAssignments => Set<CouponAssignment>();
    public DbSet<ShoppingCart> ShoppingCarts => Set<ShoppingCart>();
    public DbSet<ShoppingCartItem> ShoppingCartItems => Set<ShoppingCartItem>();

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderItemReview> OrderItemReviews => Set<OrderItemReview>();
    public DbSet<OrderItemReviewPhoto> OrderItemReviewPhotos => Set<OrderItemReviewPhoto>();
    public DbSet<OrderItemReviewVideo> OrderItemReviewVideos => Set<OrderItemReviewVideo>();

    public DbSet<Product> Products => Set<Product>();
    public DbSet<InventoryReservation> InventoryReservations => Set<InventoryReservation>();
    public DbSet<Category> Categories => Set<Category>();

    public DbSet<CategoryClosure> CategoryClosures => Set<CategoryClosure>();
    public DbSet<Brand> Brands => Set<Brand>();

    public DbSet<ProductAttribute> ProductAttributes => Set<ProductAttribute>();
    public DbSet<ProductAttributeValue> ProductAttributeValues => Set<ProductAttributeValue>();

    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductVideo> ProductVideos => Set<ProductVideo>();
    public DbSet<AttributeGroup> AttributeGroups => Set<AttributeGroup>();
    public DbSet<UserFavorite> UserFavorites => Set<UserFavorite>();
    public DbSet<UserComparison> UserComparisons => Set<UserComparison>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();
    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();

    public DbSet<Payment> Payments => Set<Payment>();

    //translations for 6 entities
    public DbSet<CategoryTranslation> CategoryTranslations => Set<CategoryTranslation>();

    public DbSet<ProductTranslation> ProductTranslations => Set<ProductTranslation>();
    public DbSet<BrandTranslation> BrandTranslations => Set<BrandTranslation>();
    public DbSet<AttributeGroupTranslation> AttributeGroupTranslations => Set<AttributeGroupTranslation>();
    public DbSet<ProductAttributeTranslation> ProductAttributeTranslations => Set<ProductAttributeTranslation>();
    public DbSet<ProductAttributeValueTranslation> ProductAttributeValueTranslations => Set<ProductAttributeValueTranslation>();
    public DbSet<PromotionTranslation> PromotionTranslations => Set<PromotionTranslation>();

    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        #region Global Query Filters

        // -------------------------
        // Identity / Users
        // -------------------------
        builder.Entity<ApplicationUser>()
            .HasQueryFilter(u => !u.IsDeleted);

        builder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<RefreshToken>()
            .HasIndex(rt => rt.TokenHash)
            .IsUnique();

        // -------------------------
        // Categories
        // -------------------------
        builder.Entity<Category>()
            .HasMany(c => c.Children)
            .WithOne(c => c.Parent)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Category>()
            .HasIndex(c => c.Slug)
            .IsUnique();
        builder.Entity<Category>()
            .HasIndex(c => c.ParentId);

        builder.Entity<CategoryTranslation>()
            .HasOne(ct => ct.Category)
            .WithMany(c => c.Translations)
            .HasForeignKey(ct => ct.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CategoryTranslation>()
            .HasIndex(ct => new { ct.CategoryId, ct.LanguageCode })
            .IsUnique();

        builder.Entity<CategoryClosure>()
            .HasKey(cc => new { cc.AncestorId, cc.DescendantId });

        builder.Entity<CategoryClosure>()
            .HasOne(cc => cc.Ancestor)
            .WithMany(c => c.Descendants)
            .HasForeignKey(cc => cc.AncestorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<CategoryClosure>()
            .HasIndex(cc => cc.AncestorId);
        builder.Entity<CategoryClosure>()
            .HasIndex(cc => cc.DescendantId);

        // -------------------------
        // Brands
        // -------------------------
        builder.Entity<Brand>()
            .HasMany(b => b.Products)
            .WithOne(p => p.Brand)
            .HasForeignKey(p => p.BrandId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Brand>()
            .HasIndex(b => b.Slug)
            .IsUnique();

        builder.Entity<Brand>()
            .HasMany(b => b.Translations)
            .WithOne(t => t.Brand)
            .HasForeignKey(t => t.BrandId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<BrandTranslation>()
            .HasIndex(t => new { t.BrandId, t.LanguageCode })
            .IsUnique();

        // -------------------------
        // Attribute Groups & Attributes
        // -------------------------
        builder.Entity<AttributeGroup>()
            .HasMany(ag => ag.Attributes)
            .WithOne(pa => pa.AttributeGroup)
            .HasForeignKey(pa => pa.AttributeGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<AttributeGroup>()
            .HasIndex(ag => ag.Slug)
            .IsUnique();

        builder.Entity<AttributeGroup>()
            .HasMany(ag => ag.Translations)
            .WithOne(t => t.AttributeGroup)
            .HasForeignKey(t => t.AttributeGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<AttributeGroupTranslation>()
            .HasIndex(t => new { t.AttributeGroupId, t.LanguageCode })
            .IsUnique();

        builder.Entity<ProductAttribute>()
            .HasOne(pa => pa.AttributeGroup)
            .WithMany(ag => ag.Attributes)
            .HasForeignKey(pa => pa.AttributeGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProductAttribute>()
            .HasIndex(pa => pa.Slug)
            .IsUnique();

        builder.Entity<ProductAttribute>()
            .HasMany(pa => pa.Translations)
            .WithOne(t => t.ProductAttribute)
            .HasForeignKey(t => t.ProductAttributeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProductAttributeTranslation>()
            .HasIndex(t => new { t.ProductAttributeId, t.LanguageCode })
            .IsUnique();

        builder.Entity<ProductAttributeValue>()
            .HasOne(pav => pav.Product)
            .WithMany(p => p.AttributeValues)
            .HasForeignKey(pav => pav.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProductAttributeValue>()
            .HasOne(pav => pav.ProductAttribute)
            .WithMany(pa => pa.Values)
            .HasForeignKey(pav => pav.ProductAttributeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProductAttributeValue>()
            .HasIndex(pav => new { pav.ProductId, pav.ProductAttributeId })
            .IsUnique();

        builder.Entity<ProductAttributeValueTranslation>()
            .HasOne(t => t.ProductAttributeValue)
            .WithMany(pav => pav.Translations)
            .HasForeignKey(t => t.ProductAttributeValueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProductAttributeValueTranslation>()
            .HasIndex(t => new { t.ProductAttributeValueId, t.LanguageCode })
            .IsUnique();

        // -------------------------
        // Products / Media
        // -------------------------
        builder.Entity<Product>()
            .HasMany(p => p.Translations)
            .WithOne(t => t.Product)
            .HasForeignKey(t => t.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProductTranslation>()
            .HasIndex(t => new { t.ProductId, t.LanguageCode })
            .IsUnique();

        builder.Entity<Product>()
            .HasMany(p => p.Images)
            .WithOne(pi => pi.Product)
            .HasForeignKey(pi => pi.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Product>()
            .HasMany(p => p.Videos)
            .WithOne(pv => pv.Product)
            .HasForeignKey(pv => pv.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Product>()
            .HasMany(p => p.OrderItems)
            .WithOne(oi => oi.Product)
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Product>()
            .HasIndex(p => p.Slug)
            .IsUnique();
        builder.Entity<Product>()
            .HasIndex(p => p.CategoryId);
        builder.Entity<Product>()
            .HasIndex(p => p.BrandId);

        // -------------------------
        // Favorites / Comparisons
        // -------------------------
        builder.Entity<UserFavorite>()
            .HasKey(uf => new { uf.UserId, uf.ProductId });

        builder.Entity<UserFavorite>()
            .HasOne(uf => uf.User)
            .WithMany(u => u.Favorites)
            .HasForeignKey(uf => uf.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UserFavorite>()
            .HasOne(uf => uf.Product)
            .WithMany(p => p.FavoritedBy)
            .HasForeignKey(uf => uf.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UserFavorite>()
            .HasIndex(uf => uf.UserId);
        builder.Entity<UserFavorite>()
            .HasIndex(uf => uf.ProductId);

        builder.Entity<UserComparison>()
            .HasKey(uc => new { uc.UserId, uc.ProductId });

        builder.Entity<UserComparison>()
            .HasOne(uc => uc.User)
            .WithMany(u => u.Comparisons)
            .HasForeignKey(uc => uc.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UserComparison>()
            .HasOne(uc => uc.Product)
            .WithMany(p => p.ComparedBy)
            .HasForeignKey(uc => uc.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UserComparison>()
            .HasIndex(uc => uc.UserId);
        builder.Entity<UserComparison>()
            .HasIndex(uc => uc.ProductId);

        // -------------------------
        // Promotions / Coupons
        // -------------------------
        builder.Entity<Promotion>()
            .HasMany(p => p.Translations)
            .WithOne(t => t.Promotion)
            .HasForeignKey(t => t.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Promotion>()
            .HasMany(p => p.Assignments)
            .WithOne(a => a.Promotion)
            .HasForeignKey(a => a.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PromotionTranslation>()
            .HasIndex(t => new { t.PromotionId, t.LanguageCode })
            .IsUnique();

        builder.Entity<CouponAssignment>()
            .HasOne(ca => ca.Promotion)
            .WithMany(p => p.Assignments)
            .HasForeignKey(ca => ca.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CouponAssignment>()
            .HasOne(ca => ca.User)
            .WithMany()
            .HasForeignKey(ca => ca.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CouponAssignment>()
            .HasIndex(ca => new { ca.PromotionId, ca.UserId, ca.ExpiresAt })
            .IsUnique();

        // -------------------------
        // Shopping Cart
        // -------------------------
        builder.Entity<ShoppingCart>()
            .HasOne(c => c.User)
            .WithOne(u => u.ShoppingCart)
            .HasForeignKey<ShoppingCart>(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ShoppingCart>()
            .HasMany(c => c.Items)
            .WithOne(i => i.Cart)
            .HasForeignKey(i => i.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ShoppingCartItem>()
            .HasOne(ci => ci.Product)
            .WithMany()
            .HasForeignKey(ci => ci.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ShoppingCartItem>()
            .HasIndex(ci => new { ci.CartId, ci.ProductId })
            .IsUnique();

        // -------------------------
        // Inventory
        // -------------------------
        builder.Entity<InventoryReservation>()
            .HasOne(r => r.Product)
            .WithMany()
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<InventoryReservation>()
            .HasIndex(r => new { r.ProductId, r.OrderId })
            .IsUnique();

        // -------------------------
        // Orders / OrderItems / Payments / Delivery / Address
        // -------------------------
        builder.Entity<Order>()
            .HasOne(o => o.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Order>()
            .HasOne(o => o.Address)
            .WithMany()
            .HasForeignKey(o => o.AddressId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Order>()
            .HasIndex(o => o.UserId);

        builder.Entity<OrderItem>()
            .HasOne(oi => oi.Order)
            .WithMany(o => o.OrderItems)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<OrderItem>()
            .HasOne(oi => oi.Review)
            .WithOne(r => r.OrderItem)
            .HasForeignKey<OrderItemReview>(r => r.OrderItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<OrderItemReview>()
            .HasIndex(r => r.OrderItemId)
            .IsUnique();
        builder.Entity<OrderItemReview>()
            .HasIndex(r => r.IsApproved);

        builder.Entity<OrderItemReviewPhoto>()
            .HasOne(p => p.OrderItemReview)
            .WithMany(r => r.Photos)
            .HasForeignKey(p => p.OrderItemReviewId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<OrderItemReviewVideo>()
            .HasOne(v => v.OrderItemReview)
            .WithMany(r => r.Videos)
            .HasForeignKey(v => v.OrderItemReviewId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Payment>()
            .HasOne(p => p.Order)
            .WithOne(o => o.Payment)
            .HasForeignKey<Payment>(p => p.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Payment>()
            .HasIndex(p => p.LiqPayPaymentId)
            .IsUnique();

        builder.Entity<Address>()
            .HasIndex(a => a.PostalCode);

        builder.Entity<UserAddress>()
            .HasOne(ua => ua.User)
            .WithMany(u => u.UserAddresses)
            .HasForeignKey(ua => ua.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UserAddress>()
            .HasOne(ua => ua.Address)
            .WithMany()
            .HasForeignKey(ua => ua.AddressId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UserAddress>()
            .HasIndex(ua => new { ua.UserId, ua.AddressId })
            .IsUnique();
        builder.Entity<Delivery>()
            .HasOne(d => d.Order)
            .WithOne(o => o.Delivery)
            .HasForeignKey<Delivery>(d => d.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Delivery>()
            .HasOne(d => d.Address)
            .WithMany()
            .HasForeignKey(d => d.AddressId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Delivery>()
            .HasIndex(d => d.OrderId)
            .IsUnique();
        builder.Entity<Delivery>()
            .HasIndex(d => d.TrackingNumber)
            .IsUnique();

        // -------------------------
        // Products / Media
        // -------------------------
        builder.Entity<Product>()
            .HasMany(p => p.Translations)
            .WithOne(t => t.Product)
            .HasForeignKey(t => t.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProductTranslation>()
            .HasIndex(t => new { t.ProductId, t.LanguageCode })
            .IsUnique();

        builder.Entity<Product>()
            .HasMany(p => p.Images)
            .WithOne(pi => pi.Product)
            .HasForeignKey(pi => pi.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProductImage>()
            .HasIndex(pi => pi.ProductId);

        builder.Entity<Product>()
            .HasMany(p => p.Videos)
            .WithOne(pv => pv.Product)
            .HasForeignKey(pv => pv.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProductVideo>()
            .HasIndex(pv => pv.ProductId);

        builder.Entity<Product>()
            .HasMany(p => p.OrderItems)
            .WithOne(oi => oi.Product)
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Product>()
            .HasIndex(p => p.Slug)
            .IsUnique();

        builder.Entity<Product>()
            .HasIndex(p => p.CategoryId);

        builder.Entity<Product>()
            .HasIndex(p => p.BrandId);

        #endregion Global Query Filters

        // ───────────────────────────────────────────────────────────────
        // КОНФІГУРАЦІЯ OWNED TYPES у Promotion: ProductLevelCondition та CartLevelCondition
        // ───────────────────────────────────────────────────────────────

        // ProductLevelCondition
        builder.Entity<Promotion>()
            .OwnsOne(p => p.ProductConditions, plc =>
            {
                plc.Property(c => c.ProductIds)
                   .HasConversion(
                       v => v.HasValue ? JsonSerializer.Serialize(v.Value, JsonSerializerOptions.Default) : null,
                       v => v == null ? new Optional<List<Guid>>() : new Optional<List<Guid>>(JsonSerializer.Deserialize<List<Guid>>(v)!)
                   )
                   .HasColumnType("jsonb");

                plc.Property(c => c.CategoryIds)
                   .HasConversion(
                       v => v.HasValue ? JsonSerializer.Serialize(v.Value, JsonSerializerOptions.Default) : null,
                       v => v == null ? new Optional<List<Guid>>() : new Optional<List<Guid>>(JsonSerializer.Deserialize<List<Guid>>(v)!)
                   )
                   .HasColumnType("jsonb");
            });

        // CartLevelCondition
        builder.Entity<Promotion>()
            .OwnsOne(p => p.CartConditions, clc =>
            {
                clc.Property(c => c.MinTotalAmount)
                   .HasColumnType("numeric(18,2)");

                clc.Property(c => c.MinQuantity)
                   .HasColumnType("integer");

                clc.Property(c => c.ProductIds)
                   .HasConversion(
                       v => v.HasValue ? JsonSerializer.Serialize(v.Value, JsonSerializerOptions.Default) : null,
                       v => v == null ? new Optional<List<Guid>>() : new Optional<List<Guid>>(JsonSerializer.Deserialize<List<Guid>>(v)!)
                   )
                   .HasColumnType("jsonb");

                clc.Property(c => c.CategoryIds)
                   .HasConversion(
                       v => v.HasValue ? JsonSerializer.Serialize(v.Value, JsonSerializerOptions.Default) : null,
                       v => v == null ? new Optional<List<Guid>>() : new Optional<List<Guid>>(JsonSerializer.Deserialize<List<Guid>>(v)!)
                   )
                   .HasColumnType("jsonb");
            });
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries<ITimestamped>();
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}