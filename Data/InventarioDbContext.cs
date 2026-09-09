using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using InventarioAppDesktop.Models;
using InventarioAppDesktop.Services;

namespace InventarioAppDesktop.Data;

public class InventarioDbContext : DbContext
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Articulo> Articulos => Set<Articulo>();
    public DbSet<Categoria> Categorias => Set<Categoria>();

    public InventarioDbContext() { }

    public InventarioDbContext(DbContextOptions<InventarioDbContext> options)
        : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
            return;

        // Demo local: únicamente SQLite, sin modos de conexión.
        var dbPath = AppConfigService.RutaBaseDatosLocal();
        optionsBuilder.UseSqlite(CrearConexionSqlite(dbPath));
    }

    // Conexión SQLite con WAL + busy_timeout: permite lecturas concurrentes
    // con escrituras y evita errores "database is locked" bajo carga.
    private static SqliteConnection CrearConexionSqlite(string dbPath)
    {
        var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.StateChange += (_, e) =>
        {
            if (e.CurrentState == ConnectionState.Open)
            {
                using var command = connection.CreateCommand();
                command.CommandText =
                    "PRAGMA journal_mode=WAL;" +
                    "PRAGMA busy_timeout=5000;" +
                    "PRAGMA synchronous=NORMAL;";
                command.ExecuteNonQuery();
            }
        };
        return connection;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.NombreUsuario)
            .IsUnique();

        modelBuilder.Entity<Articulo>()
            .HasOne(a => a.Categoria)
            .WithMany(c => c.Articulos)
            .HasForeignKey(a => a.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);

        // ============================================================
        // Meta indexing: índices para las consultas de mayor frecuencia.
        // - (CategoriaId, Nombre): filtro por categoría + ORDER BY Nombre
        //   (cubre además la FK, siendo índice de cobertura para la búsqueda
        //   por categoría).
        // - FechaModificacion: ORDER BY DESC del panel "recientes".
        // - Codigo: búsqueda exacta por código de artículo.
        // ============================================================
        modelBuilder.Entity<Articulo>()
            .HasIndex(a => new { a.CategoriaId, a.Nombre });

        modelBuilder.Entity<Articulo>()
            .HasIndex(a => a.FechaModificacion);

        modelBuilder.Entity<Articulo>()
            .HasIndex(a => a.Codigo);

        modelBuilder.Entity<Categoria>()
            .HasIndex(c => c.Nombre);
    }
}
