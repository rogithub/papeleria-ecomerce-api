using Ro.Inventario.Core.Entities;
using Ro.Inventario.Core.Repos;

public record PedidoItemRequest(Guid ProductoId, decimal Cantidad);
public record CrearPedidoRequest(string Nombre, string Telefono, PedidoItemRequest[] Items);
public record PedidoCreadoResponse(int PedidoId, Guid PedidoUid, Guid ClienteId);

public static class Pedidos
{
    public static async Task<IResult> Crear(
        CrearPedidoRequest req,
        IClientesRepo clientesRepo,
        IPedidosRepo pedidosRepo,
        ILogger<Program> logger)
    {
        if (string.IsNullOrWhiteSpace(req.Telefono) ||
            req.Telefono.Length != 10 ||
            !req.Telefono.All(char.IsDigit))
        {
            return Results.BadRequest("El teléfono debe tener exactamente 10 dígitos numéricos.");
        }

        if (string.IsNullOrWhiteSpace(req.Nombre))
            return Results.BadRequest("El nombre es requerido.");

        if (req.Items == null || req.Items.Length == 0)
            return Results.BadRequest("El pedido debe tener al menos un artículo.");

        // Buscar cliente por teléfono exacto
        var resultados = await clientesRepo.Search(req.Telefono);
        var cliente = resultados.FirstOrDefault(c => c.Telefono == req.Telefono);

        if (cliente == null)
        {
            cliente = new Cliente
            {
                Nombre = req.Nombre.Trim(),
                Telefono = req.Telefono
            };
            await clientesRepo.Save(cliente);
            logger.LogInformation("Cliente creado: {Id} {Telefono}", cliente.Id, cliente.Telefono);
        }
        else
        {
            logger.LogInformation("Cliente existente: {Id} {Telefono}", cliente.Id, cliente.Telefono);
        }

        var pedido = new Pedido
        {
            ClienteId = cliente.Id,
            Estatus = EstatusPedido.Nuevo,
            Origen = OrigenPedido.EnLinea,
            Items = req.Items.Select(i => new PedidoItem
            {
                ProductoId = i.ProductoId,
                Cantidad = i.Cantidad
            }).ToArray()
        };

        await pedidosRepo.Save(pedido);
        var saved = (await pedidosRepo.GetOne(pedido.Uid))!;
        logger.LogInformation("Pedido online creado: {Id} {Uid} cliente {ClienteId}", saved.Id, pedido.Uid, cliente.Id);

        return Results.Ok(new PedidoCreadoResponse(saved.Id, pedido.Uid, cliente.Id));
    }
}
