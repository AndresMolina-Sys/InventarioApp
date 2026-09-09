using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using InventarioAppDesktop.Data;
using InventarioAppDesktop.Models;
using InventarioAppDesktop.Services;

namespace InventarioAppDesktop.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly InventarioService _service = new();

    private string _currentPage = "dashboard";
    public string CurrentPage
    {
        get => _currentPage;
        set => SetProperty(ref _currentPage, value);
    }

    private string _tituloVentana = "InventarioApp - Inventario";
    public string TituloVentana
    {
        get => _tituloVentana;
        set => SetProperty(ref _tituloVentana, value);
    }

    public bool EsAdmin => Session.IsAdmin;
    public string NombreUsuario
    {
        get => Session.UsuarioActual?.NombreUsuario ?? "";
        set => OnPropertyChanged();
    }

    public ICommand NavigateCommand { get; }
    public ICommand LogoutCommand { get; }

    public DashboardViewModel DashboardVM { get; } = new();
    public ArticulosViewModel ArticulosVM { get; } = new();
    public CategoriasViewModel CategoriasVM { get; } = new();
    public UsuariosViewModel UsuariosVM { get; } = new();

    public MainViewModel()
    {
        NavigateCommand = new RelayCommand(async page =>
        {
            CurrentPage = page?.ToString() ?? "dashboard";
            TituloVentana = CurrentPage switch
            {
                "articulos" => "InventarioApp - Artículos",
                "articulos_ficha" => "InventarioApp - Ficha Técnica",
                "categorias" => "InventarioApp - Categorías",
                "usuarios" => "InventarioApp - Usuarios",
                "perfil" => "InventarioApp - Mi Perfil",
                _ => "InventarioApp - Inventario"
            };
            if (CurrentPage == "dashboard")
                await DashboardVM.LoadAsync();
            else if (CurrentPage == "articulos")
                await ArticulosVM.LoadAsync();
            else if (CurrentPage == "categorias")
                await CategoriasVM.LoadAsync();
            else if (CurrentPage == "usuarios" && EsAdmin)
                await UsuariosVM.LoadAsync();
        });
        LogoutCommand = new RelayCommand(_ =>
        {
            Session.Clear();
            var login = new Views.LoginWindow();
            login.Show();
            Application.Current.Windows.OfType<Views.MainWindow>().FirstOrDefault()?.Close();
        });
    }

    public async Task LoadAllAsync()
    {
        await DashboardVM.LoadAsync();
        await ArticulosVM.LoadAsync();
        await CategoriasVM.LoadAsync();
        if (EsAdmin)
            await UsuariosVM.LoadAsync();
    }
}
