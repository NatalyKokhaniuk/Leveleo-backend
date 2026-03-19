using dotenv.net;
using LeveLEO.Data;
using LeveLEO.Features.AdminTasks.Services;
using LeveLEO.Features.AttributeGroups.Services;
using LeveLEO.Features.Brands.Services;
using LeveLEO.Features.Categories.Services;
using LeveLEO.Features.Identity.Models;
using LeveLEO.Features.Identity.Services;
using LeveLEO.Features.Inventory.Services;
using LeveLEO.Features.Newsletter.EventHandlers;
using LeveLEO.Features.Newsletter.Services;
using LeveLEO.Features.Notifications.EventHandlers;
using LeveLEO.Features.Orders.Services;
using LeveLEO.Features.Payments.Services;
using LeveLEO.Features.ProductAttributes.Services;
using LeveLEO.Features.ProductAttributeValues.Services;
using LeveLEO.Features.Products.Services;
using LeveLEO.Features.Promotions.Services;
using LeveLEO.Features.Shipping.Services;
using LeveLEO.Features.ShoppingCarts.Services;
using LeveLEO.Features.Statistics.Services;
using LeveLEO.Features.UserProductRelations.Services;
using LeveLEO.Features.Users.Services;
using LeveLEO.Infrastructure.Caching;
using LeveLEO.Infrastructure.Delivery;
using LeveLEO.Infrastructure.Email;
using LeveLEO.Infrastructure.Events;
using LeveLEO.Infrastructure.Events.DomainEvents;
using LeveLEO.Infrastructure.Logging;
using LeveLEO.Infrastructure.Media.Services;
using LeveLEO.Infrastructure.Payments;
using LeveLEO.Infrastructure.SlugGenerator;
using LeveLEO.Infrastructure.SMS;
using LeveLEO.Middleware;
using LeveLEO.OpenApi;
using LeveLEO.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using StackExchange.Redis;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;

// Завантажуємо .env файл (якщо не в контейнері)
if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != "true")
{
    DotEnv.Load();
}

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// SERILOG LOGGING - конфігуруємо на самому початку
// =====================================================
builder.ConfigureSerilog();

// =====================================================
// DATABASE
// =====================================================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(); // retry при тимчасових помилках
            npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery); // для Include
        }));

// =====================================================
// IDENTITY
// =====================================================
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// =====================================================
// JWT AUTHENTICATION
// =====================================================
var jwtSettings = new JwtSettings
{
    Secret = Environment.GetEnvironmentVariable("JWT_SECRET")!,
    Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER")!,
    Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")!,
    AccessTokenExpirationMinutes = int.Parse(Environment.GetEnvironmentVariable("JWT_ACCESS_EXP") ?? "15"),
    RefreshTokenExpirationDays = int.Parse(Environment.GetEnvironmentVariable("JWT_REFRESH_EXP") ?? "7")
};

builder.Services.AddSingleton(jwtSettings);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
        NameClaimType = ClaimTypes.NameIdentifier,
        RoleClaimType = ClaimTypes.Role,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorizationBuilder()
    .SetDefaultPolicy(new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .Build());

// =====================================================
// REDIS CACHING (якщо доступний) або In-Memory
// =====================================================
var redisConnection = Environment.GetEnvironmentVariable("REDIS__CONNECTION");

if (!string.IsNullOrEmpty(redisConnection))
{
    try
    {
        var redis = ConnectionMultiplexer.Connect(redisConnection);
        builder.Services.AddSingleton<IConnectionMultiplexer>(redis);
        builder.Services.AddSingleton<ICacheService, RedisCacheService>();
        Log.Information("✅ Redis caching enabled: {RedisConnection}", redisConnection);
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "⚠️ Redis connection failed, falling back to InMemory cache");
        builder.Services.AddMemoryCache();
        builder.Services.AddSingleton<ICacheService, InMemoryCacheService>();
    }
}
else
{
    // Fallback до InMemory якщо Redis не налаштований
    builder.Services.AddMemoryCache();
    builder.Services.AddSingleton<ICacheService, InMemoryCacheService>();
    Log.Information("ℹ️ Using InMemory cache (Redis not configured)");
}

// =====================================================
// HTTP CLIENT
// =====================================================
builder.Services.AddHttpClient();

// =====================================================
// AUTH & USER SERVICES
// =====================================================
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISocialAuthService, SocialAuthService>();

// =====================================================
// FEATURE SERVICES
// =====================================================
builder.Services.AddScoped<IAttributeGroupService, AttributeGroupService>();
builder.Services.AddScoped<IBrandService, BrandService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IOrderItemReviewService, OrderItemReviewService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IProductAttributeService, ProductAttributeService>();
builder.Services.AddScoped<IProductAttributeValueService, ProductAttributeValueService>();

// ProductService з декоратором для кешування
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<IProductService>(provider =>
{
    var inner = provider.GetRequiredService<ProductService>();
    var cache = provider.GetRequiredService<ICacheService>();
    var logger = provider.GetRequiredService<ILogger<CachedProductService>>();
    return new CachedProductService(inner, cache, logger);
});

builder.Services.AddScoped<ICouponAssignmentService, CouponAssignmentService>();
builder.Services.AddScoped<IPromotionService, PromotionService>();
builder.Services.AddScoped<IShoppingCartService, ShoppingCartService>();
builder.Services.AddScoped<IUserProductRelationService, UserProductRelationService>();
builder.Services.AddScoped<IAddressService, AddressService>();
builder.Services.AddScoped<IDeliveryService, DeliveryService>();
builder.Services.AddScoped<IAdminTaskService, AdminTaskService>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>();

// =====================================================
// INFRASTRUCTURE SERVICES
// =====================================================
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddTransient<ISmsSender, SmsSender>();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<ISlugGenerator, SlugGenerator>();
builder.Services.AddScoped<ILiqPayService, LiqPayService>();
builder.Services.AddHttpClient<INovaPoshtaService, NovaPoshtaService>();
builder.Services.AddScoped<IProductMediaService, ProductMediaService>();
builder.Services.AddHttpClient();
// =====================================================
// EVENT BUS & HANDLERS
// =====================================================
builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();

// Email Handlers
builder.Services.AddScoped<OrderCreatedEmailHandler>();
builder.Services.AddScoped<OrderPaidEmailHandler>();
builder.Services.AddScoped<OrderShippedEmailHandler>();
builder.Services.AddScoped<OrderCompletedEmailHandler>();
builder.Services.AddScoped<ReviewApprovedEmailHandler>();
builder.Services.AddScoped<ReviewRejectedEmailHandler>();

// Admin Task Handlers
builder.Services.AddScoped<ReviewCreatedTaskHandler>();
builder.Services.AddScoped<OrderPaidTaskHandler>();
builder.Services.AddScoped<PaymentMismatchTaskHandler>();
// Newsletter Event Handlers
builder.Services.AddScoped<ProductCreatedNewsletterHandler>();
builder.Services.AddScoped<PromotionCreatedNewsletterHandler>();
builder.Services.AddScoped<INewsletterService, NewsletterService>();

// =====================================================
// CONTROLLERS & API
// =====================================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer(new BearerSecuritySchemeTransformer());
});

builder.Services.AddEndpointsApiExplorer();

// =====================================================
// BUILD APP
// =====================================================
var app = builder.Build();

// =====================================================
// SEEDING (тільки Development)
// =====================================================
if (app.Environment.IsDevelopment())
{
    // await DatabaseSeeder.SeedAsync(app);
}

// =====================================================
// MIDDLEWARE
// =====================================================
app.UseMiddleware<GlobalExceptionHandler>();

// Serilog HTTP Request Logging
app.UseSerilogRequestLogging();
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

// =====================================================
// API DOCUMENTATION
// =====================================================
if (!app.Environment.IsDevelopment())
{
    app.Use((context, next) =>
    {
        // Примусово ставимо HTTPS-схему для скаляра та інших URL
        context.Request.Scheme = "https";
        return next();
    });
}

app.MapScalarApiReference(options =>
{
    options.Title = "LeveLEO API Документація";
    options.WithTheme(ScalarTheme.Kepler);
    options.AddHttpAuthentication("Bearer", scheme => { });
    options.AddPreferredSecuritySchemes("Bearer");
});

app.MapOpenApi();
app.MapControllers();

// =====================================================
// ПІДПИСКА НА ПОДІЇ
// =====================================================
var eventBus = app.Services.GetRequiredService<IEventBus>();

// Email notifications
eventBus.Subscribe<OrderCreatedEvent, OrderCreatedEmailHandler>();
eventBus.Subscribe<OrderPaidEvent, OrderPaidEmailHandler>();
eventBus.Subscribe<OrderShippedEvent, OrderShippedEmailHandler>();
eventBus.Subscribe<OrderCompletedEvent, OrderCompletedEmailHandler>();
eventBus.Subscribe<ReviewApprovedEvent, ReviewApprovedEmailHandler>();
eventBus.Subscribe<ReviewRejectedEvent, ReviewRejectedEmailHandler>();

// Admin task creation
eventBus.Subscribe<ReviewCreatedEvent, ReviewCreatedTaskHandler>();
eventBus.Subscribe<OrderPaidEvent, OrderPaidTaskHandler>();
eventBus.Subscribe<PaymentOrderMismatchEvent, PaymentMismatchTaskHandler>();
// Newsletter events
eventBus.Subscribe<ProductCreatedEvent, ProductCreatedNewsletterHandler>();
eventBus.Subscribe<PromotionCreatedEvent, PromotionCreatedNewsletterHandler>();

Log.Information("✅ Event handlers subscribed successfully");

// =====================================================
// RUN APP
// =====================================================
try
{
    Log.Information("🚀 Starting LeveLEO application...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ Application terminated unexpectedly");
}
finally
{
    SerilogConfiguration.EnsureSerilogFlushed();
}