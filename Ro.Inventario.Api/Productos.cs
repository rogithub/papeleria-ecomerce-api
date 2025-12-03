using Ro.Inventario.Core.Repos;

public static class Productos
{
    public static async Task<IResult> GetAll(
        int pagina, int rows,        
        IGaleriaRepo galeriaRepo,
        ILogger<Program> logger,
        string? search = null)
    {
        pagina = Math.Max(1, Math.Min(pagina, 10000));
        rows = Math.Max(1, Math.Min(rows, 100));

        // Truncar a 150 caracteres
        if (!string.IsNullOrWhiteSpace(search))
        {           
            search = search.Length > 150 
                ? search[..150] 
                : search;
            logger.LogInformation("Long search text length: '{len}'", search.Length);
        }
        else
        {
            search = null;
        }

        var result = await galeriaRepo.Busqueda(search, pagina, rows);
        logger.LogInformation("Visita: página {pagina}, rows {rows}, búsqueda: '{search}'", 
            pagina, rows, search ?? string.Empty);
        
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
            producto.Id,
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
    Guid Id,
    int Nid, 
    string Nombre, 
    decimal PrecioVenta,
    string UnidadMedida,
    string Categoria,
    decimal Stock,
    string[] Fotos,
    string[] Videos
);