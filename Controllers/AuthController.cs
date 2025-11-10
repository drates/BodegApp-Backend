using Microsoft.AspNetCore.Mvc;
using BodegApp.Backend.Data;
using BodegApp.Backend.DTOs;
using BodegApp.Backend.Models;
using BodegApp.Backend.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization; 

namespace BodegApp.Backend.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly InventoryContext _context;
        private readonly JwtService _jwt;

        public AuthController(InventoryContext context, JwtService jwt)
        {
            _context = context;
            _jwt = jwt;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            // Manejar el error de Email Duplicado
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return Conflict(new { message = "El correo electrónico ya está registrado. Por favor, inicia sesión o usa otro correo." });
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                // Usamos el método estático directamente
                PasswordHash = PasswordHelper.Hash(request.Password), 
                Role = "User",
                NombreEmpresa = request.NombreEmpresa,
                TipoNegocio = request.TipoNegocio,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // 1. Crear la Bodega Principal por defecto
            var warehouse = new Warehouse
            {
                Id = Guid.NewGuid(),
                Name = "Bodega Principal",
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.Warehouses.Add(warehouse);
            await _context.SaveChangesAsync();

            user.DefaultWarehouseId = warehouse.Id;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // 2. Generar y devolver el token con datos de usuario
            var token = _jwt.GenerateToken(user.Id, user.Email, user.Role, user.DefaultWarehouseId.Value); 
            
            // Devolver formato completo para login automático en el frontend
            return Ok(new 
            { 
                Token = token, 
                Email = user.Email, 
                NombreEmpresa = user.NombreEmpresa 
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var user = await _context.Users
                                     .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || user.DefaultWarehouseId == null)
                return Unauthorized("Credenciales inválidas.");

            // Usamos Verify() para comparar la contraseña de entrada con el hash de la base de datos.
            bool isPasswordValid = PasswordHelper.Verify(request.Password, user.PasswordHash);
            
            if (!isPasswordValid)
                return Unauthorized("Credenciales inválidas.");

            // Usamos el ID de la Bodega del usuario
            var token = _jwt.GenerateToken(user.Id, user.Email, user.Role, user.DefaultWarehouseId.Value);

            // Devolver el token junto con datos de usuario
            return Ok(new 
            { 
                Token = token, 
                Email = user.Email, 
                NombreEmpresa = user.NombreEmpresa 
            });
        }

        [HttpGet("me")]
        [Authorize] 
        public IActionResult Me()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "N/A";
            var email = User.FindFirstValue(ClaimTypes.Email) ?? "N/A";
            var role = User.FindFirstValue(ClaimTypes.Role) ?? "N/A";
            var warehouseId = User.FindFirstValue("WarehouseId") ?? "N/A"; 

            return Ok(new
            {
                UserId = userId,
                Email = email,
                Role = role,
                WarehouseId = warehouseId
            });
        }
    }
}