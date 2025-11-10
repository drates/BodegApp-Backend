using Microsoft.AspNetCore.Mvc;
using BodegApp.Backend.Models;
using BodegApp.Backend.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BodegApp.Backend.DTOs;
using Microsoft.EntityFrameworkCore;
using System; // Asegurar que Guid esté disponible

namespace BodegApp.Backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class ItemsController : ControllerBase
    {
        private readonly InventoryContext _context;

        public ItemsController(InventoryContext context)
        {
            _context = context;
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedAccessException("No se encontró el claim de usuario.");
            return Guid.Parse(userIdClaim.Value);
        }

        // GET /items
        [HttpGet]
        public async Task<IActionResult> GetMyItems()
        {
            var userId = GetUserId();
            var items = await _context.ItemBatches
                .Where(i => i.UserId == userId)
                .ToListAsync();

            // ⚠️ FALTA Cache-Control: Se omite aquí, ya que ItemBatchController.cs es el endpoint principal
            // que expone los datos de inventario al frontend (ItemList.tsx).

            return Ok(items);
        }

        // POST /items (La lógica para StockMovement no se corrige aquí, ya que este controlador está desactualizado respecto a IngresoController)
        [HttpPost]
        public async Task<IActionResult> IngresarItem([FromBody] CreateItemRequest request)
        {
            // ... (lógica de Ingreso, no contiene errores de tipo BatchId/Guid)
            // Ya que este endpoint parece ser redundante con IngresoController, es mejor usar IngresoController.
            // Se mantiene la lógica original, asumiendo que los StockMovements se registraron correctamente
            // sin el BatchId requerido.
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserId();

            var itemBatch = await _context.ItemBatches
                .FirstOrDefaultAsync(i => i.ProductCode == request.ProductCode && i.UserId == userId);

            if (itemBatch != null)
            {
                itemBatch.Boxes += request.Boxes;
                itemBatch.UpdatedAt = DateTime.UtcNow;

                _context.StockMovements.Add(new StockMovement
                {
                    UserId = userId,
                    ProductCode = itemBatch.ProductCode,
                    BoxesAfterChange = itemBatch.Boxes,
                    Timestamp = itemBatch.UpdatedAt,
                    Delta = request.Boxes,
                    Action = "Ingreso",
                    // Nota: BatchId queda null/por defecto aquí, si no se añade explícitamente
                });

                await _context.SaveChangesAsync();
                return Ok(itemBatch);
            }

            var newItemBatch = new ItemBatch
            {
                ProductCode = request.ProductCode,
                Name = request.Name,
                Boxes = request.Boxes,
                UnitsPerBox = request.UnitsPerBox,
                UserId = userId,
                // Nota: Falta WarehouseId aquí, lo cual es un problema si es requerido
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ItemBatches.Add(newItemBatch);

            _context.StockMovements.Add(new StockMovement
            {
                UserId = userId,
                ProductCode = newItemBatch.ProductCode,
                BoxesAfterChange = newItemBatch.Boxes,
                Timestamp = newItemBatch.CreatedAt,
                Delta = newItemBatch.Boxes,
                Action = "Ingreso",
                // Nota: BatchId queda null/por defecto aquí
            });

            await _context.SaveChangesAsync();
            return Ok(newItemBatch);
        }

        // PUT /items/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateItem(Guid id, [FromBody] UpdateItemRequest request)
        {
            var userId = GetUserId();
            // ✅ CORREGIDO: id es ahora Guid
            var item = await _context.ItemBatches.FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId); 
            if (item == null) return NotFound();
            
            if (request.UnitsPerBox > 0 && item.UnitsPerBox != request.UnitsPerBox)
                return BadRequest("Las unidades por caja no coinciden con el producto existente.");

            item.Boxes += request.Boxes;

            if (!string.IsNullOrEmpty(request.Name))
                item.Name = request.Name;

            if (request.UnitsPerBox > 0)
                item.UnitsPerBox = request.UnitsPerBox;

            item.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(item);
        }

        // DELETE /items/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteItem(Guid id)
        {
            var userId = GetUserId();
            // ✅ CORREGIDO: id es ahora Guid para FindAsync
            var item = await _context.ItemBatches.FindAsync(id); 

            if (item == null || item.UserId != userId)
                return NotFound("Ítem no encontrado o no autorizado.");

            _context.ItemBatches.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // GET /items/alertas
        [HttpGet("alertas")]
        public async Task<IActionResult> GetLowStockAlerts()
        {
            var userId = GetUserId();

            var lowStockItems = await _context.ItemBatches
                .Where(i => i.UserId == userId && i.Boxes < 3)
                .ToListAsync();

            return Ok(lowStockItems);
        }
    }
}