using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;
using Ro.Inventario.Core.Repos;
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


// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Crear grupos de endpoints
var galeria = app.MapGroup("/api/galeria");

galeria.MapGet("/", Galeria.GetAll);
galeria.MapGet("/{id}", Galeria.GetById);
galeria.MapPost("/", Galeria.Create);
galeria.MapPut("/{id}", Galeria.Update);
galeria.MapDelete("/{id}", Galeria.Delete);


app.Run();

[JsonSerializable(typeof(ProductoDto))]
[JsonSerializable(typeof(ProductoDto[]))]
[JsonSerializable(typeof(List<ProductoDto>))]
[JsonSerializable(typeof(IEnumerable<ProductoDto>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}