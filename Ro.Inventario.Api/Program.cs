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

Todo[] sampleTodos =
[
    new(1, "Walk the dog"),
    new(2, "Do the dishes", DateOnly.FromDateTime(DateTime.Now)),
    new(3, "Do the laundry", DateOnly.FromDateTime(DateTime.Now.AddDays(1))),
    new(4, "Clean the bathroom"),
    new(5, "Clean the car", DateOnly.FromDateTime(DateTime.Now.AddDays(2)))
];

var todosApi = app.MapGroup("/todos");
todosApi.MapGet("/", () => sampleTodos)
        .WithName("GetTodos");

todosApi.MapGet("/{id}", Results<Ok<Todo>, NotFound> (int id) =>
    sampleTodos.FirstOrDefault(a => a.Id == id) is { } todo
        ? TypedResults.Ok(todo)
        : TypedResults.NotFound())
    .WithName("GetTodoById");

app.Run();

public record Todo(int Id, string? Title, DateOnly? DueBy = null, bool IsComplete = false);

[JsonSerializable(typeof(Todo[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}
