using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;
using Ro.Inventario.Api.Models;
using Ro.Inventario.Core.Entities;
using Ro.Inventario.Api.Models;
using Ro.Inventario.Core.Repos;

public static class Productos
{
    public static async Task<IResult> GetAll(
        int pagina, int items,
        IGaleriaRepo galeriaRepo)
    {
        var res = await galeriaRepo.Page(pagina, items);

        var result = new ProductosPaginadosDTO();
        result.Productos = res.Select(it => it.Item1);
        result.Paginacion = res.Select(it => it.Item2).FirstOrDefault() ?? 
        new PaginationInfo()
        {
            PaginaActual = pagina,
            RowsPorPagina = items,
            TotalRows = 0,
            TotalPaginas = 0
        };

        return Results.Ok(result);
    }

    public static async Task<IResult> GetById(int id, 
        IBusquedaProductosRepo prod,
        IFotoProductoRepo foto,
        IUrlContentProductRepo content,
        IConfiguration configuration,
        ILogger<Program> logger)
    {
        var producto = await prod.GetOne(id);
        
        if (producto == null)
            return Results.NotFound();

        var baseUrl = configuration["MinIO:PublicBaseUrl"];
        var bucket = configuration["MinIO:Buckets:FotosProductos"];

        var fotos = await foto.GetForProduct(producto.Id);
        var videos = await content.GetForProduct(producto.Id);

        logger.LogInformation("Producto: {id}", producto.Id);
        logger.LogInformation("Fotos encontradas: {count}", fotos.Count());

        
        return Results.Ok(new ProductoDto(
            producto.Nid,
            producto.Nombre,
            producto.PrecioVenta,
            producto.UnidadMedida,
            producto.Categoria,
            producto.Stock,
            fotos.Select(f => $"{baseUrl}/{bucket}/{f.FileName}").ToArray(),
            videos.Select(v => v.Url).ToArray()
        ));
    }    
}

public record ProductoDto(
    int Id, 
    string Nombre, 
    decimal Precio,
    string UnidadMedida,
    string Categoria,
    decimal Stock,
    string[] Fotos,
    string[] Videos
);