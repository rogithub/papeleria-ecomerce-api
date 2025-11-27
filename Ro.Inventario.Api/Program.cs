using System.Text.Json.Serialization;
using Ro.Inventario.Core.Repos;
using Ro.Inventario.Core.Entities;
using Ro.Npgsql.Data;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

var pg_host = builder.Configuration.GetSection("PostgresDb:Host").Value;
var pg_port = builder.Configuration.GetSection("PostgresDb:Port").Value;
var pg_db = builder.Configuration.GetSection("PostgresDb:Database").Value;
var pg_user = builder.Configuration.GetSection("PostgresDb:Username").Value;
var pg_pass = builder.Configuration.GetSection("PostgresDb:Password").Value;

string connString = $"Host={pg_host};Port={pg_port};Database={pg_db};Username={pg_user};Password={pg_pass};";

builder.Services.AddTransient<IDbAsync>((svc) =>
            {
                return new Database(connString);
            });

builder.Services.AddScoped<IBusquedaProductosRepo, BusquedaProductosRepo>();
builder.Services.AddScoped<IFotoProductoRepo, FotoProductoRepo>();
builder.Services.AddScoped<IUrlContentProductRepo, UrlContentProductRepo>();
builder.Services.AddScoped<IGaleriaRepo, GaleriaRepo>();


// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("AllowedOrigins")
            .Get<string[]>();

        policy.WithOrigins(allowedOrigins??Array.Empty<string>())
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});



var app = builder.Build();

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Crear grupos de endpoints
var productos = app.MapGroup("/api/productos");

productos.MapGet("/", Productos.GetAll);
productos.MapGet("/{id}", Productos.GetById);


app.Run();

[JsonSerializable(typeof(ProductoDto))]
[JsonSerializable(typeof(IEnumerable<ItemPortadaGaleria>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}