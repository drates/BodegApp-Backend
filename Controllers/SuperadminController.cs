using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BodegApp.Backend.Data;
using System.Security.Claims;



namespace BodegApp.Backend.Controllers
{
    [ApiController]
    [Route("superadmin")]
    [Authorize(Roles = "Superadmin")]
    public class SuperadminController : ControllerBase
    {
        private readonly InventoryContext _context;

        public SuperadminController(InventoryContext context)
        {
            _context = context;
        }

        [HttpGet("metricas")]
        public async Task<IActionResult> GetSuperadminMetrics()
        {
            var now = DateTime.UtcNow;
            var cutoff30 = now.AddDays(-30);

            var users = await _context.Users.ToListAsync();
            var batches = await _context.ItemBatches.ToListAsync();
            var movements = await _context.StockMovements
                .Where(m => m.Timestamp >= cutoff30)
                .ToListAsync();

            var usuariosPorDia = users
                .GroupBy(u => u.CreatedAt.Date)
                .Select(g => new { Fecha = g.Key, Nuevos = g.Count() });

            var movimientosPorDia = movements
                .GroupBy(m => m.Timestamp.Date)
                .Select(g => new {
                    Fecha = g.Key,
                    Total = g.Count(),
                    UnidadesEgresadas = g.Where(m => m.Action == "Egreso").Sum(m => m.Delta),
                    CajasEgresadas = g.Where(m => m.Action == "Egreso").Sum(m => m.Delta / m.UnitsPerBox)
                });

            var resumenGlobal = new {
                UsuariosTotales = users.Count,
                ProductosTotales = batches.Select(b => b.ProductCode).Distinct().Count(),
                CajasTotales = batches.Sum(b => b.Boxes),
                UnidadesTotales = batches.Sum(b => b.Boxes * b.UnitsPerBox),
                MovimientosTotales = movements.Count,
                IngresosTotales = movements.Where(m => m.Action == "Ingreso").Sum(m => m.Delta),
                EgresosTotales = movements.Where(m => m.Action == "Egreso").Sum(m => m.Delta)
            };

            var resumenPorUsuario = users.Select(u => {
                var userBatches = batches.Where(b => b.UserId == u.Id);
                var userMovements = movements.Where(m => m.UserId == u.Id);

                return new {
                    UserId = u.Id,
                    UserName = u.UserName,
                    Productos = userBatches.Select(b => b.ProductCode).Distinct().Count(),
                    Cajas = userBatches.Sum(b => b.Boxes),
                    Unidades = userBatches.Sum(b => b.Boxes * b.UnitsPerBox),
                    UnidadesEgresadas30d = userMovements
                        .Where(m => m.Action == "Egreso")
                        .Sum(m => m.Delta)
                };
            });

            return Ok(new {
                UsuariosPorDia = usuariosPorDia,
                MovimientosPorDia = movimientosPorDia,
                ResumenGlobal = resumenGlobal,
                TablaPorUsuario = resumenPorUsuario
            });
        }

        [HttpGet("lotes")]
public async Task<IActionResult> GetLotesPorUsuario([FromQuery] Guid userId)
{
    var batches = await _context.ItemBatches
        .Where(b => b.UserId == userId)
        .Include(b => b.Warehouse)
        .OrderByDescending(b => b.UpdatedAt)
        .ToListAsync();

    var resultado = batches.Select(b => new {
        b.Id,
        b.ProductCode,
        b.UnitsPerBox,
        b.Boxes,
        b.UpdatedAt,
        Warehouse = b.Warehouse.Name
    });

    return Ok(resultado);
}

    }
}
