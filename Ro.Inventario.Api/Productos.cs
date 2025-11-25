using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;
using Ro.Inventario.Core.Entities;
using Ro.Inventario.Core.Repos;

public static class Productos
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

    public static async Task<IResult> GetById(int id, 
        IBusquedaProductosRepo prod,
        IFotoProductoRepo foto,
        IConfiguration configuration,
        ILogger<Program> logger)
    {
        var producto = await prod.GetOne(id);
        
        if (producto == null)
            return Results.NotFound();

        var baseUrl = configuration["MinIO:PublicBaseUrl"];
        var bucket = configuration["MinIO:Buckets:FotosProductos"];

        var fotos = await foto.GetForProduct(producto.Id);

        producto.PrecioCompraPromedio = 0m;
        producto.Fotos = fotos.Select(f => new WebFileInfo(){
                Id = f.Id,
                UrlContentPath = $"{baseUrl}/{bucket}/{f.FileName}",
                DisplayName = string.Empty
            });
        
        logger.LogInformation("Producto: {id}", producto.Id);
        logger.LogInformation("Fotos encontradas: {count}", fotos.Count());

        
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