using System.Collections.ObjectModel;
using System.Windows.Input;
using InventarioAppDesktop.Models;
using InventarioAppDesktop.Services;

namespace InventarioAppDesktop.ViewModels;

public class ArticulosViewModel : ViewModelBase
{
    private readonly IArticulosService _service = new ArticulosService();

    private string _filtro = string.Empty;
    public string Filtro
    {
        get => _filtro;
        set
        {
            if (SetProperty(ref _filtro, value))
                ProgramarBusquedaConDebounce();
        }
    }

    public ObservableCollection<Articulo> Articulos { get; } = new();

    private const int ArticulosPorPagina = 25;
    private int _paginaActual = 1;
    public int PaginaActual
    {
        get => _paginaActual;
        set
        {
            if (SetProperty(ref _paginaActual, value))
                _ = CargarPaginaAsync(value);
        }
    }

    // Paginación en el servidor: el total llega de la consulta SQL (COUNT),
    // no de materializar todo el catálogo en memoria.
    private int _totalRegistros;
    public int TotalRegistros
    {
        get => _totalRegistros;
        private set => SetProperty(ref _totalRegistros, value);
    }

    public int TotalPaginas => Math.Max(1, (int)Math.Ceiling((double)TotalRegistros / ArticulosPorPagina));

    public string TextoPagina => $"Página {PaginaActual} de {TotalPaginas}";

    public bool PuedeRetroceder => PaginaActual > 1;
    public bool PuedeAvanzar => PaginaActual < TotalPaginas;

    public ICommand PaginaAnteriorCommand { get; }
    public ICommand PaginaSiguienteCommand { get; }

    private Articulo? _fichaArticulo;
    public Articulo? FichaArticulo
    {
        get => _fichaArticulo;
        set => SetProperty(ref _fichaArticulo, value);
    }

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }

    public event Action<string>? ShowMessage;
    public event Func<string, string, bool>? ConfirmDelete;

    // Estado interno de la búsqueda (serializa las recargas para evitar
    // resultados fuera de orden cuando el usuario escribe rápido).
    private int? _categoriaIdActiva;
    private CancellationTokenSource? _ctsBusqueda;
    private readonly SemaphoreSlim _cargaLock = new(1, 1);

    public ArticulosViewModel()
    {
        AddCommand = new RelayCommand(_ => DoAdd());
        EditCommand = new RelayCommand(art => DoEdit((Articulo)art!));
        DeleteCommand = new RelayCommand(art => DoDelete((Articulo)art!));
        PaginaAnteriorCommand = new RelayCommand(_ => { if (PuedeRetroceder) PaginaActual--; });
        PaginaSiguienteCommand = new RelayCommand(_ => { if (PuedeAvanzar) PaginaActual++; });
    }

    public async Task LoadAsync()
    {
        _categoriaIdActiva = null;
        _paginaActual = 1;
        await CargarPaginaAsync(1);
    }

    public async Task LoadArticulosAsync(int categoriaId)
    {
        _categoriaIdActiva = categoriaId;
        _paginaActual = 1;
        await CargarPaginaAsync(1);
    }

    private void ProgramarBusquedaConDebounce()
    {
        _ctsBusqueda?.Cancel();
        var cts = new CancellationTokenSource();
        _ctsBusqueda = cts;
        _ = BuscarConDebounceAsync(cts);
    }

    private async Task BuscarConDebounceAsync(CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(300, cts.Token);
        }
        catch (TaskCanceledException)
        {
            return;
        }
        if (cts.IsCancellationRequested) return;
        await CargarPaginaAsync(1);
    }

    private async Task CargarPaginaAsync(int pagina)
    {
        await _cargaLock.WaitAsync();
        try
        {
            var filtro = string.IsNullOrWhiteSpace(Filtro) ? null : Filtro;
            var (total, items) = await _service.BuscarArticulosAsync(
                _categoriaIdActiva, filtro, pagina, ArticulosPorPagina);

            Articulos.Clear();
            foreach (var a in items)
                Articulos.Add(a);

            TotalRegistros = total;
            _paginaActual = pagina;
            OnPropertyChanged(nameof(TotalPaginas));
            OnPropertyChanged(nameof(TextoPagina));
            OnPropertyChanged(nameof(PuedeRetroceder));
            OnPropertyChanged(nameof(PuedeAvanzar));
        }
        finally
        {
            _cargaLock.Release();
        }
    }

    private async Task RecargarActualAsync()
    {
        if (_categoriaIdActiva.HasValue)
            await LoadArticulosAsync(_categoriaIdActiva.Value);
        else
            await CargarPaginaAsync(Math.Max(1, PaginaActual));
    }

    private void DoAdd()
    {
        var dialog = new Views.ArticuloDialog(_categoriaIdActiva ?? 0);
        if (dialog.ShowDialog() == true)
            _ = RecargarActualAsync();
    }

    private void DoEdit(Articulo art)
    {
        var dialog = new Views.ArticuloDialog(art.CategoriaId, art);
        if (dialog.ShowDialog() == true)
            _ = RecargarActualAsync();
    }

    private async void DoDelete(Articulo art)
    {
        if (ConfirmDelete?.Invoke("artículo", art.Nombre) == true)
        {
            await _service.DeleteArticuloAsync(art.Id);
            ShowMessage?.Invoke("Artículo eliminado correctamente.");
            await RecargarActualAsync();
        }
    }
}
