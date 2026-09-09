using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using InventarioAppDesktop.Data;
using InventarioAppDesktop.Models;
using InventarioAppDesktop.Services;

namespace InventarioAppDesktop.Views;

public partial class ArticulosPage : UserControl
{
    private string _activeArtSort = "";
    private bool _sortNombreArtAsc = true;
    private bool _sortCategoriaArtAsc = true;
    private bool _sortFechaIngresoAsc = true;

    public ArticulosPage()
    {
        InitializeComponent();
        Loaded += ArticulosPage_Loaded;
        NumericInput.AdjuntarFiltroPrecio(TxtPrecio);
    }

    private void ArticulosPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (!Session.IsAdmin)
            ColAcciones.Visibility = Visibility.Collapsed;
        UpdateSortIndicators();

        if (Vm.FichaArticulo != null)
        {
            ShowFichaTecnica(Vm.FichaArticulo);
            Vm.FichaArticulo = null;
        }
    }

    private ViewModels.MainViewModel MainVm => (ViewModels.MainViewModel)DataContext;
    private ViewModels.ArticulosViewModel Vm => MainVm.ArticulosVM;
    private readonly IArticulosService _service = new ArticulosService();
    private Articulo? _editingArticulo;

    private void ShowPanel(StackPanel active)
    {
        PanelArticulos.Visibility = Visibility.Collapsed;
        PanelNuevoArticulo.Visibility = Visibility.Collapsed;
        PanelFichaTecnica.Visibility = Visibility.Collapsed;
        active.Visibility = Visibility.Visible;
    }

    private void UpdateEmptyState()
    {
        LblNoArticulos.Visibility = Vm.Articulos.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        GridArticulos.Visibility = Vm.Articulos.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    private void UpdateSortIndicators()
    {
        UpdateSortLabel(LblSortNombreArt, "nombreart");
        UpdateSortLabel(LblSortCategoriaArt, "categoriaart");
        UpdateSortLabel(LblSortFechaIngreso, "fechaingreso");
    }

    private void UpdateSortLabel(TextBlock? tb, string column)
    {
        if (tb == null) return;
        bool active = _activeArtSort == column;
        if (active)
        {
            tb.Text = column switch
            {
                "nombreart" => _sortNombreArtAsc ? "v" : "^",
                "categoriaart" => _sortCategoriaArtAsc ? "v" : "^",
                "fechaingreso" => _sortFechaIngresoAsc ? "v" : "^",
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

    private void ApplyArtSort()
    {
        var items = Vm.Articulos;
        var sorted = _activeArtSort switch
        {
            "nombreart" => _sortNombreArtAsc ? items.OrderBy(a => a.Nombre).ToList() : items.OrderByDescending(a => a.Nombre).ToList(),
            "categoriaart" => _sortCategoriaArtAsc ? items.OrderBy(a => a.Categoria?.Nombre ?? "").ToList() : items.OrderByDescending(a => a.Categoria?.Nombre ?? "").ToList(),
            "fechaingreso" => _sortFechaIngresoAsc ? items.OrderBy(a => a.FechaIngreso).ToList() : items.OrderByDescending(a => a.FechaIngreso).ToList(),
            _ => items.ToList()
        };
        items.Clear();
        foreach (var item in sorted) items.Add(item);
    }

    private void BtnSortNombreArt_Click(object sender, MouseButtonEventArgs e)
    {
        _activeArtSort = "nombreart";
        _sortNombreArtAsc = !_sortNombreArtAsc;
        UpdateSortIndicators();
        ApplyArtSort();
    }

    private void BtnSortCategoriaArt_Click(object sender, MouseButtonEventArgs e)
    {
        _activeArtSort = "categoriaart";
        _sortCategoriaArtAsc = !_sortCategoriaArtAsc;
        UpdateSortIndicators();
        ApplyArtSort();
    }

    private void BtnSortFechaIngreso_Click(object sender, MouseButtonEventArgs e)
    {
        _activeArtSort = "fechaingreso";
        _sortFechaIngresoAsc = !_sortFechaIngresoAsc;
        UpdateSortIndicators();
        ApplyArtSort();
    }

    private void BtnNuevoArticulo_Click(object sender, RoutedEventArgs e)
    {
        OpenFormNew();
    }

    // Refresco manual: invalida la caché y recarga desde la base local.
    private async void BtnRefrescar_Click(object sender, RoutedEventArgs e)
    {
        InventarioService.InvalidarTodaLaCache();
        await MainVm.DashboardVM.LoadAsync();
        await MainVm.CategoriasVM.LoadAsync();
        await Vm.LoadAsync();
        UpdateEmptyState();
    }

    private void OpenFormNew()
    {
        _editingArticulo = null;
        LblFormTitle.Text = "Nuevo Artículo";
        LblFormError.Text = string.Empty;
        TxtCodigo.Text = string.Empty;
        TxtNombre.Text = string.Empty;
        TxtMarca.Text = string.Empty;
        TxtModelo.Text = string.Empty;
        TxtSku.Text = string.Empty;
        TxtDepartamento.Text = string.Empty;
        TxtPrecio.Text = string.Empty;
        TxtNotas.Text = string.Empty;

        CmbCategoria.ItemsSource = MainVm.CategoriasVM.Categorias;
        CmbCategoria.SelectedIndex = -1;

        ShowPanel(PanelNuevoArticulo);
    }

    private void OpenFormEdit(Articulo art)
    {
        _editingArticulo = art;
        LblFormTitle.Text = "Editar Artículo";
        LblFormError.Text = string.Empty;
        TxtCodigo.Text = art.Codigo;
        TxtNombre.Text = art.Nombre;
        TxtMarca.Text = art.Marca ?? string.Empty;
        TxtModelo.Text = art.Modelo ?? string.Empty;
        TxtSku.Text = art.Sku ?? string.Empty;
        TxtDepartamento.Text = art.Departamento;
        TxtPrecio.Text = art.Precio?.ToString("0.##") ?? string.Empty;
        TxtNotas.Text = art.Notas ?? string.Empty;

        CmbCategoria.ItemsSource = MainVm.CategoriasVM.Categorias;
        CmbCategoria.SelectedItem = MainVm.CategoriasVM.Categorias.FirstOrDefault(c => c.Id == art.CategoriaId);

        ShowPanel(PanelNuevoArticulo);
    }

    private void BtnCancelarNuevoArticulo_Click(object sender, RoutedEventArgs e)
    {
        _editingArticulo = null;
        ShowPanel(PanelArticulos);
    }

    private async void BtnGuardarNuevoArticulo_Click(object sender, RoutedEventArgs e)
    {
        LblFormError.Text = string.Empty;

        var codigo = TxtCodigo.Text?.Trim() ?? string.Empty;
        var nombre = TxtNombre.Text?.Trim() ?? string.Empty;
        var marca = TxtMarca.Text?.Trim() ?? string.Empty;
        var modelo = TxtModelo.Text?.Trim() ?? string.Empty;
        var sku = TxtSku.Text?.Trim() ?? string.Empty;
        var depto = TxtDepartamento.Text?.Trim() ?? string.Empty;
        var notas = TxtNotas.Text?.Trim() ?? string.Empty;
        var cat = CmbCategoria.SelectedItem as Categoria;

        if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(nombre) ||
            string.IsNullOrWhiteSpace(depto) || cat == null)
        {
            LblFormError.Text = "Complete todos los campos (Categoría es obligatoria).";
            return;
        }

        var precio = NumericInput.ParsePrecio(TxtPrecio.Text);
        if (precio == null && !string.IsNullOrWhiteSpace(TxtPrecio.Text))
        {
            LblFormError.Text = "El costo debe ser un número válido (ej: 12.50).";
            return;
        }

        if (_editingArticulo != null)
        {
            _editingArticulo.Codigo = codigo;
            _editingArticulo.Nombre = nombre;
            _editingArticulo.Marca = string.IsNullOrWhiteSpace(marca) ? null : marca;
            _editingArticulo.Modelo = string.IsNullOrWhiteSpace(modelo) ? null : modelo;
            _editingArticulo.Sku = string.IsNullOrWhiteSpace(sku) ? null : sku;
            _editingArticulo.Departamento = depto;
            _editingArticulo.Notas = string.IsNullOrWhiteSpace(notas) ? null : notas;
            _editingArticulo.Precio = precio;
            _editingArticulo.CategoriaId = cat.Id;
            await _service.UpdateArticuloAsync(_editingArticulo);
        }
        else
        {
            var art = new Articulo
            {
                Codigo = codigo,
                Nombre = nombre,
                Marca = string.IsNullOrWhiteSpace(marca) ? null : marca,
                Modelo = string.IsNullOrWhiteSpace(modelo) ? null : modelo,
                Sku = string.IsNullOrWhiteSpace(sku) ? null : sku,
                Departamento = depto,
                Notas = string.IsNullOrWhiteSpace(notas) ? null : notas,
                Precio = precio,
                CategoriaId = cat.Id
            };
            await _service.CreateArticuloAsync(art);
        }

        _editingArticulo = null;
        await MainVm.CategoriasVM.LoadAsync();
        await MainVm.DashboardVM.LoadAsync();
        await Vm.LoadAsync();
        UpdateEmptyState();
        ShowPanel(PanelArticulos);
    }

    private void BtnEditarArticulo_Click(object sender, RoutedEventArgs e)
    {
        var art = (Articulo)((Button)sender).Tag;
        OpenFormEdit(art);
    }

    private async void BtnEliminarArticulo_Click(object sender, RoutedEventArgs e)
    {
        var art = (Articulo)((Button)sender).Tag;
        if (MessageBox.Show($"¿Eliminar artículo '{art.Nombre}'?", "Confirmar",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            await _service.DeleteArticuloAsync(art.Id);
            await Vm.LoadAsync();
            UpdateEmptyState();
        }
    }

    private void BtnVerArticulo_Click(object sender, RoutedEventArgs e)
    {
        var art = (Articulo)((Button)sender).Tag;
        ShowFichaTecnica(art);
    }

    private void ShowFichaTecnica(Articulo art)
    {
        LblFichaCodigo.Text = art.Codigo;
        LblFichaNombre.Text = art.Nombre;
        LblFichaMarca.Text = string.IsNullOrWhiteSpace(art.Marca) ? "N/A" : art.Marca;
        LblFichaModelo.Text = string.IsNullOrWhiteSpace(art.Modelo) ? "N/A" : art.Modelo;
        LblFichaSku.Text = string.IsNullOrWhiteSpace(art.Sku) ? "N/A" : art.Sku;
        LblFichaCategoria.Text = art.Categoria?.Nombre ?? "";
        LblFichaUbicacion.Text = art.Departamento;
        LblFichaFecha.Text = art.FechaIngreso.ToString("dd/MM/yyyy hh:mm tt");
        LblFichaModificacion.Text = art.FechaModificacion.ToString("dd/MM/yyyy hh:mm tt");
        LblFichaPrecio.Text = NumericInput.FormatearPrecio(art.Precio);
        LblFichaNotas.Text = string.IsNullOrWhiteSpace(art.Notas) ? "N/A" : art.Notas;
        ShowPanel(PanelFichaTecnica);
    }

    private void BtnVolverFicha_Click(object sender, RoutedEventArgs e)
    {
        if (MainVm.CurrentPage == "articulos_ficha")
        {
            MainVm.CurrentPage = "dashboard";
            MainVm.TituloVentana = "InventarioApp - Inventario";
        }
        else
        {
            ShowPanel(PanelArticulos);
        }
    }
}
