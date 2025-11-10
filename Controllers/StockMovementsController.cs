using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BodegApp.Backend.Data;
using Microsoft.EntityFrameworkCore;
using BodegApp.Backend.Models;
using System; 
using BodegApp.Backend.DTOs;

[ApiController]
[Route("stockmovements")]
public class StockMovementsController : ControllerBase
{
    private readonly InventoryContext _context;

    public StockMovementsController(InventoryContext context)
    {
        _context = context;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> RegisterMovement([FromBody] CreateStockMovementRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // ✅ CORRECCIÓN CRÍTICA: Exigir BatchId para movimientos
        if (request.BatchId == null || request.BatchId == Guid.Empty)
            return BadRequest(new { message = "BatchId es obligatorio para registrar un movimiento de stock." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // ✅ CORRECCIÓN: Buscar exactamente por BatchId
        var batch = await _context.ItemBatches
            .FirstOrDefaultAsync(b =>
                b.Id == request.BatchId.Value && 
                b.UserId == Guid.Parse(userId!));

        if (batch == null)
            return NotFound("No existe lote para el BatchId especificado.");

        var movement = new StockMovement
        {
            UserId = Guid.Parse(userId!),
            ProductCode = request.ProductCode,
            ProductName = batch.Name,
            Action = request.Action,
            BoxesAfterChange = request.BoxesAfterChange,
            Delta = request.Delta,
            UnitsPerBox = batch.UnitsPerBox,
            BatchId = batch.Id,
            Timestamp = DateTime.UtcNow
        };

        _context.StockMovements.Add(movement);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Movimiento registrado con éxito." });
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetMyMovements([FromQuery] string? productCode)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var cutoffDate = DateTime.UtcNow.AddDays(-60);

        var query = _context.StockMovements
            .Where(m => m.UserId == userId && m.Timestamp >= cutoffDate);

        if (!string.IsNullOrEmpty(productCode))
        {
            query = query.Where(m => m.ProductCode == productCode);
        }

        var movements = await query
            .OrderByDescending(m => m.Timestamp)
            .ToListAsync();

        Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
        Response.Headers.Append("Pragma", "no-cache"); 
        Response.Headers.Append("Expires", "0"); 

        return Ok(movements.Select(m => new
        {
            m.ProductCode,
            m.ProductName,
            m.Action,
            m.Delta,
            m.BoxesAfterChange,
            m.UnitsPerBox,
            m.BatchId, 
            m.Timestamp
        }));
    }
}