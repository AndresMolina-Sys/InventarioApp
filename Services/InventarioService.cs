using Microsoft.EntityFrameworkCore;
using InventarioAppDesktop.Data;
using InventarioAppDesktop.Models;

namespace InventarioAppDesktop.Services;

public class InventarioService
{
    // ============================================================
    // Política de contraseñas (centralizada, aplicada en el servicio)
    // ============================================================
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 64;
    private const int BcryptWorkFactor = 12;
    private const int MaxLoginFailures = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromSeconds(30);

    // Keyspaces de caché (invalidación por operaciones CUD).
    private const string KsArticulos = "articulos";
    private const string KsCategorias = "categorias";
    private const string KsMetricas = "metricas";
    private const string KsUsuarios = "usuarios";

    private readonly Dictionary<string, (int Intentos, DateTime BloqueadoHasta)> _intentosFallidos = new();
    private readonly ICacheService _cache;

    // ============================================================
    // TTLs de caché. Demo local: se usan siempre los TTLs largos
    // (menos I/O contra el SQLite local).
    // ============================================================
    private const bool EsModoServidor = false;

    private static TimeSpan TtlCategorias() => EsModoServidor ? TimeSpan.FromSeconds(20) : TimeSpan.FromMinutes(2);
    private static TimeSpan TtlMetricas() => EsModoServidor ? TimeSpan.FromSeconds(60) : TimeSpan.FromSeconds(60);
    private static TimeSpan TtlDistribucion() => EsModoServidor ? TimeSpan.FromSeconds(60) : TimeSpan.FromMinutes(3);

    private readonly ArticulosService _articulos;

    public InventarioService(ICacheService? cache = null)
    {
        _cache = cache ?? MemoryCacheService.Default;
        _articulos = new ArticulosService(_cache);
    }

    private InventarioDbContext CreateDb() => new();

    // Invalida toda la caché. Lo usa el botón "Refrescar" de las páginas para
    // forzar una lectura fresca desde la base (cambios hechos en otras PCs).
    public static void InvalidarTodaLaCache()
    {
        MemoryCacheService.Default.Invalidate(KsArticulos);
        MemoryCacheService.Default.Invalidate(KsCategorias);
        MemoryCacheService.Default.Invalidate(KsMetricas);
        MemoryCacheService.Default.Invalidate(KsUsuarios);
    }

    public static string? ValidarPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            return "La contraseña es obligatoria.";
        if (password.Length < PasswordMinLength)
            return $"La contraseña debe tener al menos {PasswordMinLength} caracteres.";
        if (password.Length > PasswordMaxLength)
            return $"La contraseña no puede superar los {PasswordMaxLength} caracteres.";
        // BCrypt trunca silenciosamente a 72 bytes; rechazar contraseñas que excedan ese límite.
        if (System.Text.Encoding.UTF8.GetByteCount(password) > 72)
            return "La contraseña es demasiado larga.";
        return null;
    }

    // ============================================================
    // AUTENTICACIÓN (con rate limiting y mensaje genérico)
    // ============================================================
    public async Task<Usuario?> LoginAsync(string usuario, string password)
    {
        var clave = usuario.Trim().ToLowerInvariant();

        lock (_intentosFallidos)
        {
            if (_intentosFallidos.TryGetValue(clave, out var estado))
            {
                if (estado.BloqueadoHasta > DateTime.UtcNow)
                    return null;
                if (DateTime.UtcNow > estado.BloqueadoHasta)
                    _intentosFallidos.Remove(clave);
            }
        }

        using var db = CreateDb();
        var user = await db.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuario == usuario.Trim());

        // BCrypt es costoso por diseño (~250 ms con work factor 12). Se ejecuta en
        // el thread pool para no bloquear el hilo de UI ni el request de login.
        // Cuando el usuario no existe se verifica contra un hash dummy para que el
        // tiempo de respuesta no revele si la cuenta existe (anti-timing).
        var hashUsuario = user?.PasswordHash ?? HashDummy;
        var valido = await Task.Run(() => BCrypt.Net.BCrypt.Verify(password, hashUsuario));
        if (user == null || !valido)
        {
            RegistrarFallo(clave);
            return null;
        }

        lock (_intentosFallidos)
            _intentosFallidos.Remove(clave);

        user.PasswordHash = string.Empty; // No retener el hash en memoria (sesión).

        return user;
    }

    // Hash de costo equivalente usado cuando el usuario no existe, para igualar
    // el tiempo de verificación y no filtrar qué cuentas existen.
    private static readonly string HashDummy =
        BCrypt.Net.BCrypt.HashPassword("inventario-timing-dummy", BcryptWorkFactor);

    public bool EstaBloqueado(string usuario)
    {
        var clave = usuario.Trim().ToLowerInvariant();
        lock (_intentosFallidos)
        {
            return _intentosFallidos.TryGetValue(clave, out var estado)
                   && estado.BloqueadoHasta > DateTime.UtcNow;
        }
    }

    public static bool IntentoDemasiadoReciente()
        => false; // Mantiene firma simple; el lockout real vive en EstaBloqueado.

    private void RegistrarFallo(string clave)
    {
        lock (_intentosFallidos)
        {
            if (!_intentosFallidos.TryGetValue(clave, out var estado))
                estado = (0, DateTime.MinValue);

            estado.Intentos++;
            if (estado.Intentos >= MaxLoginFailures)
            {
                estado.BloqueadoHasta = DateTime.UtcNow.Add(LockoutDuration);
                estado.Intentos = 0;
            }
            _intentosFallidos[clave] = estado;
        }
    }

    // ============================================================
    // CATEGORÍAS (lecturas cacheadas, invalidadas en cada CUD)
    // ============================================================
    public Task<List<Categoria>> GetCategoriasAsync()
        => _cache.GetOrCreateAsync(KsCategorias, "todas", async () =>
        {
            using var db = CreateDb();
            // Proyección ligera: no se carga la colección Articulos (evita el
            // N+1 / lectura masiva). El contador viene de un COUNT en SQL.
            var filas = await db.Categorias
                .Select(c => new { c.Id, c.Nombre, Cantidad = c.Articulos.Count })
                .OrderBy(x => x.Nombre)
                .ToListAsync();
            return filas
                .Select(x => new Categoria { Id = x.Id, Nombre = x.Nombre, CantidadArticulos = x.Cantidad })
                .ToList();
        }, TtlCategorias());

    public Task<Categoria?> GetCategoriaByIdAsync(int id)
        => _cache.GetOrCreateAsync(KsCategorias, $"id:{id}", async () =>
        {
            using var db = CreateDb();
            var cat = await db.Categorias.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (cat == null) return null;
            cat.CantidadArticulos = await db.Articulos.CountAsync(a => a.CategoriaId == id);
            return cat;
        }, TtlCategorias());

    public async Task<bool> CreateCategoriaAsync(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 100)
            return false;

        using var db = CreateDb();
        if (await db.Categorias.AnyAsync(c => c.Nombre == nombre.Trim()))
            return false;
        db.Categorias.Add(new Categoria { Nombre = nombre.Trim() });
        await db.SaveChangesAsync();
        InvalidarCatalogosCategoria();
        return true;
    }

    public async Task<bool> UpdateCategoriaAsync(int id, string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 100)
            return false;

        using var db = CreateDb();
        var cat = await db.Categorias.FindAsync(id);
        if (cat == null) return false;
        if (await db.Categorias.AnyAsync(c => c.Nombre == nombre.Trim() && c.Id != id))
            return false;
        cat.Nombre = nombre.Trim();
        await db.SaveChangesAsync();
        InvalidarCatalogosCategoria();
        return true;
    }

    public async Task<bool> DeleteCategoriaAsync(int id)
    {
        using var db = CreateDb();
        var cat = await db.Categorias.FindAsync(id);
        if (cat == null) return false;
        if (await db.Articulos.AnyAsync(a => a.CategoriaId == id)) return false;
        db.Categorias.Remove(cat);
        await db.SaveChangesAsync();
        InvalidarCatalogosCategoria();
        return true;
    }

    private void InvalidarCatalogosCategoria()
    {
        _cache.Invalidate(KsCategorias);
        _cache.Invalidate(KsArticulos);
        _cache.Invalidate(KsMetricas);
        RecalentarCacheEnSegundoPlano();
    }

    // ============================================================
    // USUARIOS (authz en capa de servicios + invariante último admin)
    // ============================================================
    public Task<List<Usuario>> GetUsuariosAsync()
    {
        RequerirAdmin();
        return _cache.GetOrCreateAsync(KsUsuarios, "todos", async () =>
        {
            using var db = CreateDb();
            var usuarios = await db.Usuarios.AsNoTracking().OrderBy(u => u.NombreUsuario).ToListAsync();
            foreach (var u in usuarios)
                u.PasswordHash = string.Empty; // Nunca exponer hashes al front.
            return usuarios;
        }, TimeSpan.FromSeconds(60));
    }

    public async Task<bool> CreateUsuarioAsync(string nombre, string password, string rol)
    {
        RequerirAdmin();

        var error = ValidarPassword(password);
        if (error != null) return false;
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 50) return false;
        if (rol != "admin" && rol != "usuario") return false;

        using var db = CreateDb();
        if (await db.Usuarios.AnyAsync(u => u.NombreUsuario == nombre.Trim()))
            return false;
        var hash = await Task.Run(() => BCrypt.Net.BCrypt.HashPassword(password, BcryptWorkFactor));
        db.Usuarios.Add(new Usuario
        {
            NombreUsuario = nombre.Trim(),
            PasswordHash = hash,
            Rol = rol
        });
        await db.SaveChangesAsync();
        _cache.Invalidate(KsUsuarios);
        return true;
    }

    public async Task<bool> UpdateUsuarioAsync(int id, string nombre, string? password, string rol)
    {
        RequerirAdmin();
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 50) return false;
        if (rol != "admin" && rol != "usuario") return false;
        if (!string.IsNullOrWhiteSpace(password))
        {
            var error = ValidarPassword(password);
            if (error != null) return false;
        }

        using var db = CreateDb();
        var user = await db.Usuarios.FindAsync(id);
        if (user == null) return false;
        if (await db.Usuarios.AnyAsync(u => u.NombreUsuario == nombre.Trim() && u.Id != id))
            return false;

        // Invariante último admin: no se puede degradar al último admin.
        if (user.Rol == "admin" && rol != "admin" && await EsUltimoAdminAsync(db, id))
            return false;

        user.NombreUsuario = nombre.Trim();
        user.Rol = rol;
        if (!string.IsNullOrWhiteSpace(password))
            user.PasswordHash = await Task.Run(() => BCrypt.Net.BCrypt.HashPassword(password, BcryptWorkFactor));
        await db.SaveChangesAsync();
        _cache.Invalidate(KsUsuarios);
        return true;
    }

    // Un usuario solo puede cambiar su propio nombre desde su perfil.
    public async Task<bool> UpdatePropioNombreAsync(int id, string nombre)
    {
        if (Session.UsuarioActual?.Id != id)
            return false;
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 50)
            return false;

        using var db = CreateDb();
        var user = await db.Usuarios.FindAsync(id);
        if (user == null) return false;
        if (await db.Usuarios.AnyAsync(u => u.NombreUsuario == nombre.Trim() && u.Id != id))
            return false;

        user.NombreUsuario = nombre.Trim();
        await db.SaveChangesAsync();
        _cache.Invalidate(KsUsuarios);
        return true;
    }

    public async Task<bool> DeleteUsuarioAsync(int id)
    {
        RequerirAdmin();

        using var db = CreateDb();
        var user = await db.Usuarios.FindAsync(id);
        if (user == null) return false;

        // Invariante último admin: no se puede eliminar al último admin.
        if (user.Rol == "admin" && await EsUltimoAdminAsync(db, id))
            return false;

        db.Usuarios.Remove(user);
        await db.SaveChangesAsync();
        _cache.Invalidate(KsUsuarios);
        return true;
    }

    private static async Task<bool> EsUltimoAdminAsync(InventarioDbContext db, int idExcluido)
        => await db.Usuarios.CountAsync(u => u.Rol == "admin" && u.Id != idExcluido) == 0;

    private static void RequerirAdmin()
    {
        if (!Session.IsAdmin)
            throw new UnauthorizedAccessException("No tiene permisos para administrar usuarios.");
    }

    // ============================================================
    // CONTRASEÑAS (BCrypt en thread pool, no en el hilo de UI)
    // ============================================================
    public async Task<bool> UpdatePasswordAsync(int id, string currentPassword, string newPassword)
    {
        var error = ValidarPassword(newPassword);
        if (error != null) return false;

        using var db = CreateDb();
        var user = await db.Usuarios.FindAsync(id);
        if (user == null) return false;

        var hashCorrecto = await Task.Run(() => BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash));
        if (!hashCorrecto) return false;
        var esLaMisma = await Task.Run(() => BCrypt.Net.BCrypt.Verify(newPassword, user.PasswordHash));
        if (esLaMisma) return false; // No repetir la contraseña actual.

        user.PasswordHash = await Task.Run(() => BCrypt.Net.BCrypt.HashPassword(newPassword, BcryptWorkFactor));
        user.DebeCambiarPassword = false;
        await db.SaveChangesAsync();
        _cache.Invalidate(KsUsuarios);
        return true;
    }

    public async Task<bool> ForcePasswordChangeAsync(int id, string newPassword)
    {
        var error = ValidarPassword(newPassword);
        if (error != null) return false;

        using var db = CreateDb();
        var user = await db.Usuarios.FindAsync(id);
        if (user == null) return false;

        // Solo se permite forzar el cambio si la cuenta exige cambio pendiente
        // y el llamador es el propio usuario (o un admin).
        if (!user.DebeCambiarPassword && !Session.IsAdmin)
            return false;
        if (!Session.IsAdmin && Session.UsuarioActual?.Id != id)
            return false;

        var esLaMisma = await Task.Run(() => BCrypt.Net.BCrypt.Verify(newPassword, user.PasswordHash));
        if (esLaMisma) return false; // No usar la misma contraseña.

        user.PasswordHash = await Task.Run(() => BCrypt.Net.BCrypt.HashPassword(newPassword, BcryptWorkFactor));
        user.DebeCambiarPassword = false;
        await db.SaveChangesAsync();
        _cache.Invalidate(KsUsuarios);
        return true;
    }

    // ============================================================
    // MÉTRICAS (agregaciones cacheadas)
    // ============================================================
    public Task<int> GetTotalCategoriasAsync()
        => _cache.GetOrCreateAsync(KsMetricas, "total-categorias", async () =>
        {
            using var db = CreateDb();
            return await db.Categorias.CountAsync();
        }, TtlMetricas());

    public Task<int> GetTotalUsuariosAsync()
    {
        RequerirAdmin();
        return _cache.GetOrCreateAsync(KsUsuarios, "total", async () =>
        {
            using var db = CreateDb();
            return await db.Usuarios.CountAsync();
        }, TtlMetricas());
    }

    public Task<List<CategoriaConCantidad>> GetCategoriasConCantidadAsync()
        => _cache.GetOrCreateAsync(KsMetricas, "distribucion", async () =>
        {
            using var db = CreateDb();
            return await db.Categorias
                .Where(c => c.Articulos.Any())
                .Select(c => new CategoriaConCantidad
                {
                    Nombre = c.Nombre,
                    Cantidad = c.Articulos.Count
                })
                .OrderByDescending(c => c.Cantidad)
                .ToListAsync();
        }, TtlDistribucion());

    // ============================================================
    // BACKGROUND: recalentamiento de caché tras escrituras.
    // La invalidación deja frío el cache; este trabajo re-materializa en
    // segundo plano los datos más consultados (métricas y dashboard) para
    // evitar el thundering herd cuando la UI vuelve a leer.
    // ============================================================
    private void RecalentarCacheEnSegundoPlano()
    {
        BackgroundJobQueue.Default.TryEnqueue(async ct =>
        {
            // Cada refresco es independiente: un fallo aislado no aborta el resto.
            await RefrescarMetricaAsync(() => _articulos.GetTotalArticulosAsync());
            await RefrescarMetricaAsync(() => GetTotalCategoriasAsync());
            await RefrescarMetricaAsync(() => GetCategoriasConCantidadAsync());
            await RefrescarMetricaAsync(() => _articulos.GetRecentArticulosAsync());
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
}

public class CategoriaConCantidad
{
    public string Nombre { get; set; } = string.Empty;
    public int Cantidad { get; set; }
}
