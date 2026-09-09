using Microsoft.EntityFrameworkCore;
using InventarioAppDesktop.Data;
using InventarioAppDesktop.Models;

namespace InventarioAppDesktop.Services;

// Implementación del contrato de artículos. Antes vivía dentro del monolito
// InventarioService; se extrajo para separar responsabilidades por dominio.
public class ArticulosService : IArticulosService
{
    private const int ArticulosRecientesMax = 500;

    // Keyspaces de caché (invalidación por operaciones CUD).
    private const string KsArticulos = "articulos";
    private const string KsCategorias = "categorias";
    private const string KsMetricas = "metricas";

    private readonly ICacheService _cache;

    // Demo local: sin modo servidor; se usan siempre los TTLs locales.
    private const bool EsModoServidor = false;

    private static TimeSpan TtlArticulos() => EsModoServidor ? TimeSpan.FromSeconds(20) : TimeSpan.FromSeconds(60);
    private static TimeSpan TtlDetalle() => EsModoServidor ? TimeSpan.FromSeconds(60) : TimeSpan.FromMinutes(5);
    private static TimeSpan TtlMetricas() => EsModoServidor ? TimeSpan.FromSeconds(60) : TimeSpan.FromSeconds(60);

    public ArticulosService(ICacheService? cache = null)
        => _cache = cache ?? MemoryCacheService.Default;

    private InventarioDbContext CreateDb() => new();

    public Task<List<Articulo>> GetArticulosByCategoriaAsync(int categoriaId)
        => _cache.GetOrCreateAsync(KsArticulos, $"cat:{categoriaId}", async () =>
        {
            using var db = CreateDb();
            return await db.Articulos.AsNoTracking()
                .Where(a => a.CategoriaId == categoriaId)
                .Include(a => a.Categoria)
                .OrderBy(a => a.Nombre)
                .ToListAsync();
        }, TtlArticulos());

    public Task<List<Articulo>> GetAllArticulosAsync()
        => _cache.GetOrCreateAsync(KsArticulos, "todas", async () =>
        {
            using var db = CreateDb();
            return await db.Articulos.AsNoTracking()
                .Include(a => a.Categoria)
                .OrderBy(a => a.Nombre)
                .ToListAsync();
        }, TtlArticulos());

    public Task<List<Articulo>> GetRecentArticulosAsync()
        => _cache.GetOrCreateAsync(KsArticulos, "recientes", async () =>
        {
            using var db = CreateDb();
            // Tope duro: el dashboard pagina 5 a la vez; cargar 100k filas para
            // "recientes" era el cuello de botella principal del arranque.
            return await db.Articulos.AsNoTracking()
                .Include(a => a.Categoria)
                .OrderByDescending(a => a.FechaModificacion)
                .Take(ArticulosRecientesMax)
                .ToListAsync();
        }, TtlArticulos());

    public Task<Articulo?> GetArticuloByIdAsync(int id)
        => _cache.GetOrCreateAsync(KsArticulos, $"id:{id}", async () =>
        {
            using var db = CreateDb();
            return await db.Articulos.AsNoTracking()
                .Include(a => a.Categoria)
                .FirstOrDefaultAsync(a => a.Id == id);
        }, TtlDetalle());

    // Búsqueda exacta por código (aprovecha IX_Articulos_Codigo).
    public Task<List<Articulo>> GetArticulosPorCodigoAsync(string codigo)
    {
        var codigoNormalizado = codigo.Trim();
        return _cache.GetOrCreateAsync(KsArticulos, $"codigo:{codigoNormalizado}", async () =>
        {
            using var db = CreateDb();
            return await db.Articulos.AsNoTracking()
                .Include(a => a.Categoria)
                .Where(a => a.Codigo == codigoNormalizado)
                .ToListAsync();
        }, TtlDetalle());
    }

    // Paginación + filtro en el servidor: la UI ya no materializa el catálogo
    // completo en memoria; solo trae la página solicitada.
    public Task<(int Total, List<Articulo> Items)> BuscarArticulosAsync(
        int? categoriaId, string? filtro, int pagina, int tamanoPagina)
    {
        var filtroNormalizado = string.IsNullOrWhiteSpace(filtro) ? null : filtro.Trim();
        var paginaSegura = Math.Max(1, pagina);
        var tamanoSeguro = Math.Clamp(tamanoPagina, 1, 200);
        var key = $"{categoriaId?.ToString() ?? "-"}|{filtroNormalizado ?? "-"}|{paginaSegura}|{tamanoSeguro}";

        return _cache.GetOrCreateAsync(KsArticulos, $"busca:{key}", async () =>
        {
            using var db = CreateDb();
            IQueryable<Articulo> query = db.Articulos.AsNoTracking().Include(a => a.Categoria);

            if (categoriaId.HasValue)
                query = query.Where(a => a.CategoriaId == categoriaId.Value);

            if (filtroNormalizado != null)
            {
                var patron = $"%{EscapeLike(filtroNormalizado)}%";
                query = query.Where(a =>
                    EF.Functions.Like(a.Nombre, patron, "\\") ||
                    EF.Functions.Like(a.Codigo, patron, "\\") ||
                    EF.Functions.Like(a.Departamento, patron, "\\") ||
                    (a.Categoria != null && EF.Functions.Like(a.Categoria.Nombre, patron, "\\")));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(a => a.Nombre)
                .Skip((paginaSegura - 1) * tamanoSeguro)
                .Take(tamanoSeguro)
                .ToListAsync();
            return (total, items);
        }, TtlArticulos());
    }

    public async Task<bool> CreateArticuloAsync(Articulo articulo)
    {
        if (!ValidarArticulo(articulo))
            return false;

        using var db = CreateDb();
        if (!await db.Categorias.AnyAsync(c => c.Id == articulo.CategoriaId))
            return false;
        NormalizarOpcionales(articulo);
        articulo.FechaModificacion = DateTime.Now;
        articulo.FechaIngreso = DateTime.Now;
        db.Articulos.Add(articulo);
        await db.SaveChangesAsync();
        InvalidarCatalogosArticulo();
        return true;
    }

    public async Task<bool> UpdateArticuloAsync(Articulo articulo)
    {
        if (!ValidarArticulo(articulo))
            return false;

        using var db = CreateDb();
        var existing = await db.Articulos.FindAsync(articulo.Id);
        if (existing == null) return false;
        if (!await db.Categorias.AnyAsync(c => c.Id == articulo.CategoriaId))
            return false;
        existing.Codigo = articulo.Codigo.Trim();
        existing.Nombre = articulo.Nombre.Trim();
        existing.Marca = articulo.Marca?.Trim();
        existing.Modelo = articulo.Modelo?.Trim();
        existing.Sku = articulo.Sku?.Trim();
        existing.Departamento = articulo.Departamento.Trim();
        existing.Notas = articulo.Notas?.Trim();
        existing.Precio = articulo.Precio;
        existing.CategoriaId = articulo.CategoriaId;
        existing.FechaModificacion = DateTime.Now;
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // El artículo fue eliminado/actualizado por otro proceso entre el
            // FindAsync y el SaveChanges (carrera entre usuarios). Se reporta
            // como "no se pudo actualizar" en vez de propagar la excepción.
            return false;
        }
        InvalidarCatalogosArticulo();
        return true;
    }

    public async Task<bool> DeleteArticuloAsync(int id)
    {
        using var db = CreateDb();
        var art = await db.Articulos.FindAsync(id);
        if (art == null) return false;
        db.Articulos.Remove(art);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Carrera benigna: otro proceso eliminó el registro primero.
            return false;
        }
        InvalidarCatalogosArticulo();
        return true;
    }

    private void InvalidarCatalogosArticulo()
    {
        _cache.Invalidate(KsArticulos);
        _cache.Invalidate(KsMetricas);
        // Los conteos por categoría (GetCategoriasAsync) dependen de Articulos:
        // sin esto quedarían obsoletos hasta expirar su TTL.
        _cache.Invalidate(KsCategorias);
        RecalentarCacheEnSegundoPlano();
    }

    private static bool ValidarArticulo(Articulo a)
        => !string.IsNullOrWhiteSpace(a.Codigo) && a.Codigo.Trim().Length <= 50 &&
           !string.IsNullOrWhiteSpace(a.Nombre) && a.Nombre.Trim().Length <= 150 &&
           (a.Sku == null || a.Sku.Trim().Length <= 100) &&
           (a.Marca == null || a.Marca.Trim().Length <= 100) &&
           (a.Modelo == null || a.Modelo.Trim().Length <= 100) &&
           !string.IsNullOrWhiteSpace(a.Departamento) && a.Departamento.Trim().Length <= 100 &&
           (a.Notas == null || a.Notas.Trim().Length <= 500) &&
           (a.Precio == null || (a.Precio >= 0 && a.Precio <= 100000000m));

    // Los campos opcionales vacíos se guardan como NULL (no como cadenas vacías).
    private static void NormalizarOpcionales(Articulo a)
    {
        a.Sku = string.IsNullOrWhiteSpace(a.Sku) ? null : a.Sku.Trim();
        a.Marca = string.IsNullOrWhiteSpace(a.Marca) ? null : a.Marca.Trim();
        a.Modelo = string.IsNullOrWhiteSpace(a.Modelo) ? null : a.Modelo.Trim();
        a.Notas = string.IsNullOrWhiteSpace(a.Notas) ? null : a.Notas.Trim();
    }

    private static string EscapeLike(string texto)
        => texto.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    // ============================================================
    // BACKGROUND: recalentamiento de la métrica de artículos tras escrituras.
    // La invalidación deja frío el cache; este trabajo re-materializa en
    // segundo plano los datos de artículos (total y recientes) para evitar el
    // thundering herd cuando la UI vuelve a leer.
    // ============================================================
    private void RecalentarCacheEnSegundoPlano()
    {
        BackgroundJobQueue.Default.TryEnqueue(async ct =>
        {
            // Cada refresco es independiente: un fallo aislado no aborta el resto.
            await RefrescarMetricaAsync(() => GetTotalArticulosAsync());
            await RefrescarMetricaAsync(() => GetRecentArticulosAsync());
        });
    }

    private static async Task RefrescarMetricaAsync(Func<Task> leer)
    {
        try
        {
            await leer();
        }
        catch
        {
            BackgroundJobQueue.Default.RegistrarFallo();
        }
    }

    public Task<int> GetTotalArticulosAsync()
        => _cache.GetOrCreateAsync(KsMetricas, "total-articulos", async () =>
        {
            using var db = CreateDb();
            return await db.Articulos.CountAsync();
        }, TtlMetricas());
}
