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
using Microsoft.Extensions.Logging; // 🚨 NECESARIO para usar ILogger

var builder = WebApplication.CreateBuilder(args);


// **********************************************
// 1. CONFIGURACIÓN DE CORS
// **********************************************
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy
            // 💡 Usaremos AllowAnyOrigin para pruebas en Azure App Service.
            .AllowAnyOrigin() 
            .AllowAnyHeader()
            .AllowAnyMethod());
});


// **********************************************
// 2. REGISTRO DE SERVICIOS
// **********************************************
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

// Registro de DB Context con Npgsql
builder.Services.AddDbContext<InventoryContext>(options =>
{
    // Obtiene la cadena de conexión del Configuration (appsettings.json o Azure App Service Settings)
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection"));
});

// Registro de servicios propios
builder.Services.AddScoped<JwtService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Esto previene que Newtonsoft.Json use referencias cíclicas si accidentalmente las creamos.
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// Configuración de JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false, // Lo dejamos en false para simplificar
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();


// **********************************************
// 3. MIDDLEWARE
// **********************************************
var app = builder.Build();

app.UseCors("AllowFrontend");

// Enable Swagger in development (se recomienda desactivarlo en producción en App Service)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection(); // Se deshabilita por defecto en Azure App Service

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


// **********************************************
// 4. LÓGICA DE SEEDING DE DATOS (CRÍTICA)
// **********************************************
// 🚨 CORRECCIÓN CRÍTICA: Se agrega un bloque try-catch para evitar que la aplicación
// falle al iniciar si hay un problema de conexión o seeding.
using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    var context = serviceProvider.GetRequiredService<InventoryContext>();
    // Creamos un logger para registrar el error si la operación falla
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>(); 
    var superadminEmail = builder.Configuration["Superadmin:Email"]!;

    try 
    {
        // ⚠️ La aplicación se cae si esta verificación falla (ej: No hay conexión a la DB)
        if (!context.Users.Any(u => u.Role == "Superadmin"))
        {
            logger.LogInformation($"Iniciando Seeding: Creando Superadmin '{superadminEmail}'...");
            
            // Obtener credenciales de appsettings.json
            var superadminPassword = builder.Configuration["Superadmin:Password"]!;

            var adminId = Guid.NewGuid();
            var warehouseId = Guid.NewGuid();

            // 1. Crear el Superadmin
            var superadmin = new User
            {
                Id = adminId,
                Email = superadminEmail,
                PasswordHash = PasswordHelper.Hash(superadminPassword),
                Role = "Superadmin",
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
                Id = warehouseId,
                Name = "Bodega Central Admin",
                UserId = adminId,
                CreatedAt = DateTime.UtcNow
            };
            context.Warehouses.Add(warehouse);
            context.SaveChanges();

            // 3. Asignar la bodega al Superadmin
            superadmin.DefaultWarehouseId = warehouseId;
            context.Users.Update(superadmin);
            context.SaveChanges();
            
            logger.LogInformation($"✅ Superadmin '{superadminEmail}' creado con éxito y Bodega inicial asignada.");
        } else {
             logger.LogInformation($"✅ Superadmin '{superadminEmail}' ya existe. Se omite el seeding.");
        }
    }
    catch (Exception ex)
    {
        // ⚠️ Si la conexión o el seeding falla, registramos el error pero NO hacemos throw, 
        // permitiendo que el host continúe su ejecución. Esto es lo que soluciona el 500.30.
        logger.LogError(ex, "⚠️ FATAL: Ocurrió un error en el Seeding de datos. Revise la cadena de conexión o firewall de la DB.");
    }
}


app.Run();