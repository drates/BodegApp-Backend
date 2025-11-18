# 📦 BodegApp Backend API

API RESTful para la gestión de inventario multi-bodega, desarrollada con .NET 8 y PostgreSQL, enfocada en la seguridad por cliente/bodega.

## ⚙️ Stack Tecnológico Principal

* **Framework:** C# / .NET 8 (ASP.NET Core)
* **Base de Datos:** PostgreSQL
* **ORM:** Entity Framework Core (EF Core)
* **Autenticación:** JWT (JSON Web Tokens)
* **Contraseñas:** BCrypt.Net para hashing seguro.
* **Servicios:** JwtService, PasswordHelper.

## 🛡️ Arquitectura y Seguridad

La seguridad se implementa bajo un esquema de **Multi-tenancy implícito** donde cada usuario solo puede acceder a los datos vinculados a su bodega por defecto (`DefaultWarehouseId`).

### Mecanismo de Aislamiento de Datos

1.  **Login:** Al iniciar sesión, el servicio `JwtService.cs` genera un Token JWT que incluye un *Claim* llamado **`WarehouseId`**, que contiene el GUID de la bodega principal del usuario.
2.  **Controladores:** En todos los Controladores de inventario (`ItemBatchController`, `IngresoController`, `EgresoController`), se extrae este `WarehouseId` del token.
3.  **Consultas a DB:** Todas las consultas a la tabla `ItemBatch` (lotes) utilizan una cláusula `WHERE WarehouseId == warehouseId` para garantizar que el usuario **solo** ve y modifica el stock de su propia bodega.

### Modelos Clave

| Archivo | Rol | Relaciones Clíticas |
| :--- | :--- | :--- |
| **User.cs** | Gestiona usuarios y su rol (`User`, `Admin`, `Superadmin`). | Contiene `DefaultWarehouseId` (GUID) para aislamiento de datos. |
| **Warehouse.cs** | Entidad de Bodega. | Un `User` puede tener muchas `Warehouses`. |
| **ItemBatch.cs** | Lote de productos (la unidad de inventario). | Vinculado a `WarehouseId`. |
| **StockMovement.cs** | Historial de ingresos y egresos. | Trazabilidad por `UserId` y `BatchId`. |

## 🚀 Endpoints Principales

Todos los endpoints usan la ruta base `/api/[Controller]`.

| Ruta | Método | Descripción | Autorización |
| :--- | :--- | :--- | :--- |
| `/api/auth/register` | `POST` | Crea una nueva cuenta, un `User`, y una `Warehouse` por defecto. | Anónimo |
| `/api/auth/login` | `POST` | Inicia sesión, devuelve el token JWT con el **`WarehouseId`** y el `Role`. | Anónimo |
| `/api/auth/me` | `GET` | Valida el token y devuelve los datos del usuario (incluyendo `WarehouseId`). | Autorizado |
| `/api/itembatch` | `GET` | Obtiene todos los lotes (`ItemBatch`) activos en **la bodega del usuario**. | Autorizado |
| `/api/ingreso` | `POST` | Registra la entrada de stock (crea o suma a un lote existente). | Autorizado |
| `/api/egreso` | `POST` | Registra la salida de stock (resta a un lote existente). | Autorizado |
| `/api/superadmin/metricas` | `GET` | Métricas y resumen global del sistema. | `Superadmin` |

## 🛠️ Configuración y Ejecución Local

1.  **Requisitos:** Instalar .NET 8 SDK y tener una instancia de PostgreSQL corriendo (ej: Docker o instalación local).
2.  **Configurar DB:** Ajusta la cadena de conexión en `appsettings.Development.json`:
    ```json
    "ConnectionStrings": {
      "PostgresConnection": "Host=localhost;Port=5432;Database=bodegapp_db;Username=postgres;Password=MiClaveSecreta123"
    }
    ```
3.  **Aplicar Migraciones:** Aplica las migraciones de Entity Framework Core para crear la base de datos y sus tablas.
    ```bash
    dotnet ef database update
    # La lógica en Program.cs creará el Superadmin inicial si la DB está vacía.
    ```
4.  **Ejecutar la API:**
    ```bash
    dotnet run
    ```
    La API estará disponible en la URL configurada (usualmente `http://localhost:5000` o similar).

---