using Microsoft.EntityFrameworkCore;
using BodegApp.Backend.Models;

namespace BodegApp.Backend.Data
{
    public class InventoryContext : DbContext
    {
        public InventoryContext(DbContextOptions<InventoryContext> options)
            : base(options) { }

        // 🔹 Tablas principales
        public DbSet<User> Users => Set<User>();
        public DbSet<ItemBatch> ItemBatches => Set<ItemBatch>();
        public DbSet<StockMovement> StockMovements => Set<StockMovement>();
        public DbSet<Warehouse> Warehouses => Set<Warehouse>();

        // 🔹 Configuración de relaciones
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 🔸 Relación: User → Warehouses
            modelBuilder.Entity<Warehouse>()
                .HasOne(w => w.User)
                .WithMany(u => u.Warehouses)
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // 🔸 Relación: Warehouse → ItemBatches
            modelBuilder.Entity<ItemBatch>()
                .HasOne(b => b.Warehouse)
                .WithMany(w => w.ItemBatches)
                .HasForeignKey(b => b.WarehouseId)
                .OnDelete(DeleteBehavior.Cascade);

            // 🔸 Relación: User → ItemBatches (trazabilidad directa)
            modelBuilder.Entity<ItemBatch>()
                .HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // 🔸 Relación: StockMovement → ItemBatch
            modelBuilder.Entity<StockMovement>()
                .HasOne(sm => sm.Batch)
                .WithMany()
                .HasForeignKey(sm => sm.BatchId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
