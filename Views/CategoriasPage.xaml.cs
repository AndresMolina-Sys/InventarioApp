using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using InventarioAppDesktop.Data;
using InventarioAppDesktop.Models;
using InventarioAppDesktop.Services;

namespace InventarioAppDesktop.Views;

public partial class CategoriasPage : UserControl
{
    private readonly IArticulosService _articulosService = new ArticulosService();
    private ViewModels.CategoriasViewModel _vm => ((ViewModels.MainViewModel)DataContext).CategoriasVM;
    private ViewModels.ArticulosViewModel _articulosVM => ((ViewModels.MainViewModel)DataContext).ArticulosVM;
    private Categoria? _editingCategoria;
    private Categoria? _detallesCategoria;
    private string _activeDetSort = "";
    private bool _sortDetCodigoAsc = true;
    private bool _sortDetNombreAsc = true;
    private bool _sortDetSkuAsc = true;
    private bool _sortDetUbicacionAsc = true;
    private bool _sortDetFechaAsc = false;

    public CategoriasPage()
    {
        InitializeComponent();
        Loaded += CategoriasPage_Loaded;
    }

    private void CategoriasPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (!Session.IsAdmin)
            ColDetallesAcciones.Visibility = Visibility.Collapsed;
    }

    private void ShowPanel(StackPanel active)
    {
        PanelLista.Visibility = Visibility.Collapsed;
        PanelEditar.Visibility = Visibility.Collapsed;
        PanelNueva.Visibility = Visibility.Collapsed;
        PanelDetalles.Visibility = Visibility.Collapsed;
        PanelFichaArticulo.Visibility = Visibility.Collapsed;
        active.Visibility = Visibility.Visible;
    }

    private async void BtnVerDetalle_Click(object sender, RoutedEventArgs e)
    {
        var cat = (Categoria)((Button)sender).Tag;
        _detallesCategoria = cat;
        _activeDetSort = "";
        LblDetallesNombre.Text = cat.Nombre;
        await _articulosVM.LoadArticulosAsync(cat.Id);
        LblDetallesConteo.Text = $"({_articulosVM.TotalRegistros} artículos)";
        UpdateDetallesEmptyState();
        UpdateDetSortIndicators();
        ShowPanel(PanelDetalles);
    }

    private void BtnVolverDetalles_Click(object sender, RoutedEventArgs e)
    {
        _detallesCategoria = null;
        ShowPanel(PanelLista);
    }

    private void UpdateDetallesEmptyState()
    {
        LblNoDetallesArticulos.Visibility = _articulosVM.Articulos.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        GridDetallesArticulos.Visibility = _articulosVM.Articulos.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    private void UpdateDetSortIndicators()
    {
        UpdateDetSortLabel(LblSortDetCodigo, "codigo");
        UpdateDetSortLabel(LblSortDetNombre, "nombre");
        UpdateDetSortLabel(LblSortDetSku, "sku");
        UpdateDetSortLabel(LblSortDetUbicacion, "ubicacion");
        UpdateDetSortLabel(LblSortDetFecha, "fecha");
    }

    private void UpdateDetSortLabel(TextBlock? tb, string column)
    {
        if (tb == null) return;
        bool active = _activeDetSort == column;
        if (active)
        {
            tb.Text = column switch
            {
                "codigo" => _sortDetCodigoAsc ? "v" : "^",
                "nombre" => _sortDetNombreAsc ? "v" : "^",
                "sku" => _sortDetSkuAsc ? "v" : "^",
                "ubicacion" => _sortDetUbicacionAsc ? "v" : "^",
                "fecha" => _sortDetFechaAsc ? "v" : "^",
                _ => "v"
            };
            tb.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C00000"));
        }
        else
        {
            tb.Text = "v";
            tb.Foreground = (Brush)FindResource("MaterialDesignBodyLight");
        }
    }

    private void ApplyDetSort()
    {
        var items = _articulosVM.Articulos.ToList();
        var sorted = _activeDetSort switch
        {
            "codigo" => _sortDetCodigoAsc ? items.OrderBy(a => a.Codigo).ToList() : items.OrderByDescending(a => a.Codigo).ToList(),
            "nombre" => _sortDetNombreAsc ? items.OrderBy(a => a.Nombre).ToList() : items.OrderByDescending(a => a.Nombre).ToList(),
            "sku" => _sortDetSkuAsc ? items.OrderBy(a => a.Sku ?? "").ToList() : items.OrderByDescending(a => a.Sku ?? "").ToList(),
            "ubicacion" => _sortDetUbicacionAsc ? items.OrderBy(a => a.Departamento).ToList() : items.OrderByDescending(a => a.Departamento).ToList(),
            "fecha" => _sortDetFechaAsc ? items.OrderBy(a => a.FechaModificacion).ToList() : items.OrderByDescending(a => a.FechaModificacion).ToList(),
            _ => items.OrderByDescending(a => a.FechaModificacion).ToList()
        };
        _articulosVM.Articulos.Clear();
        foreach (var item in sorted)
            _articulosVM.Articulos.Add(item);
    }

    private void SortDet(string column, ref bool asc)
    {
        if (_activeDetSort == column)
            asc = !asc;
        else
        {
            _activeDetSort = column;
            asc = false;
        }
        ApplyDetSort();
        UpdateDetSortIndicators();
    }

    private void BtnSortDetCodigo_Click(object sender, MouseButtonEventArgs e) => SortDet("codigo", ref _sortDetCodigoAsc);
    private void BtnSortDetNombre_Click(object sender, MouseButtonEventArgs e) => SortDet("nombre", ref _sortDetNombreAsc);
    private void BtnSortDetSku_Click(object sender, MouseButtonEventArgs e) => SortDet("sku", ref _sortDetSkuAsc);
    private void BtnSortDetUbicacion_Click(object sender, MouseButtonEventArgs e) => SortDet("ubicacion", ref _sortDetUbicacionAsc);
    private void BtnSortDetFecha_Click(object sender, MouseButtonEventArgs e) => SortDet("fecha", ref _sortDetFechaAsc);

    private void BtnVerDetalleArticulo_Click(object sender, RoutedEventArgs e)
    {
        var art = (Articulo)((Button)sender).Tag;
        LblFichaCodigo.Text = art.Codigo;
        LblFichaNombre.Text = art.Nombre;
        LblFichaMarca.Text = string.IsNullOrWhiteSpace(art.Marca) ? "N/A" : art.Marca;
        LblFichaModelo.Text = string.IsNullOrWhiteSpace(art.Modelo) ? "N/A" : art.Modelo;
        LblFichaSku.Text = string.IsNullOrWhiteSpace(art.Sku) ? "N/A" : art.Sku;
        LblFichaCategoria.Text = art.Categoria?.Nombre ?? _detallesCategoria?.Nombre ?? "";
        LblFichaUbicacion.Text = art.Departamento;
        LblFichaFechaIngreso.Text = art.FechaIngreso.ToString("dd/MM/yyyy hh:mm tt");
        LblFichaFecha.Text = art.FechaModificacion.ToString("dd/MM/yyyy hh:mm tt");
        LblFichaPrecio.Text = NumericInput.FormatearPrecio(art.Precio);
        LblFichaNotas.Text = string.IsNullOrWhiteSpace(art.Notas) ? "N/A" : art.Notas;
        ShowPanel(PanelFichaArticulo);
    }

    private void BtnVolverFichaArticulo_Click(object sender, RoutedEventArgs e)
    {
        ShowPanel(PanelDetalles);
    }

    private async void BtnEliminarDetalleArticulo_Click(object sender, RoutedEventArgs e)
    {
        var art = (Articulo)((Button)sender).Tag;
        if (MessageBox.Show($"¿Eliminar artículo '{art.Nombre}'?", "Confirmar",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            await _articulosService.DeleteArticuloAsync(art.Id);
            if (_detallesCategoria != null)
            {
                await _articulosVM.LoadArticulosAsync(_detallesCategoria.Id);
                LblDetallesConteo.Text = $"({_articulosVM.TotalRegistros} artículos)";
                UpdateDetallesEmptyState();
            }
        }
    }

    private void BtnNueva_Click(object sender, RoutedEventArgs e)
    {
        TxtNombreNueva.Text = string.Empty;
        LblErrorNueva.Text = string.Empty;
        ShowPanel(PanelNueva);
    }

    // Refresco manual: invalida la caché y recarga desde la base local.
    private async void BtnRefrescar_Click(object sender, RoutedEventArgs e)
    {
        InventarioService.InvalidarTodaLaCache();
        await _vm.LoadAsync();
        await _articulosVM.LoadAsync();
        if (_detallesCategoria != null)
        {
            await _articulosVM.LoadArticulosAsync(_detallesCategoria.Id);
            LblDetallesConteo.Text = $"({_articulosVM.TotalRegistros} artículos)";
            UpdateDetallesEmptyState();
        }
    }

    private void BtnCancelarNueva_Click(object sender, RoutedEventArgs e)
    {
        ShowPanel(PanelLista);
    }

    private async void BtnGuardarNueva_Click(object sender, RoutedEventArgs e)
    {
        var nombre = TxtNombreNueva.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(nombre))
        {
            LblErrorNueva.Text = "El nombre es obligatorio.";
            return;
        }

        var service = new InventarioService();
        var created = await service.CreateCategoriaAsync(nombre);
        if (!created)
        {
            LblErrorNueva.Text = "Ya existe una categoría con ese nombre.";
            return;
        }
        await _vm.LoadAsync();
        await _articulosVM.LoadAsync();
        ShowPanel(PanelLista);
    }

    private void BtnEditar_Click(object sender, RoutedEventArgs e)
    {
        var cat = (Categoria)((Button)sender).Tag;
        _editingCategoria = cat;
        TxtNombreEditar.Text = cat.Nombre;
        LblErrorEditar.Text = string.Empty;
        ShowPanel(PanelEditar);
    }

    private void BtnCancelarEditar_Click(object sender, RoutedEventArgs e)
    {
        _editingCategoria = null;
        ShowPanel(PanelLista);
    }

    private async void BtnGuardarEditar_Click(object sender, RoutedEventArgs e)
    {
        if (_editingCategoria == null) return;

        var nombre = TxtNombreEditar.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(nombre))
        {
            LblErrorEditar.Text = "El nombre es obligatorio.";
            return;
        }

        var service = new InventarioService();
        await service.UpdateCategoriaAsync(_editingCategoria.Id, nombre);
        await _vm.LoadAsync();
        await _articulosVM.LoadAsync();

        _editingCategoria = null;
        ShowPanel(PanelLista);
    }

    private async void BtnEliminar_Click(object sender, RoutedEventArgs e)
    {
        var cat = (Categoria)((Button)sender).Tag;
        if (cat.CantidadArticulos > 0)
        {
            MessageBox.Show("No se puede eliminar una categoría que tiene artículos.", "Aviso",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (MessageBox.Show($"¿Eliminar categoría '{cat.Nombre}'?", "Confirmar",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            var service = new InventarioService();
            await service.DeleteCategoriaAsync(cat.Id);
            await _vm.LoadAsync();
            await _articulosVM.LoadAsync();
        }
    }
}
