using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BodegApp.Backend.Data;
using BodegApp.Backend.Models;
using BodegApp.Backend.DTOs;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class IngresoController : ControllerBase
{
    private readonly InventoryContext _context;

    public IngresoController(InventoryContext context)
    {
        _context = context;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<IActionResult> IngresarLote([FromBody] CreateItemRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var userId = GetUserId();

            var warehouseId = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.DefaultWarehouseId)
                .FirstOrDefaultAsync();

            if (warehouseId == null)
                return BadRequest(new { message = "No se encontró el bodega por defecto." });

            // 1. LÓGICA DE BÚSQUEDA DEL LOTE EXISTENTE (ProductCode + UnitsPerBox)
            var existingBatch = await _context.ItemBatches
                .FirstOrDefaultAsync(b =>
                    b.ProductCode == request.ProductCode &&
                    b.UnitsPerBox == request.UnitsPerBox && // LA CLAVE PARA SEPARAR LOTES
                    b.UserId == userId &&
                    b.WarehouseId == warehouseId.Value);

            if (existingBatch != null)
            {
                // Lógica de actualización (EXISTING BATCH)
                existingBatch.Boxes += request.Boxes;
                existingBatch.UpdatedAt = DateTime.UtcNow;

                _context.StockMovements.Add(new StockMovement
                {
                    UserId = userId,
                    ProductCode = existingBatch.ProductCode,
                    BoxesAfterChange = existingBatch.Boxes,
                    Delta = request.Boxes,
                    Action = "Ingreso",
                    UnitsPerBox = existingBatch.UnitsPerBox,
                    // ✅ CORRECCIÓN 1: Usar el nombre del lote existente
                    ProductName = existingBatch.Name, 
                    BatchId = existingBatch.Id,
                    Timestamp = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                
                return Ok(new { message = "Ingreso de lote existente realizado con éxito. Por favor, refresque el inventario." });
            }

            // 2. LÓGICA DE CREACIÓN DEL NUEVO LOTE
            
            // ✅ CORRECCIÓN 2: Buscar un nombre de producto existente para asegurar consistencia.
            var referenceBatch = await _context.ItemBatches
                .Where(b => b.ProductCode == request.ProductCode && b.UserId == userId)
                .Select(b => new { b.Name })
                .FirstOrDefaultAsync();

                if (referenceBatch == null && string.IsNullOrWhiteSpace(request.Name))
{
    return BadRequest(new { message = "El producto es nuevo y requiere un nombre." });
}
                
            string consistentName = referenceBatch != null ? referenceBatch.Name : request.Name;

            var newBatch = new ItemBatch
            {
                ProductCode = request.ProductCode,
                Boxes = request.Boxes,
                UnitsPerBox = request.UnitsPerBox,
                UserId = userId,
                WarehouseId = warehouseId.Value,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Name = consistentName // <-- USANDO EL NOMBRE CONSISTENTE
            };

            _context.ItemBatches.Add(newBatch);
            await _context.SaveChangesAsync(); 

            _context.StockMovements.Add(new StockMovement
            {
                UserId = userId,
                ProductCode = newBatch.ProductCode,
                BoxesAfterChange = newBatch.Boxes,
                Delta = newBatch.Boxes,
                Action = "Entrada",
                UnitsPerBox = newBatch.UnitsPerBox,
                ProductName = newBatch.Name,
                BatchId = newBatch.Id, 
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Nuevo lote ingresado con éxito. Por favor, refresque el inventario." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Ocurrió un error en el servidor al ingresar el lote.", details = ex.Message });
        }
    }
}