using dotenv.net;
using LeveLEO.Data;
using LeveLEO.Features.AdminTasks.Services;
using LeveLEO.Features.AttributeGroups.Services;
using LeveLEO.Features.Brands.Services;
using LeveLEO.Features.Categories.Services;
using LeveLEO.Features.Identity.Models;
using LeveLEO.Features.Identity.Services;
using LeveLEO.Features.Inventory.Services;
using LeveLEO.Features.Notifications.EventHandlers;
using LeveLEO.Features.Orders.Services;
using LeveLEO.Features.Payments.Services;
using LeveLEO.Features.ProductAttributes.Services;
using LeveLEO.Features.Products.Services;
using LeveLEO.Features.Promotions.Services;
using LeveLEO.Features.Shipping.Services;
using LeveLEO.Features.ShoppingCarts.Services;
using LeveLEO.Features.Statistics.Services;
using LeveLEO.Features.UserProductRelations.Services;
using LeveLEO.Features.Users.Services;
using LeveLEO.Infrastructure.Delivery;
using LeveLEO.Infrastructure.Email;
using LeveLEO.Infrastructure.Events;
using LeveLEO.Infrastructure.Events.DomainEvents;
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
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Swashbuckle.AspNetCore;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;

if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != "true")
{
    DotEnv.Load();
}
var builder = WebApplication.CreateBuilder(args);

// DB
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.User.RequireUniqueEmail = true;

    options.SignIn.RequireConfirmedEmail = true; // Вимагаємо підтвердження email
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();
// JWT SETTINGS
var jwtSettings = new JwtSettings
{
    Secret = Environment.GetEnvironmentVariable("JWT_SECRET")!,
    Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER")!,
    Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")!,
    AccessTokenExpirationMinutes = int.Parse(Environment.GetEnvironmentVariable("JWT_ACCESS_EXP") ?? "15"),
    RefreshTokenExpirationDays = int.Parse(Environment.GetEnvironmentVariable("JWT_REFRESH_EXP") ?? "7")
};
builder.Services.AddHttpClient();
builder.Services.AddSingleton(jwtSettings);
// Auth / Security services / User
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddScoped<ISocialAuthService, SocialAuthService>();

// Feature services
builder.Services.AddScoped<IAttributeGroupService, AttributeGroupService>();
builder.Services.AddScoped<IBrandService, BrandService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IOrderItemReviewService, OrderItemReviewService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IProductAttributeService, ProductAttributeService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICouponAssignmentService, CouponAssignmentService>();
builder.Services.AddScoped<IPromotionService, PromotionService>();
builder.Services.AddScoped<IShoppingCartService, ShoppingCartService>();
builder.Services.AddScoped<IUserProductRelationService, UserProductRelationService>();
builder.Services.AddScoped<IAddressService, AddressService>();
builder.Services.AddScoped<IDeliveryService, DeliveryService>();
// Admin Tasks
builder.Services.AddScoped<IAdminTaskService, AdminTaskService>();
// Statistics
builder.Services.AddScoped<IStatisticsService, StatisticsService>();
// Infrastructure services
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddTransient<ISmsSender, SmsSender>();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<ISlugGenerator, SlugGenerator>();
builder.Services.AddScoped<ILiqPayService, LiqPayService>();
builder.Services.AddHttpClient<INovaPoshtaService, NovaPoshtaService>();
builder.Services.AddScoped<IProductMediaService, ProductMediaService>();

// Event Bus
builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();
// Event Handlers - Email
builder.Services.AddScoped<OrderCreatedEmailHandler>();
builder.Services.AddScoped<OrderPaidEmailHandler>();
builder.Services.AddScoped<OrderShippedEmailHandler>();
builder.Services.AddScoped<OrderCompletedEmailHandler>();
builder.Services.AddScoped<ReviewApprovedEmailHandler>();
builder.Services.AddScoped<ReviewRejectedEmailHandler>();
// Event Handlers - Admin Tasks
builder.Services.AddScoped<ReviewCreatedTaskHandler>();
builder.Services.AddScoped<OrderPaidTaskHandler>();
builder.Services.AddScoped<PaymentMismatchTaskHandler>();

// CONTROLLERS
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
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
//builder.Services.AddAuthorization(options => { options.DefaultPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme).RequireAuthenticatedUser().Build(); });

//builder.Services.AddControllers()
//    .AddJsonOptions(options =>
//    {
//      options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
//    });

builder.Services.AddAuthorizationBuilder()
                                                                                                                                                                                                                                                                                                                                                                  //builder.Services.AddAuthorization(options => { options.DefaultPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme).RequireAuthenticatedUser().Build(); });//builder.Services.AddControllers()//    .AddJsonOptions(options =>//    {//      options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());//    });
                                                                                                                                                                                                                                                                                                                                                                  .SetDefaultPolicy(new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .Build());
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer(new BearerSecuritySchemeTransformer());
    options.AddOperationTransformer((operation, context, cancellationToken) =>
    {
        Console.WriteLine($"Processing endpoint: {context.Description.ActionDescriptor.DisplayName}");
        return Task.CompletedTask;
    });
});

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();
await DatabaseSeeder.SeedAsync(app);
app.UseMiddleware<GlobalExceptionHandler>();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();

app.MapScalarApiReference(options =>
{
    options.Title = "LeveLEO API Документація";
    options.WithTheme(ScalarTheme.Kepler);
    //    options.Servers = new List<ScalarServer>
    //{
    //    new ScalarServer("http://localhost:5158", "Локальний сервер")
    //};

    options.AddHttpAuthentication("Bearer", scheme =>
    {
    });

    options.AddPreferredSecuritySchemes("Bearer");
});
var transformer = new BearerSecuritySchemeTransformer();

app.MapOpenApi();

app.MapControllers();
// Підписуємо обробники подій
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
app.Run();