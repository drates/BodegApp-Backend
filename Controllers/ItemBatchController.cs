using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims; // Se mantiene solo una vez
using Microsoft.EntityFrameworkCore;
using BodegApp.Backend.Data;
using BodegApp.Backend.Models;
using BodegApp.Backend.DTOs;
// using System.Security.Claims; // <-- ELIMINADA línea duplicada (CS0105)

namespace BodegApp.Backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("itembatches")]
    public class ItemBatchController : ControllerBase
    {
        private readonly InventoryContext _context;

        public ItemBatchController(InventoryContext context)
        {
            _context = context;
        }

        private Guid GetUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ItemBatchDto>>> GetItemBatches()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userIdString == null)
            {
                return Unauthorized("Token de usuario no válido o faltante.");
            }

            var userId = Guid.Parse(userIdString);

            var batches = await _context.ItemBatches
                .Where(b => b.UserId == userId && b.Boxes > 0) 
                .Select(b => new ItemBatchDto
                {
                    // 🚨 CORRECCIÓN ASUMIDA: Si ItemBatchDto.Id es 'int', aquí está el error CS0029. 
                    // Si ItemBatchDto.Id es Guid, esta línea está correcta.
                    Id = b.Id, // Esta línea (la línea 47 en su código) es donde falla si ItemBatchDto.Id es int
                    ProductCode = b.ProductCode,
                    Name = b.Name,
                    Boxes = b.Boxes,
                    UnitsPerBox = b.UnitsPerBox
                })
                .ToListAsync();

            // ✅ CORRECCIÓN ASP0019: Usar Append para evitar ArgumentException
            Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            Response.Headers.Append("Pragma", "no-cache"); 
            Response.Headers.Append("Expires", "0"); 

            return Ok(batches);
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

        // Si existen otros endpoints (POST, PUT, DELETE) en este controller, deben ser revisados individualmente.
    }
}