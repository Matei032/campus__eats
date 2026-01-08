using System.Security.Claims;
using System.Text;
using CampusEats.Backend.Common;
using CampusEats.Backend.Common.Behaviors;
using CampusEats.Backend.Common.Config;
using CampusEats.Backend.Common.DTOs;
using CampusEats.Backend.Common.Services;
using CampusEats.Backend.Features.Authentication;
using CampusEats.Backend.Features.Kitchen;
using CampusEats.Backend.Features.Menu;
using CampusEats.Backend.Features.Orders;
using CampusEats.Backend.Features.Payments;
using CampusEats.Backend.Persistence;
using CampusEats.Backend.Features.Loyalty;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Stripe;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
    WebRootPath = "wwwroot"
});

// Dezactivează file watching în producție
if (!builder.Environment.IsDevelopment())
{
    builder.Configuration.Sources
        .OfType<Microsoft.Extensions.Configuration.Json.JsonConfigurationSource>()
        .ToList()
        .ForEach(s => s.ReloadOnChange = false);
}

// ✅ CONFIGURARE PORT PENTRU RENDER
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// ✅ DATABASE CONFIG - folosește connection string exact cum vine
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");

if (string.IsNullOrEmpty(connectionString))
{
    // Local development
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    Console.WriteLine("📍 Using local database connection");
}
else
{
    // Production - folosește exact ce vine din DATABASE_URL
    Console.WriteLine("📍 Using Render database connection");
    
    // Debug: afișează ultimele caractere pentru verificare
    var lastPart = connectionString.Length > 50 ? connectionString.Substring(connectionString.Length - 50) : connectionString;
    Console.WriteLine($"🔗 Connection string ends with: ...{lastPart}");
}

// Construiește data source DIRECT, fără modificări
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.EnableDynamicJson();
var dataSource = dataSourceBuilder.Build();
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(dataSource));

// ✅ STRIPE CONFIG - suportă environment variables
var stripeSecretKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY") 
                      ?? builder.Configuration["Stripe:SecretKey"] 
                      ?? "";

var stripePublicKey = Environment.GetEnvironmentVariable("STRIPE_PUBLIC_KEY") 
                      ?? builder.Configuration["Stripe:PublicKey"] 
                      ?? "";

var stripeWebhookSecret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET") 
                          ?? builder.Configuration["Stripe:WebhookSecret"] 
                          ?? "";

builder.Services.Configure<StripeSettings>(options =>
{
    options.PublicKey = stripePublicKey;
    options.SecretKey = stripeSecretKey;
    options.WebhookSecret = stripeWebhookSecret;
});

builder.Services.AddSingleton<Stripe.StripeClient>(provider =>
{
    return new Stripe.StripeClient(stripeSecretKey);
});

// MEDIATR + VALIDATION
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// JWT SERVICE
builder.Services.AddScoped<IJwtService, JwtService>();

// ✅ AUTHENTICATION - suportă environment variables pentru JWT
var jwtSecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") 
    ?? builder.Configuration["Jwt:SecretKey"]!;

var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") 
    ?? builder.Configuration["Jwt:Issuer"]!;

var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") 
    ?? builder.Configuration["Jwt:Audience"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.NameIdentifier
        };
    });

// AUTHORIZATION
builder.Services.AddAuthorization();

// SWAGGER
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization", Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer", BearerFormat = "JWT", In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token."
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        { new Microsoft.OpenApi.Models.OpenApiSecurityScheme { Reference = new Microsoft.OpenApi.Models.OpenApiReference { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});

// ✅ CORS - permite localhost + render.com
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
        {
            if (string.IsNullOrWhiteSpace(origin)) return false;
            try
            {
                var uri = new Uri(origin);
                return uri.Host == "localhost" 
                    || uri.Host == "127.0.0.1" 
                    || uri.Host.EndsWith(".onrender.com")
                    || uri.Host.EndsWith(".render.com");
            }
            catch { return false; }
        })
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

var app = builder.Build();
app.UseCors("AllowFrontend");

// ✅ Swagger disponibil și în producție pentru testare
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();

// DEV AUTH BYPASS — DOAR ÎN DEVELOPMENT
if (app.Environment.IsDevelopment())
{
    app.Use(async (ctx, next) =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api")
            && !(ctx.User?.Identity?.IsAuthenticated ?? false))
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var devUser = await db.Users.OrderByDescending(u => u.CreatedAt).FirstOrDefaultAsync(u => u.Role == "Admin" || u.Role == "Staff")
                ?? await db.Users.OrderByDescending(u => u.CreatedAt).FirstOrDefaultAsync();
            if (devUser != null)
            {
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, devUser.Id.ToString()),
                    new("sub", devUser.Id.ToString()),
                    new(ClaimTypes.Email, devUser.Email),
                    new(ClaimTypes.Name, string.IsNullOrWhiteSpace(devUser.FullName) ? devUser.Email : devUser.FullName),
                    new(ClaimTypes.Role, string.IsNullOrWhiteSpace(devUser.Role) ? "Admin" : devUser.Role),
                    new("role", string.IsNullOrWhiteSpace(devUser.Role) ? "Admin" : devUser.Role)
                };
                ctx.User = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "DevBypass"));
            }
        }
        await next();
    });
}
app.UseAuthorization();

// ✅ DATABASE MIGRATION & SEEDING cu logging îmbunătățit
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        Console.WriteLine("🔄 Applying migrations...");
        await context.Database.MigrateAsync();
        Console.WriteLine("✅ Migrations applied successfully");
        
        Console.WriteLine("🌱 Seeding database...");
        await DatabaseSeeder.SeedAsync(context);
        Console.WriteLine("✅ Database seeded successfully");
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "❌ Error during migration or seeding");
        throw;
    }
}

// ================== AUTH ENDPOINTS ==================
var authGroup = app.MapGroup("api/auth").WithTags("Authentication");

authGroup.MapPost("/register", async (Register.Command command, ISender sender) =>
{
    var result = await sender.Send(command);
    return result.IsSuccess
        ? Results.Created($"/api/users/{result.Value!.Id}", result.Value)
        : Results.BadRequest(new { errors = result.Errors });
})
.WithName("Register")
.AllowAnonymous()
.Produces<UserDto>(StatusCodes.Status201Created)
.Produces<object>(StatusCodes.Status400BadRequest);

authGroup.MapPost("/login", async (Login.Query query, ISender sender) =>
{
    var result = await sender.Send(query);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { errors = result.Errors });
})
.WithName("Login")
.AllowAnonymous()
.Produces<AuthResponseDto>(StatusCodes.Status200OK)
.Produces<object>(StatusCodes.Status400BadRequest);

authGroup.MapGet("/me", async (ClaimsPrincipal user, ISender sender) =>
{
    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? user.FindFirst("sub")?.Value;

    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var result = await sender.Send(new GetCurrentUser.Query { UserId = userId });
    return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(new { errors = result.Errors });
})
.WithName("GetCurrentUser")
.RequireAuthorization()
.Produces<UserDto>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized)
.Produces<object>(StatusCodes.Status404NotFound);

// ================== MENU ENDPOINTS ==================
var menuGroup = app.MapGroup("api/menu").WithTags("Menu");

menuGroup.MapGet("/", async (ISender sender) =>
{
    var result = await sender.Send(new GetMenu.Query());
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
})
.WithName("GetMenu")
.AllowAnonymous()
.Produces<List<ProductDto>>(StatusCodes.Status200OK);

menuGroup.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
{
    var result = await sender.Send(new GetProductById.Query(id));
    return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(new { error = result.Error });
})
.WithName("GetProductById")
.AllowAnonymous()
.Produces<ProductDto>(StatusCodes.Status200OK)
.Produces<object>(StatusCodes.Status404NotFound);

menuGroup.MapPost("/", async (CreateProduct.Command command, ISender sender) =>
{
    var result = await sender.Send(command);
    if (!result.IsSuccess)
        return Results.BadRequest(new { errors = result.Errors.Any() ? result.Errors : new List<string> { result.Error! } });

    return Results.CreatedAtRoute("GetProductById", new { id = result.Value!.Id }, result.Value);
})
.WithName("CreateProduct")
.Produces<ProductDto>(StatusCodes.Status201Created)
.Produces<object>(StatusCodes.Status400BadRequest);

menuGroup.MapPut("/{id:guid}", async (Guid id, UpdateProduct.Command command, ISender sender) =>
{
    if (id != command.Id)
        return Results.BadRequest(new { error = "ID in route does not match ID in body" });

    var result = await sender.Send(command);
    if (!result.IsSuccess)
        return Results.BadRequest(new { errors = result.Errors.Any() ? result.Errors : new List<string> { result.Error! } });

    return Results.Ok(result.Value);
})
.WithName("UpdateProduct")
.Produces<ProductDto>(StatusCodes.Status200OK)
.Produces<object>(StatusCodes.Status400BadRequest);

menuGroup.MapDelete("/{id:guid}", async (Guid id, ISender sender) =>
{
    var result = await sender.Send(new DeleteProduct.Command(id));
    return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
})
.WithName("DeleteProduct")
.Produces(StatusCodes.Status204NoContent)
.Produces<object>(StatusCodes.Status400BadRequest);

// ================== ORDERS ENDPOINTS (SECURED) ==================
var ordersGroup = app.MapGroup("api/orders").WithTags("Orders");

ordersGroup.MapPost("/", async (CreateOrder.Command command, ClaimsPrincipal user, ISender sender) =>
{
    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var authenticatedUserId))
    {
        return Results.Unauthorized();
    }
    if (command.UserId != authenticatedUserId) { return Results.Forbid(); }

    var result = await sender.Send(command);
    return result.IsSuccess ? Results.Created($"/api/orders/{result.Value!.Id}", result.Value) : Results.BadRequest(new { errors = result.Errors });
})
.WithName("CreateOrder")
.RequireAuthorization()
.Produces<OrderDto>(StatusCodes.Status201Created)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status403Forbidden)
.Produces<object>(StatusCodes.Status400BadRequest);

ordersGroup.MapGet("/user/{userId:guid}", async (Guid userId, ClaimsPrincipal user, ISender sender) =>
{
    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var authenticatedUserId))
    {
        return Results.Unauthorized();
    }
    var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
    if (userId != authenticatedUserId && userRole != "Staff" && userRole != "Admin")
    {
        return Results.Forbid();
    }
    var result = await sender.Send(new GetUserOrders.Query { UserId = userId });
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { errors = result.Errors });
})
.WithName("GetUserOrders")
.RequireAuthorization()
.Produces<List<OrderDto>>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status403Forbidden)
.Produces<object>(StatusCodes.Status400BadRequest);

ordersGroup.MapGet("/{orderId:guid}", async (Guid orderId, ClaimsPrincipal user, ISender sender) =>
{
    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var authenticatedUserId))
    {
        return Results.Unauthorized();
    }
    var result = await sender.Send(new GetOrderById.Query { OrderId = orderId, UserId = authenticatedUserId });
    return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(new { errors = result.Errors });
})
.WithName("GetOrderById")
.RequireAuthorization()
.Produces<OrderDto>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized)
.Produces<object>(StatusCodes.Status404NotFound);

ordersGroup.MapDelete("/{orderId:guid}", async (Guid orderId, string? cancellationReason, ClaimsPrincipal user, ISender sender) =>
{
    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var authenticatedUserId))
    {
        return Results.Unauthorized();
    }
    var result = await sender.Send(new CancelOrder.Command
    {
        OrderId = orderId,
        UserId = authenticatedUserId,
        CancellationReason = cancellationReason
    });
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { errors = result.Errors });
})
.WithName("CancelOrder")
.RequireAuthorization()
.Produces<OrderDto>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized)
.Produces<object>(StatusCodes.Status400BadRequest);

// ================== KITCHEN ENDPOINTS ==================
var kitchenGroup = app.MapGroup("api/kitchen").WithTags("Kitchen");

kitchenGroup.MapGet("/pending", async (ISender sender) =>
{
    var result = await sender.Send(new GetPendingOrders.Query());
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { errors = result.Errors });
})
.WithName("GetPendingOrders")
.RequireAuthorization(policy => policy.RequireRole("Staff", "Admin"))
.Produces<List<OrderDto>>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status403Forbidden)
.Produces<object>(StatusCodes.Status400BadRequest);

kitchenGroup.MapPatch("/orders/{orderId:guid}/status", async (Guid orderId, UpdateOrderStatus.Command command, ISender sender) =>
{
    var updatedCommand = command with { OrderId = orderId };
    var result = await sender.Send(updatedCommand);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { errors = result.Errors });
})
.WithName("UpdateOrderStatus")
.RequireAuthorization(policy => policy.RequireRole("Staff", "Admin"))
.Produces<OrderDto>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status403Forbidden)
.Produces<object>(StatusCodes.Status400BadRequest);

kitchenGroup.MapGet("/inventory/daily", async (DateTime? reportDate, ISender sender) =>
{
    var result = await sender.Send(new GetDailyInventoryReport.Query { ReportDate = reportDate });
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { errors = result.Errors });
})
.WithName("GetDailyInventoryReport")
.RequireAuthorization(policy => policy.RequireRole("Staff", "Admin"))
.Produces<object>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status403Forbidden)
.Produces<object>(StatusCodes.Status400BadRequest);

// ================== PAYMENTS ENDPOINT ==================
var paymentsGroup = app.MapGroup("api/payments").WithTags("Payments");

paymentsGroup.MapPost("/process", async (ProcessPayment.Command command, ISender sender) =>
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { errors = result.Errors });
    })
    .WithName("ProcessPayment")
    .RequireAuthorization()
    .Produces<PaymentDto>(StatusCodes.Status200OK)
    .Produces<object>(StatusCodes.Status400BadRequest);

paymentsGroup.MapGet("/user/{userId:guid}/history", async (Guid userId, int? page, int? pageSize, string? status, string? method, ISender sender, ClaimsPrincipal user) =>
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
        var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var currentUserId))
            return Results.Unauthorized();
        if (userId != currentUserId && userRole != "Staff" && userRole != "Admin")
            return Results.Forbid();

        if (page.HasValue && pageSize.HasValue)
        {
            var result = await sender.Send(new GetUserPayments.Query(userId, page.Value, pageSize.Value, status, method));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { errors = result.Errors });
        }
        else
        {
            var payments = await sender.Send(new GetUserPayments.Query(userId));
            return Results.Ok(payments);
        }
    })
    .WithName("GetUserPaymentHistory")
    .RequireAuthorization()
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status401Unauthorized)
    .Produces(StatusCodes.Status403Forbidden);

paymentsGroup.MapGet("/order/{orderId:guid}", async (Guid orderId, ISender sender, ClaimsPrincipal user) =>
    {
        var payments = await sender.Send(new GetOrderPayments.Query(orderId));
        return Results.Ok(payments);
    })
    .WithName("GetOrderPayments")
    .RequireAuthorization()
    .Produces<List<PaymentDto>>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status401Unauthorized);

// ✅ STRIPE WEBHOOK ENDPOINT - cu error handling îmbunătățit
app.MapPost("/api/payments/webhook/stripe", async (HttpRequest req, AppDbContext db, IConfiguration config) =>
{
    string json = await new StreamReader(req.Body).ReadToEndAsync();
    
    var webhookSecret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET") 
        ?? config["Stripe:WebhookSecret"];
    
    try
    {
        var stripeEvent = Stripe.EventUtility.ConstructEvent(
            json, 
            req.Headers["Stripe-Signature"], 
            webhookSecret
        );

        if (stripeEvent.Type == "checkout.session.completed")
        {
            var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
            if (session != null && session.Metadata.TryGetValue("OrderId", out var orderIdStr) && Guid.TryParse(orderIdStr, out var orderId))
            {
                var payment = await db.Payments.FirstOrDefaultAsync(p => p.StripeSessionId == session.Id);
                var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
                
                if (payment != null && order != null)
                {
                    payment.Status = PaymentStatus.Completed;
                    payment.PaidAt = DateTime.UtcNow;
                    order.PaymentStatus = "Paid";
                    await db.SaveChangesAsync();
                    
                    Console.WriteLine($"✅ Payment completed for order {orderId}");
                }
            }
        }
        else if (stripeEvent.Type == "checkout.session.expired")
        {
            var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
            if (session != null)
            {
                var payment = await db.Payments.FirstOrDefaultAsync(p => p.StripeSessionId == session.Id);
                if (payment != null)
                {
                    payment.Status = PaymentStatus.Failed;
                    payment.FailureReason = "Session expired";
                    payment.FailedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                    
                    Console.WriteLine($"❌ Payment expired for session {session.Id}");
                }
            }
        }
        
        return Results.Ok();
    }
    catch (StripeException ex)
    {
        Console.WriteLine($"❌ Stripe webhook error: {ex.Message}");
        return Results.BadRequest(new { error = ex.Message });
    }
})
.AllowAnonymous();

paymentsGroup.MapPatch("/{paymentId:guid}/refund", async (Guid paymentId, string? reason, ClaimsPrincipal user, ISender sender) =>
    {
        var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
        if (userRole != "Staff" && userRole != "Admin")
            return Results.Forbid();

        var result = await sender.Send(new RefundPayment.Command(paymentId, reason));
        return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { errors = result.Errors });
    })
    .WithName("RefundPayment")
    .RequireAuthorization(policy => policy.RequireRole("Staff", "Admin"))
    .Produces<PaymentDto>(StatusCodes.Status200OK)
    .Produces<object>(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status403Forbidden);

paymentsGroup.MapGet("/", async (int page, int pageSize, string? status, string? method, ISender sender, ClaimsPrincipal user) =>
    {
        var userRole = user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (userRole != "Staff" && userRole != "Admin")
            return Results.Forbid();

        var result = await sender.Send(new GetAllPayments.Query(page, pageSize, status, method));
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { errors = result.Errors });
    })
    .WithName("GetAllPayments")
    .RequireAuthorization(policy => policy.RequireRole("Staff", "Admin"))
    .Produces<PagedResult<PaymentDto>>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status403Forbidden);

// ================== LOYALTY ENDPOINTS ==================
var loyaltyGroup = app.MapGroup("api/loyalty").WithTags("Loyalty");

loyaltyGroup.MapGet("/points", async (ClaimsPrincipal user, ISender sender) =>
{
    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        return Results.Unauthorized();

    var result = await sender.Send(new GetLoyaltyPoints.Query(userId));
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { errors = result.Errors });
})
.WithName("GetLoyaltyPoints")
.RequireAuthorization()
.Produces<LoyaltyPointsDto>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized)
.Produces<object>(StatusCodes.Status400BadRequest);

loyaltyGroup.MapGet("/transactions", async (ClaimsPrincipal user, int page, int pageSize, ISender sender) =>
{
    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        return Results.Unauthorized();

    var result = await sender.Send(new GetLoyaltyTransactions.Query(userId, page, pageSize));
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { errors = result.Errors });
})
.WithName("GetLoyaltyTransactions")
.RequireAuthorization()
.Produces<List<LoyaltyTransactionDto>>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized)
.Produces<object>(StatusCodes.Status400BadRequest);

loyaltyGroup.MapPost("/redeem", async (RedeemLoyaltyPoints.Command command, ClaimsPrincipal user, ISender sender) =>
{
    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var authenticatedUserId))
        return Results.Unauthorized();

    if (command.UserId != authenticatedUserId)
        return Results.Forbid();

    var result = await sender.Send(command);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { errors = result.Errors });
})
.WithName("RedeemLoyaltyPoints")
.RequireAuthorization()
.Produces<RedeemPointsResultDto>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status403Forbidden)
.Produces<object>(StatusCodes.Status400BadRequest);

loyaltyGroup.MapPost("/award", async (AwardLoyaltyPoints.Command command, ClaimsPrincipal user, ISender sender) =>
{
    var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
    if (userRole != "Staff" && userRole != "Admin")
        return Results.Forbid();

    var result = await sender.Send(command);
    return result.IsSuccess ? Results.Ok(new { message = "Points awarded successfully" }) : Results.BadRequest(new { errors = result.Errors });
})
.WithName("AwardLoyaltyPoints")
.RequireAuthorization(policy => policy.RequireRole("Staff", "Admin"))
.Produces<object>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status403Forbidden)
.Produces<object>(StatusCodes.Status400BadRequest);

app.Run();
