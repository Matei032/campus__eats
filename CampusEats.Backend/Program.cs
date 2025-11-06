using CampusEats.Backend.Persistence;
using CampusEats.Backend.Features.Menu; 
using FluentValidation;
using MediatR; 
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));


builder.Services.AddMediatR(cfg => 
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var menuGroup = app.MapGroup("api/menu").WithTags("Menu");

menuGroup.MapGet("/", async (ISender sender) =>
    {
        var query = new GetMenu.Query();
        var result = await sender.Send(query);
        return Results.Ok(result);
    })
    .Produces<List<GetMenu.ProductDto>>();

app.Run();