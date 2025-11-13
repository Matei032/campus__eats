using System.Security.Claims;
using System.Text;
using CampusEats.Backend.Common.Behaviors;
using CampusEats.Backend.Common.DTOs;
using CampusEats.Backend.Common.Services;
using CampusEats.Backend.Features.Authentication;
using CampusEats.Backend.Features.Kitchen;
using CampusEats.Backend.Features.Menu;
using CampusEats.Backend.Features.Orders;
using CampusEats.Backend.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// ========== DATABASE CONFIG ==========
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.EnableDynamicJson();
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(dataSource));

// ========== MEDIATR + VALIDATION ==========
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// ========== JWT SERVICE ==========
builder.Services.AddScoped<IJwtService, JwtService>();

// ========== AUTHENTICATION ==========
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
            ClockSkew = TimeSpan.Zero
        };
    });

// ========== AUTHORIZATION ==========
builder.Services.AddAuthorization();

// ========== SWAGGER ==========
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
    
    // JWT authentication in Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token."
    });
    
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ========== CORS CONFIG ==========
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5086",
                "https://localhost:7263",
                "http://localhost:8000"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// ========== USE CORS ==========
app.UseCors("AllowFrontend");

// ========== DEV TOOLS ==========
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

// ========== AUTHENTICATION & AUTHORIZATION MIDDLEWARE ==========
app.UseAuthentication();
app.UseAuthorization();

// ========== DATABASE SEEDING ==========
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(context);
}

// ============================================================
// AUTHENTICATION ENDPOINTS
// ============================================================
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
    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.BadRequest(new { errors = result.Errors });
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
    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.NotFound(new { errors = result.Errors });
})
.WithName("GetCurrentUser")
.RequireAuthorization()
.Produces<UserDto>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized)
.Produces<object>(StatusCodes.Status404NotFound);

// ============================================================
// MENU ENDPOINTS
// ============================================================
var menuGroup = app.MapGroup("api/menu").WithTags("Menu");

menuGroup.MapGet("/", async (ISender sender) =>
{
    var result = await sender.Send(new GetMenu.Query());
    return result.IsSuccess 
        ? Results.Ok(result.Value) 
        : Results.BadRequest(new { error = result.Error });
})
.WithName("GetMenu")
.AllowAnonymous()
.Produces<List<ProductDto>>(StatusCodes.Status200OK);

menuGroup.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
{
    var result = await sender.Send(new GetProductById.Query(id));
    return result.IsSuccess 
        ? Results.Ok(result.Value) 
        : Results.NotFound(new { error = result.Error });
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
    return result.IsSuccess 
        ? Results.NoContent() 
        : Results.BadRequest(new { error = result.Error });
})
.WithName("DeleteProduct")
.Produces(StatusCodes.Status204NoContent)
.Produces<object>(StatusCodes.Status400BadRequest);

// ============================================================
// ORDERS ENDPOINTS (SECURED)
// ============================================================
var ordersGroup = app.MapGroup("api/orders").WithTags("Orders");

ordersGroup.MapPost("/", async (CreateOrder.Command command, ClaimsPrincipal user, ISender sender) =>
{
    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                   ?? user.FindFirst("sub")?.Value;
    
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var authenticatedUserId))
    {
        return Results.Unauthorized();
    }

    // User can only create orders for themselves
    if (command.UserId != authenticatedUserId)
    {
        return Results.Forbid();
    }

    var result = await sender.Send(command);
    return result.IsSuccess
        ? Results.Created($"/api/orders/{result.Value!.Id}", result.Value)
        : Results.BadRequest(new { errors = result.Errors });
})
.WithName("CreateOrder")
.RequireAuthorization()
.Produces<OrderDto>(StatusCodes.Status201Created)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status403Forbidden)
.Produces<object>(StatusCodes.Status400BadRequest);

ordersGroup.MapGet("/user/{userId:guid}", async (Guid userId, ClaimsPrincipal user, ISender sender) =>
{
    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                   ?? user.FindFirst("sub")?.Value;
    
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var authenticatedUserId))
    {
        return Results.Unauthorized();
    }

    // User can only view their own orders (unless Staff/Admin)
    var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
    if (userId != authenticatedUserId && userRole != "Staff" && userRole != "Admin")
    {
        return Results.Forbid();
    }

    var result = await sender.Send(new GetUserOrders.Query { UserId = userId });
    return result.IsSuccess 
        ? Results.Ok(result.Value) 
        : Results.BadRequest(new { errors = result.Errors });
})
.WithName("GetUserOrders")
.RequireAuthorization()
.Produces<List<OrderDto>>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status403Forbidden)
.Produces<object>(StatusCodes.Status400BadRequest);

ordersGroup.MapGet("/{orderId:guid}", async (Guid orderId, ClaimsPrincipal user, ISender sender) =>
{
    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                   ?? user.FindFirst("sub")?.Value;
    
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var authenticatedUserId))
    {
        return Results.Unauthorized();
    }

    var result = await sender.Send(new GetOrderById.Query { OrderId = orderId, UserId = authenticatedUserId });
    return result.IsSuccess 
        ? Results.Ok(result.Value) 
        : Results.NotFound(new { errors = result.Errors });
})
.WithName("GetOrderById")
.RequireAuthorization()
.Produces<OrderDto>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized)
.Produces<object>(StatusCodes.Status404NotFound);

ordersGroup.MapDelete("/{orderId:guid}", async (Guid orderId, string? cancellationReason, ClaimsPrincipal user, ISender sender) =>
{
    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                   ?? user.FindFirst("sub")?.Value;
    
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
    return result.IsSuccess 
        ? Results.Ok(result.Value) 
        : Results.BadRequest(new { errors = result.Errors });
})
.WithName("CancelOrder")
.RequireAuthorization()
.Produces<OrderDto>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized)
.Produces<object>(StatusCodes.Status400BadRequest);

// ============================================================
// KITCHEN ENDPOINTS (STAFF ONLY)
// ============================================================
var kitchenGroup = app.MapGroup("api/kitchen").WithTags("Kitchen");

kitchenGroup.MapGet("/pending", async (ISender sender) =>
{
    var result = await sender.Send(new GetPendingOrders.Query());
    return result.IsSuccess 
        ? Results.Ok(result.Value) 
        : Results.BadRequest(new { errors = result.Errors });
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
    return result.IsSuccess 
        ? Results.Ok(result.Value) 
        : Results.BadRequest(new { errors = result.Errors });
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
    return result.IsSuccess 
        ? Results.Ok(result.Value) 
        : Results.BadRequest(new { errors = result.Errors });
})
.WithName("GetDailyInventoryReport")
.RequireAuthorization(policy => policy.RequireRole("Staff", "Admin"))
.Produces<object>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status403Forbidden)
.Produces<object>(StatusCodes.Status400BadRequest);

app.Run();