using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using BodegApp.Backend.Data;
using BodegApp.Backend.Models;
using BodegApp.Backend.DTOs;
using System; // Necesario para Guid


namespace BodegApp.Backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class EgresoController : ControllerBase
    {
        private readonly InventoryContext _context;

        public EgresoController(InventoryContext context)
        {
            _context = context;
        }

        private Guid GetUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost]
        public async Task<IActionResult> RetirarLote([FromBody] EgresoRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserId();

            var warehouseId = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.DefaultWarehouseId)
                .FirstOrDefaultAsync();

            if (warehouseId == null)
                return BadRequest("No se encontró bodega por defecto.");

            // 1. Encontrar el lote que coincide con el Código y las Unidades/Caja (UnitsPerBox)
            var batch = await _context.ItemBatches
                .FirstOrDefaultAsync(b =>
                    b.ProductCode == request.ProductCode &&
                    b.UnitsPerBox == request.UnitsPerBox &&
                    b.UserId == userId &&
                    b.WarehouseId == warehouseId.Value);

            if (batch == null)
                return NotFound("No existe lote con ese código y unidades por caja.");

            if (request.Boxes <= 0)
                return BadRequest("Cantidad inválida.");

            if (batch.Boxes < request.Boxes)
                return BadRequest("No hay suficientes cajas en el lote.");

            // 2. Actualizar el stock del lote
            batch.Boxes -= request.Boxes;
            batch.UpdatedAt = DateTime.UtcNow;

            // 3. Registrar el movimiento de stock
            _context.StockMovements.Add(new StockMovement
            {
                UserId = userId,
                ProductCode = batch.ProductCode,
                BoxesAfterChange = batch.Boxes,
                Delta = -request.Boxes,
                Action = "Salida",
                Timestamp = DateTime.UtcNow,
                BatchId = batch.Id,
                // 🚨 CORRECCIÓN CRÍTICA AÑADIDA: Asignar UnitsPerBox del lote
                UnitsPerBox = batch.UnitsPerBox,
                ProductName = batch.Name 
            });

            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Egreso de lote realizado con éxito. Por favor, refresque el inventario." });
        }
    }
}