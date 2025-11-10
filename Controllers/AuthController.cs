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
            try 
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
                    PasswordHash = PasswordHelper.Hash(request.Password), 
                    Role = "User",
                    NombreEmpresa = request.NombreEmpresa,
                    TipoNegocio = request.TipoNegocio,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    // DefaultWarehouseId se deja null por ahora para evitar el ciclo
                };

                // 1. Crear la Bodega por defecto (Warehouse)
                var warehouse = new Warehouse
                {
                    Id = Guid.NewGuid(),
                    Name = $"Bodega Principal - {request.NombreEmpresa}",
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow
                };
                
                // Agregar ambas entidades al contexto
                _context.Users.Add(user);
                _context.Warehouses.Add(warehouse);

                // 2. PRIMER SAVE: Guarda el User y el Warehouse. (Rompe el ciclo)
                await _context.SaveChangesAsync(); 

                // 3. Asignar la Bodega al usuario (DefaultWarehouseId)
                user.DefaultWarehouseId = warehouse.Id;

                // 4. Actualizar la entidad User en el contexto
                _context.Users.Update(user); 
                
                // 5. SEGUNDO SAVE: Guarda solo el cambio de DefaultWarehouseId.
                await _context.SaveChangesAsync(); 

                // Generar el token (Ahora sabemos que DefaultWarehouseId.Value existe)
                var token = _jwt.GenerateToken(user.Id, user.Email, user.Role, user.DefaultWarehouseId.Value);

                // Devolver el token junto con datos de usuario
                return Ok(new 
                { 
                    Token = token, 
                    Email = user.Email, 
                    NombreEmpresa = user.NombreEmpresa 
                });
            }
            catch (Exception ex)
            {
                // Para depuración en Azure
                return StatusCode(500, new 
                { 
                    error = "Fallo CRÍTICO al comunicarse con la base de datos.", 
                    details = ex.Message 
                });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            // Buscar el usuario por email
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == request.Email);

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