using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using cs_api_v1.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
        .UseSnakeCaseNamingConvention());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// HTTPS redirection ditangani oleh nginx reverse proxy (TLS termination),
// jadi tidak dilakukan di aplikasi untuk menghindari redirect loop.

app.MapControllers();

app.Run();
