
using Ro.Inventario.Core.Entities;

namespace  Ro.Inventario.Api.Models;

public class ProductosPaginadosDTO
{
    public ProductosPaginadosDTO()
    {
        Productos = Array.Empty<ItemPortadaGaleria>();
        Paginacion = new PaginationInfo();
    }
    public IEnumerable<ItemPortadaGaleria> Productos { get; set; }
    public PaginationInfo Paginacion { get; set; }
}