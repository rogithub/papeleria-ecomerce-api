using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;

public static class Galeria
{
    public static IResult GetAll()
    {
        var productos = new[] 
        { 
            new ProductoDto(1, "Producto 1", 10.99m),
            new ProductoDto(2, "Producto 2", 20.99m)
        };
        return Results.Ok(productos);
    }

    public static IResult GetById(int id)
    {
        var producto = new ProductoDto(id, $"Producto {id}", 15.99m);
        return Results.Ok(producto);
    }

    public static IResult Create(ProductoDto producto)
    {
        return Results.Created($"/api/productos/{producto.Id}", producto);
    }

    public static IResult Update(int id, ProductoDto producto)
    {
        return Results.Ok(producto);
    }

    public static IResult Delete(int id)
    {
        return Results.NoContent();
    }
}

public record ProductoDto(int Id, string Nombre, decimal Precio);