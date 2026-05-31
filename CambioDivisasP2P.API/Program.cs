using Microsoft.EntityFrameworkCore;
using CambioDivisasP2P.CORE.Core.Entities; // Ajusta según la ruta donde se guardó


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var _config = builder.Configuration;
var cnx = _config.GetConnectionString("DevConnection");

builder.Services.AddDbContext<CambioDivisasP2PContext>(options =>
  options.UseSqlServer(cnx));

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
