using Microsoft.EntityFrameworkCore;
using CambioDivisasP2P.CORE.Core.Entities; // Ajusta según la ruta donde se guardó


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueApp",
        policy =>
        {
            policy
                .WithOrigins("http://localhost:9000", "http://localhost:9001")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

// Add services to the container.
var _config = builder.Configuration;
var cnx = _config.GetConnectionString("DefaultConnection");
Console.WriteLine($"CONEXION = {cnx}");

builder.Services.AddDbContext<CambioDivisasP2PContext>(options =>
  options.UseSqlServer(cnx));

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();
var app = builder.Build();

app.UseCors("AllowVueApp");

//arreglar contraseñas incriptada
Console.WriteLine(
    BCrypt.Net.BCrypt.HashPassword("123456")
);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseStaticFiles();

app.Run();
