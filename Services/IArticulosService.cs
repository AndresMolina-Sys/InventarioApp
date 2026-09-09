using InventarioAppDesktop.Models;

namespace InventarioAppDesktop.Services;

// Contrato de operaciones de artículos. Lo implementa ArticulosService y lo
// consumen los ViewModels y vistas de la página de artículos.
public interface IArticulosService
{
    Task<List<Articulo>> GetArticulosByCategoriaAsync(int categoriaId);
    Task<List<Articulo>> GetAllArticulosAsync();
    Task<List<Articulo>> GetRecentArticulosAsync();
    Task<Articulo?> GetArticuloByIdAsync(int id);
    Task<List<Articulo>> GetArticulosPorCodigoAsync(string codigo);
    Task<(int Total, List<Articulo> Items)> BuscarArticulosAsync(
        int? categoriaId, string? filtro, int pagina, int tamanoPagina);
    Task<bool> CreateArticuloAsync(Articulo articulo);
    Task<bool> UpdateArticuloAsync(Articulo articulo);
    Task<bool> DeleteArticuloAsync(int id);
    Task<int> GetTotalArticulosAsync();
}
