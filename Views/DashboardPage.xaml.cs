using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using InventarioAppDesktop.Models;
using InventarioAppDesktop.Services;
using InventarioAppDesktop.ViewModels;

namespace InventarioAppDesktop.Views;

public partial class DashboardPage : UserControl
{
    private const int PageSize = 5;
    private int _currentPage = 0;
    private List<Articulo> _allRecent = new();

    public DashboardPage()
    {
        InitializeComponent();
        Loaded += DashboardPage_Loaded;
    }

    private void DashboardPage_Loaded(object sender, RoutedEventArgs e)
    {
        LoadPage();
    }

    private MainViewModel MainVm => (MainViewModel)DataContext;

    private void LoadPage()
    {
        _allRecent = MainVm.DashboardVM.ArticulosRecientes.ToList();
        var total = _allRecent.Count;
        var totalPages = total == 0 ? 0 : (int)Math.Ceiling((double)total / PageSize);
        if (_currentPage >= totalPages) _currentPage = Math.Max(0, totalPages - 1);

        var pageItems = _allRecent.Skip(_currentPage * PageSize).Take(PageSize).ToList();

        GridRecientes.ItemsSource = pageItems;

        LblNoRecientes.Visibility = total == 0 ? Visibility.Visible : Visibility.Collapsed;
        GridRecientes.Visibility = total == 0 ? Visibility.Collapsed : Visibility.Visible;

        LblPagina.Text = total == 0
            ? "Artículos Recientes"
            : $"Artículos Recientes  ({_currentPage * PageSize + 1}–{Math.Min((_currentPage + 1) * PageSize, total)} de {total})";

        BtnPrev.IsEnabled = _currentPage > 0;
        BtnNext.IsEnabled = _currentPage < totalPages - 1;

        LblNoDistribucion.Visibility = MainVm.DashboardVM.Distribucion.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void BtnPrev_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPage > 0)
        {
            _currentPage--;
            LoadPage();
        }
    }

    // Refresco manual: invalida la caché y recarga desde la base local.
    private async void BtnRefrescar_Click(object sender, RoutedEventArgs e)
    {
        InventarioService.InvalidarTodaLaCache();
        _currentPage = 0;
        await MainVm.DashboardVM.LoadAsync();
        LoadPage();
    }

    private void BtnNext_Click(object sender, RoutedEventArgs e)
    {
        var totalPages = (int)Math.Ceiling((double)_allRecent.Count / PageSize);
        if (_currentPage < totalPages - 1)
        {
            _currentPage++;
            LoadPage();
        }
    }

    private void BtnVerArticulo_Click(object sender, RoutedEventArgs e)
    {
        var art = (Articulo)((Button)sender).Tag;
        MainVm.ArticulosVM.FichaArticulo = art;
        MainVm.CurrentPage = "articulos_ficha";
        MainVm.TituloVentana = "InventarioApp - Ficha Técnica";
    }
}

public class PercentageToWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double pct)
            return Math.Max(0, pct / 100.0 * 500.0);
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class PercentageToStarConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double pct && pct > 0)
            return new GridLength(pct, GridUnitType.Star);
        return new GridLength(0, GridUnitType.Star);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
