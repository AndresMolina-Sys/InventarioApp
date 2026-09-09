using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using InventarioAppDesktop.Models;
using InventarioAppDesktop.Services;

namespace InventarioAppDesktop.Views;

public partial class UsuariosPage : UserControl
{
    private ViewModels.UsuariosViewModel _vm => ((ViewModels.MainViewModel)DataContext).UsuariosVM;
    private Usuario? _editingUser;
    private string _activeSort = "";
    private bool _sortIdAsc = true;
    private bool _sortNombreAsc = true;
    private bool _sortRolAsc = true;

    public UsuariosPage()
    {
        InitializeComponent();
    }

    private void ShowPanel(StackPanel panel)
    {
        PanelLista.Visibility = Visibility.Collapsed;
        PanelEditar.Visibility = Visibility.Collapsed;
        PanelNuevo.Visibility = Visibility.Collapsed;
        panel.Visibility = Visibility.Visible;
    }

    private void BtnNueva_Click(object sender, RoutedEventArgs e)
    {
        TxtNombreNuevo.Text = string.Empty;
        TxtPasswordNuevo.Password = string.Empty;
        CmbRolNuevo.SelectedIndex = 1;
        LblErrorNuevo.Text = string.Empty;
        ShowPanel(PanelNuevo);
    }

    private void BtnEditar_Click(object sender, RoutedEventArgs e)
    {
        var user = (Usuario)((Button)sender).Tag;
        _editingUser = user;
        TxtNombreEditar.Text = user.NombreUsuario;
        TxtPasswordEditar.Password = string.Empty;
        CmbRolEditar.SelectedIndex = user.Rol == "admin" ? 0 : 1;
        LblErrorEditar.Text = string.Empty;
        ShowPanel(PanelEditar);
    }

    private void BtnCancelarEditar_Click(object sender, RoutedEventArgs e)
    {
        ShowPanel(PanelLista);
    }

    private void BtnCancelarNuevo_Click(object sender, RoutedEventArgs e)
    {
        ShowPanel(PanelLista);
    }

    private async void BtnGuardarEditar_Click(object sender, RoutedEventArgs e)
    {
        if (_editingUser == null) return;

        var nombre = TxtNombreEditar.Text.Trim();
        var password = TxtPasswordEditar.Password;
        var rol = ((ComboBoxItem)CmbRolEditar.SelectedItem).Content.ToString()!;

        if (string.IsNullOrWhiteSpace(nombre))
        {
            LblErrorEditar.Text = "El nombre de usuario es obligatorio.";
            return;
        }

        if (!string.IsNullOrWhiteSpace(password))
        {
            var error = InventarioService.ValidarPassword(password);
            if (error != null)
            {
                LblErrorEditar.Text = error;
                return;
            }
        }

        var service = new InventarioService();
        var success = await service.UpdateUsuarioAsync(_editingUser.Id, nombre, password, rol);

        if (!success)
        {
            LblErrorEditar.Text = "No se pudo guardar. Verifique el nombre, la contraseña (8-64 caracteres) o que no sea el último administrador.";
            return;
        }

        ShowPanel(PanelLista);
        _ = _vm.LoadAsync();
    }

    private async void BtnGuardarNuevo_Click(object sender, RoutedEventArgs e)
    {
        var nombre = TxtNombreNuevo.Text.Trim();
        var password = TxtPasswordNuevo.Password;
        var rol = ((ComboBoxItem)CmbRolNuevo.SelectedItem).Content.ToString()!;

        if (string.IsNullOrWhiteSpace(nombre))
        {
            LblErrorNuevo.Text = "El nombre de usuario es obligatorio.";
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            LblErrorNuevo.Text = "La contraseña es obligatoria.";
            return;
        }

        var errorPassword = InventarioService.ValidarPassword(password);
        if (errorPassword != null)
        {
            LblErrorNuevo.Text = errorPassword;
            return;
        }

        var service = new InventarioService();
        var success = await service.CreateUsuarioAsync(nombre, password, rol);

        if (!success)
        {
            LblErrorNuevo.Text = "El nombre de usuario ya existe o la contraseña no es válida.";
            return;
        }

        ShowPanel(PanelLista);
        _ = _vm.LoadAsync();
    }

    private async void BtnEliminar_Click(object sender, RoutedEventArgs e)
    {
        var user = (Usuario)((Button)sender).Tag;
        if (user.Id == Data.Session.UsuarioActual?.Id)
        {
            MessageBox.Show("No puede eliminar su propio usuario.", "Aviso",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (MessageBox.Show($"¿Eliminar usuario '{user.NombreUsuario}'?", "Confirmar",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            var service = new InventarioService();
            var ok = await service.DeleteUsuarioAsync(user.Id);
            if (!ok)
            {
                MessageBox.Show("No se puede eliminar al último administrador.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            _ = _vm.LoadAsync();
        }
    }

    private void UpdateSortIndicators()
    {
        UpdateSortLabel(LblSortId, "id");
        UpdateSortLabel(LblSortNombreUsuario, "nombre");
        UpdateSortLabel(LblSortRol, "rol");
    }

    private void UpdateSortLabel(TextBlock? tb, string column)
    {
        if (tb == null) return;
        bool active = _activeSort == column;
        if (active)
        {
            tb.Text = column switch
            {
                "id" => _sortIdAsc ? "v" : "^",
                "nombre" => _sortNombreAsc ? "v" : "^",
                "rol" => _sortRolAsc ? "v" : "^",
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

    private void ApplySort()
    {
        var items = _vm.Usuarios.ToList();
        var sorted = _activeSort switch
        {
            "id" => _sortIdAsc ? items.OrderBy(u => u.Id).ToList() : items.OrderByDescending(u => u.Id).ToList(),
            "nombre" => _sortNombreAsc ? items.OrderBy(u => u.NombreUsuario).ToList() : items.OrderByDescending(u => u.NombreUsuario).ToList(),
            "rol" => _sortRolAsc ? items.OrderBy(u => u.Rol).ToList() : items.OrderByDescending(u => u.Rol).ToList(),
            _ => items.ToList()
        };
        _vm.Usuarios.Clear();
        foreach (var item in sorted)
            _vm.Usuarios.Add(item);
    }

    private void SortBy(string column, ref bool asc)
    {
        if (_activeSort == column)
            asc = !asc;
        else
        {
            _activeSort = column;
            asc = false;
        }
        ApplySort();
        UpdateSortIndicators();
    }

    private void BtnSortId_Click(object sender, MouseButtonEventArgs e) => SortBy("id", ref _sortIdAsc);
    private void BtnSortNombreUsuario_Click(object sender, MouseButtonEventArgs e) => SortBy("nombre", ref _sortNombreAsc);
    private void BtnSortRol_Click(object sender, MouseButtonEventArgs e) => SortBy("rol", ref _sortRolAsc);
}

public class RolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is string rol && rol == "admin" ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class RolToUsuarioVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is string rol && rol != "admin" ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
