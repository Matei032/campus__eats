using CampusEats.Backend.Common.Behaviors;
using CampusEats.Backend.Common.DTOs;
using CampusEats.Backend.Persistence;
using CampusEats.Backend.Features.Menu;
using CampusEats.Backend.Features.Orders;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.EnableDynamicJson(); // Enable dynamic JSON serialization
var dataSource = dataSourceBuilder.Build();

// Database with configured data source
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(dataSource));

// MediatR with Validation Behavior
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    // Use full type names to avoid schema ID conflicts
    options.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Disable HTTPS redirect in development
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// ===== DATABASE SEEDING =====
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    // Apply pending migrations
    await context.Database.MigrateAsync();
    
    // Seed data
    await DatabaseSeeder.SeedAsync(context);
}

var menuGroup = app.MapGroup("api/menu").WithTags("Menu");

// GET /api/menu - Get all products
menuGroup.MapGet("/", async (ISender sender) =>
    {
        var query = new GetMenu.Query();
        var result = await sender.Send(query);
        
        return result.IsSuccess 
            ? Results.Ok(result.Value) 
            : Results.BadRequest(new { error = result.Error });
    })
    .WithName("GetMenu")
    .Produces<List<ProductDto>>();

// GET /api/menu/{id} - Get product by ID
menuGroup.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
    {
        var query = new GetProductById.Query(id);
        var result = await sender.Send(query);
        
        return result.IsSuccess 
            ? Results.Ok(result.Value) 
            : Results.NotFound(new { error = result.Error });
    })
    .WithName("GetProductById")
    .Produces<ProductDto>()
    .Produces(404);

// POST /api/menu - Create new product
menuGroup.MapPost("/", async (CreateProduct.Command command, ISender sender) =>
    {
        var result = await sender.Send(command);
        
        if (!result.IsSuccess)
        {
            return Results.BadRequest(new { errors = result.Errors.Any() ? result.Errors : new List<string> { result.Error! } });
        }
        
        return Results.CreatedAtRoute("GetProductById", new { id = result.Value!.Id }, result.Value);
    })
    .WithName("CreateProduct")
    .Produces<ProductDto>(201)
    .Produces(400);

// PUT /api/menu/{id} - Update product
menuGroup.MapPut("/{id:guid}", async (Guid id, UpdateProduct.Command command, ISender sender) =>
    {
        if (id != command.Id)
        {
            return Results.BadRequest(new { error = "ID in route does not match ID in body" });
        }
        
        var result = await sender.Send(command);
        
        if (!result.IsSuccess)
        {
            return Results.BadRequest(new { errors = result.Errors.Any() ? result.Errors : new List<string> { result.Error! } });
        }
        
        return Results.Ok(result.Value);
    })
    .WithName("UpdateProduct")
    .Produces<ProductDto>()
    .Produces(400);

// DELETE /api/menu/{id} - Delete product
menuGroup.MapDelete("/{id:guid}", async (Guid id, ISender sender) =>
    {
        var command = new DeleteProduct.Command(id);
        var result = await sender.Send(command);
        
        if (!result.IsSuccess)
        {
            return Results.BadRequest(new { error = result.Error });
        }
        
        return Results.NoContent();
    })
    .WithName("DeleteProduct")
    .Produces(204)
    .Produces(400);

// ORDERS ENDPOINTS

var ordersGroup = app.MapGroup("api/orders").WithTags("Orders");

// POST /api/orders - Create new order
ordersGroup.MapPost("/", async (CreateOrder.Command command, ISender sender) =>
    {
        var result = await sender.Send(command);
    
        return result.IsSuccess
            ? Results.Created($"/api/orders/{result.Value!.Id}", result.Value)
            : Results.BadRequest(new { errors = result.Errors });
    })
    .WithName("CreateOrder")
    .Produces<OrderDto>(StatusCodes.Status201Created)
    .Produces<object>(StatusCodes.Status400BadRequest);

// GET /api/orders/user/{userId} - Get user's orders
ordersGroup.MapGet("/user/{userId:guid}", async (Guid userId, ISender sender) =>
    {
        var query = new GetUserOrders.Query { UserId = userId };
        var result = await sender.Send(query);
    
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { errors = result.Errors });
    })
    .WithName("GetUserOrders")
    .Produces<List<OrderDto>>(StatusCodes.Status200OK)
    .Produces<object>(StatusCodes.Status400BadRequest);

// GET /api/orders/{orderId} - Get order by ID
ordersGroup.MapGet("/{orderId:guid}", async (Guid orderId, Guid userId, ISender sender) =>
    {
        // userId from query parameter: /api/orders/{orderId}?userId={userId}
        var query = new GetOrderById.Query { OrderId = orderId, UserId = userId };
        var result = await sender.Send(query);
    
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { errors = result.Errors });
    })
    .WithName("GetOrderById")
    .Produces<OrderDto>(StatusCodes.Status200OK)
    .Produces<object>(StatusCodes.Status404NotFound);

// GET /api/orders/kitchen/pending - Get pending orders for kitchen
ordersGroup.MapGet("/kitchen/pending", async (ISender sender) =>
    {
        var query = new GetPendingOrders.Query();
        var result = await sender.Send(query);
    
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { errors = result.Errors });
    })
    .WithName("GetPendingOrders")
    .Produces<List<OrderDto>>(StatusCodes.Status200OK)
    .Produces<object>(StatusCodes.Status400BadRequest);

// PATCH /api/orders/{orderId}/status - Update order status
ordersGroup.MapPatch("/{orderId:guid}/status", async (Guid orderId, UpdateOrderStatus.Command command, ISender sender) =>
    {
        // Override orderId from route
        var updatedCommand = command with { OrderId = orderId };
        var result = await sender.Send(updatedCommand);
    
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { errors = result.Errors });
    })
    .WithName("UpdateOrderStatus")
    .Produces<OrderDto>(StatusCodes.Status200OK)
    .Produces<object>(StatusCodes.Status400BadRequest);

// DELETE /api/orders/{orderId} - Cancel order (soft delete)
ordersGroup.MapDelete("/{orderId:guid}", async (Guid orderId, Guid userId, string? cancellationReason, ISender sender) =>
    {
        // userId from query parameter: /api/orders/{orderId}?userId={userId}&cancellationReason={reason}
        var command = new CancelOrder.Command 
        { 
            OrderId = orderId, 
            UserId = userId,
            CancellationReason = cancellationReason
        };
    
        var result = await sender.Send(command);
    
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { errors = result.Errors });
    })
    .WithName("CancelOrder")
    .Produces<OrderDto>(StatusCodes.Status200OK)
    .Produces<object>(StatusCodes.Status400BadRequest);

app.Run();