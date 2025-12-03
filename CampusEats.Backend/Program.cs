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

var builder = WebApplication.CreateBuilder(args);

// DATABASE CONFIG
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.EnableDynamicJson();
var dataSource = dataSourceBuilder.Build();
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(dataSource));

// STRIPE CONFIG
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));

builder.Services.AddSingleton<Stripe.StripeClient>(provider =>
{
    var stripeSettings = builder.Configuration.GetSection("Stripe").Get<StripeSettings>();
    return new Stripe.StripeClient(stripeSettings!.SecretKey);
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

// AUTHENTICATION
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
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

// CORS (replace with dynamic for any localhost port if needed)
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
                return uri.Host == "localhost" || uri.Host == "127.0.0.1";
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

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

// DATABASE SEEDING
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(context);
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

// Procesare plată (POST /api/payments/process)
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

// Istoric plăți pentru user
// Merged endpoint (replace the two duplicates with this single block)
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


// Istoric plăți pentru o comandă
paymentsGroup.MapGet("/order/{orderId:guid}", async (Guid orderId, ISender sender, ClaimsPrincipal user) =>
    {
        // Poți adăuga validări aici ca la orders
        var payments = await sender.Send(new GetOrderPayments.Query(orderId));
        return Results.Ok(payments);
    })
    .WithName("GetOrderPayments")
    .RequireAuthorization()
    .Produces<List<PaymentDto>>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status401Unauthorized);

// STRIPE WEBHOOK ENDPOINT
app.MapPost("/api/payments/webhook/stripe", async (HttpRequest req, AppDbContext db) =>
{
    string json = await new StreamReader(req.Body).ReadToEndAsync();
    var stripeSettings = app.Services.GetRequiredService<IConfiguration>().GetSection("Stripe").Get<StripeSettings>();
    var stripeEvent = Stripe.EventUtility.ConstructEvent(json, req.Headers["Stripe-Signature"], stripeSettings?.WebhookSecret);

    if (stripeEvent.Type == "payment_intent.succeeded")
    {
        var intent = stripeEvent.Data.Object as PaymentIntent;
        var payment = await db.Payments.FirstOrDefaultAsync(p => intent != null && p.StripePaymentIntentId == intent.Id);
        if (payment != null)
        {
            payment.Status = PaymentStatus.Completed;
            payment.PaidAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }
    else if (stripeEvent.Type == "payment_intent.payment_failed")
    {
        var intent = stripeEvent.Data.Object as PaymentIntent;
        var payment = await db.Payments.FirstOrDefaultAsync(p => intent != null && p.StripePaymentIntentId == intent.Id);
        if (payment != null)
        {
            payment.Status = PaymentStatus.Failed;
            payment.FailureReason = intent?.LastPaymentError?.Message ?? "Stripe reported failed payment";
            payment.FailedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }
    return Results.Ok();
});

paymentsGroup.MapPatch("/{paymentId:guid}/refund", async (Guid paymentId, string? reason, ClaimsPrincipal user, ISender sender) =>
    {
        // Doar Staff/Admin poate refunda!
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
        // Securitate: doar Staff/Admin
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

// Obține punctele curente ale user-ului
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

// Istoric tranzacții loyalty
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

// Redeem puncte pentru discount
loyaltyGroup.MapPost("/redeem", async (RedeemLoyaltyPoints.Command command, ClaimsPrincipal user, ISender sender) =>
{
    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var authenticatedUserId))
        return Results.Unauthorized();

    // User poate redeem doar pentru propriul său cont
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

// Award points (doar Admin/Staff)
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