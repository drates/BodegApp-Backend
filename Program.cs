using Microsoft.OpenApi.Models;
using BodegApp.Backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using BodegApp.Backend.Services;
using System.Security.Claims;
using BodegApp.Backend.Models;
using System.Text.Json;
using Npgsql.EntityFrameworkCore.PostgreSQL;

var builder = WebApplication.CreateBuilder(args);


// **********************************************
// 💡 CORRECCIÓN 1: Configuración de CORS ABIERTA
// **********************************************
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy
            // 💡 Temporalmente abierto para probar en Azure
            .AllowAnyOrigin() 
            .AllowAnyHeader()
            .AllowAnyMethod());
});


// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "BodegApp API", Version = "v1" });

    // Esto habilita el botón "Authorize"
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Token JWT. Escribe 'Bearer {tu token}'",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// **********************************************
// 💡 CORRECCIÓN 2: Conexión a PostgreSQL
// **********************************************
// Usamos la cadena de conexión definida en appsettings.json como "PostgresConnection"
var connectionString = builder.Configuration.GetConnectionString("PostgresConnection");
builder.Services.AddDbContext<InventoryContext>(options =>
    options.UseNpgsql(connectionString)
);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

// Registrar el servicio JWT
builder.Services.AddScoped<JwtService>();

// **********************************************
// 💡 CORRECCIÓN 3: Configuración de JWT (Leyendo la clave desde la configuración)
// **********************************************
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            // 💡 ESTO ES LO CRÍTICO: Leer la clave desde la configuración
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
        };
    });

builder.Services.AddAuthorization();


var app = builder.Build();

// **********************************************
// 💡 CORRECCIÓN 4: Inicialización del Superadmin (Leyendo credenciales)
// **********************************************
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<InventoryContext>();
    
    // Ejecutar migraciones
    context.Database.Migrate();

    // 💡 Leer las credenciales del Superadmin desde la configuración
    var superadminEmail = builder.Configuration["Superadmin:Email"];
    var superadminPassword = builder.Configuration["Superadmin:Password"];

    // Si las credenciales no están definidas, no se puede crear el usuario.
    if (string.IsNullOrEmpty(superadminEmail) || string.IsNullOrEmpty(superadminPassword))
    {
        // Esto puede ocurrir en entornos donde se olvidan de inyectar las variables
        Console.WriteLine("⚠️ ADVERTENCIA: Credenciales de Superadmin faltantes en la configuración. No se creará el usuario inicial.");
    }
    else
    {
        EnsureSuperadminExists(context, superadminEmail, superadminPassword);
    }
}


// Middlewares
app.UseCors("AllowFrontend");


// Enable Swagger in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();


// **********************************************
// Método auxiliar para crear Superadmin (sin cambios lógicos, solo refactorizado)
// **********************************************
void EnsureSuperadminExists(InventoryContext context, string superadminEmail, string superadminPassword)
{
    // Solo si el usuario no existe.
    if (!context.Users.Any(u => u.Email == superadminEmail))
    {
        var adminId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();

        // 1. Crear el Superadmin
        var superadmin = new User
        {
            Id = adminId, // Asignación directa de Guid
            Email = superadminEmail,
            PasswordHash = PasswordHelper.Hash(superadminPassword),
            Role = "Superadmin",
            // Campos NOT NULL obligatorios para la base de datos
            NombreEmpresa = "BodegApp Global Admin", 
            TipoNegocio = "Administracion Central",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        context.Users.Add(superadmin);
        context.SaveChanges();

        // 2. Crear la Bodega Principal y vincularla al Superadmin
        var warehouse = new Warehouse
        {
            Id = warehouseId, // Asignación directa de Guid
            Name = "Bodega Central Admin",
            UserId = adminId, // Asignación directa de Guid
            CreatedAt = DateTime.UtcNow
        };
        context.Warehouses.Add(warehouse);
        context.SaveChanges();

        // 3. Asignar la bodega al Superadmin
        superadmin.DefaultWarehouseId = warehouseId; // Asignación directa de Guid
        context.Users.Update(superadmin);
        context.SaveChanges(); // Persistir la asignación de la Bodega por defecto
        
        Console.WriteLine($"✅ Superadmin '{superadminEmail}' creado con éxito y Bodega inicial asignada.");
    }
}