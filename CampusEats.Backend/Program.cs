using CampusEats.Backend.Common.Behaviors;
using CampusEats.Backend.Common.DTOs;
using CampusEats.Backend.Persistence;
using CampusEats.Backend.Features.Menu;
using CampusEats.Backend.Features.Orders;
using CampusEats.Backend.Features.Kitchen;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
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

// ========== SWAGGER ==========
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
});

// ========== CORS CONFIG ==========
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5086",  // Blazor WASM (HTTP) - launchSettings
                "https://localhost:7263", // Blazor WASM (HTTPS) - launchSettings
                "http://localhost:8000"   // Nginx/dev container (dacă îl folosiți)
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
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

// ========== DATABASE SEEDING ==========
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    await context.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(context);
}

// ========== MENU ENDPOINTS ==========
var menuGroup = app.MapGroup("api/menu").WithTags("Menu");

menuGroup.MapGet("/", async (ISender sender) =>
{
    var result = await sender.Send(new GetMenu.Query());
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
});

menuGroup.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
{
    var result = await sender.Send(new GetProductById.Query(id));
    return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(new { error = result.Error });
})
.WithName("GetProductById");

menuGroup.MapPost("/", async (CreateProduct.Command command, ISender sender) =>
{
    var result = await sender.Send(command);
    if (!result.IsSuccess)
        return Results.BadRequest(new { errors = result.Errors.Any() ? result.Errors : new List<string> { result.Error! } });

    return Results.CreatedAtRoute("GetProductById", new { id = result.Value!.Id }, result.Value);
});

menuGroup.MapPut("/{id:guid}", async (Guid id, UpdateProduct.Command command, ISender sender) =>
{
    if (id != command.Id)
        return Results.BadRequest(new { error = "ID in route does not match ID in body" });

    var result = await sender.Send(command);
    if (!result.IsSuccess)
        return Results.BadRequest(new { errors = result.Errors.Any() ? result.Errors : new List<string> { result.Error! } });

    return Results.Ok(result.Value);
});

menuGroup.MapDelete("/{id:guid}", async (Guid id, ISender sender) =>
{
    var result = await sender.Send(new DeleteProduct.Command(id));
    return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
});

// ========== ORDERS ENDPOINTS ==========
var ordersGroup = app.MapGroup("api/orders").WithTags("Orders");

ordersGroup.MapPost("/", async (CreateOrder.Command command, ISender sender) =>
{
    var result = await sender.Send(command);
    return result.IsSuccess
        ? Results.Created($"/api/orders/{result.Value!.Id}", result.Value)
        : Results.BadRequest(new { errors = result.Errors });
});

ordersGroup.MapGet("/user/{userId:guid}", async (Guid userId, ISender sender) =>
{
    var result = await sender.Send(new GetUserOrders.Query { UserId = userId });
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { errors = result.Errors });
});

// Notă: acest endpoint cere obligatoriu userId în query (autorizație simplificată)
ordersGroup.MapGet("/{orderId:guid}", async (Guid orderId, Guid userId, ISender sender) =>
{
    var result = await sender.Send(new GetOrderById.Query { OrderId = orderId, UserId = userId });
    return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(new { errors = result.Errors });
});

ordersGroup.MapDelete("/{orderId:guid}", async (Guid orderId, Guid userId, string? cancellationReason, ISender sender) =>
{
    var result = await sender.Send(new CancelOrder.Command
    {
        OrderId = orderId,
        UserId = userId,
        CancellationReason = cancellationReason
    });
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { errors = result.Errors });
});

// ========== KITCHEN ENDPOINTS ==========
var kitchenGroup = app.MapGroup("api/kitchen").WithTags("Kitchen");

kitchenGroup.MapGet("/pending", async (ISender sender) =>
{
    var result = await sender.Send(new GetPendingOrders.Query());
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { errors = result.Errors });
});

kitchenGroup.MapPatch("/orders/{orderId:guid}/status", async (Guid orderId, UpdateOrderStatus.Command command, ISender sender) =>
{
    var updatedCommand = command with { OrderId = orderId };
    var result = await sender.Send(updatedCommand);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { errors = result.Errors });
});

kitchenGroup.MapGet("/inventory/daily", async (DateTime? reportDate, ISender sender) =>
{
    var result = await sender.Send(new GetDailyInventoryReport.Query { ReportDate = reportDate });
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { errors = result.Errors });
});

app.Run();