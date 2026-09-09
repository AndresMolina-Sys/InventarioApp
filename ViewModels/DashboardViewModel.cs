using System.Collections.ObjectModel;
using InventarioAppDesktop.Models;
using InventarioAppDesktop.Services;

namespace InventarioAppDesktop.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    private readonly InventarioService _service = new();
    private readonly IArticulosService _articulos = new ArticulosService();

    private int _totalArticulos;
    public int TotalArticulos
    {
        get => _totalArticulos;
        set => SetProperty(ref _totalArticulos, value);
    }

    private int _totalCategorias;
    public int TotalCategorias
    {
        get => _totalCategorias;
        set => SetProperty(ref _totalCategorias, value);
    }

    private int _totalUsuarios;
    public int TotalUsuarios
    {
        get => _totalUsuarios;
        set => SetProperty(ref _totalUsuarios, value);
    }

    public ObservableCollection<Articulo> ArticulosRecientes { get; } = new();
    public ObservableCollection<CategoriaPorcentaje> Distribucion { get; } = new();

    public async Task LoadAsync()
    {
        TotalArticulos = await _articulos.GetTotalArticulosAsync();
        TotalCategorias = await _service.GetTotalCategoriasAsync();
        TotalUsuarios = Data.Session.IsAdmin ? await _service.GetTotalUsuariosAsync() : 0;

        var recientes = await _articulos.GetRecentArticulosAsync();
        ArticulosRecientes.Clear();
        foreach (var a in recientes)
            ArticulosRecientes.Add(a);

        var cats = await _service.GetCategoriasConCantidadAsync();
        var total = cats.Sum(c => c.Cantidad);
        Distribucion.Clear();
        foreach (var c in cats)
        {
            var porcentaje = total > 0 ? (double)c.Cantidad / total * 100 : 0;
            Distribucion.Add(new CategoriaPorcentaje { Nombre = c.Nombre, Cantidad = c.Cantidad, Porcentaje = porcentaje, PorcentajeInv = 100 - porcentaje });
        }
    }
}

public class CategoriaPorcentaje : ViewModelBase
{
    private string _nombre = string.Empty;
    public string Nombre
    {
        get => _nombre;
        set => SetProperty(ref _nombre, value);
    }

    private int _cantidad;
    public int Cantidad
    {
        get => _cantidad;
        set => SetProperty(ref _cantidad, value);
    }

    private double _porcentaje;
    public double Porcentaje
    {
        get => _porcentaje;
        set => SetProperty(ref _porcentaje, value);
    }

    private double _porcentajeInv;
    public double PorcentajeInv
    {
        get => _porcentajeInv;
        set => SetProperty(ref _porcentajeInv, value);
    }
}
