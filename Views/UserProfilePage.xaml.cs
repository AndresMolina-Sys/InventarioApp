using System.Windows;
using System.Windows.Controls;
using InventarioAppDesktop.Data;
using InventarioAppDesktop.Services;

namespace InventarioAppDesktop.Views;

public partial class UserProfilePage : UserControl
{
    private ViewModels.MainViewModel MainVM => (ViewModels.MainViewModel)DataContext;

    public UserProfilePage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var user = Session.UsuarioActual;
        if (user == null) return;
        LblNombre.Text = user.NombreUsuario;
        LblRol.Text = user.Rol == "admin" ? "Admin" : "Usuario";
    }

    private void ShowPanel(StackPanel panel)
    {
        PanelInfo.Visibility = Visibility.Collapsed;
        PanelNombre.Visibility = Visibility.Collapsed;
        PanelPassword.Visibility = Visibility.Collapsed;
        panel.Visibility = Visibility.Visible;
    }

    private void BtnCambiarNombre_Click(object sender, RoutedEventArgs e)
    {
        var user = Session.UsuarioActual;
        if (user != null)
            TxtNombreNuevo.Text = user.NombreUsuario;
        LblErrorNombre.Text = string.Empty;
        ShowPanel(PanelNombre);
    }

    private void BtnCambiarPassword_Click(object sender, RoutedEventArgs e)
    {
        TxtPasswordActual.Password = string.Empty;
        TxtPasswordNueva.Password = string.Empty;
        TxtPasswordConfirmar.Password = string.Empty;
        LblErrorPassword.Text = string.Empty;
        LblSuccessPassword.Text = string.Empty;
        ShowPanel(PanelPassword);
    }

    private void BtnVolverNombre_Click(object sender, RoutedEventArgs e)
    {
        ShowPanel(PanelInfo);
    }

    private void BtnVolverPassword_Click(object sender, RoutedEventArgs e)
    {
        ShowPanel(PanelInfo);
    }

    private async void BtnEditarNombre_Click(object sender, RoutedEventArgs e)
    {
        var user = Session.UsuarioActual;
        if (user == null) return;

        var nuevoNombre = TxtNombreNuevo.Text.Trim();
        if (string.IsNullOrWhiteSpace(nuevoNombre))
        {
            LblErrorNombre.Text = "El nombre de usuario es obligatorio.";
            return;
        }

        var service = new InventarioService();
        var success = await service.UpdatePropioNombreAsync(user.Id, nuevoNombre);

        if (!success)
        {
            LblErrorNombre.Text = "Ya existe un usuario con ese nombre.";
            return;
        }

        user.NombreUsuario = nuevoNombre;
        LblNombre.Text = nuevoNombre;
        MainVM.NombreUsuario = user.NombreUsuario;
        _ = MainVM.UsuariosVM.LoadAsync();
        ShowPanel(PanelInfo);
    }

    private async void BtnConfirmarPassword_Click(object sender, RoutedEventArgs e)
    {
        var user = Session.UsuarioActual;
        if (user == null) return;

        var actual = TxtPasswordActual.Password;
        var nueva = TxtPasswordNueva.Password;
        var confirmar = TxtPasswordConfirmar.Password;

        LblErrorPassword.Text = string.Empty;
        LblSuccessPassword.Text = string.Empty;

        if (string.IsNullOrWhiteSpace(actual))
        {
            LblErrorPassword.Text = "Debe ingresar su contraseña actual.";
            return;
        }

        if (string.IsNullOrWhiteSpace(nueva))
        {
            LblErrorPassword.Text = "Debe ingresar una nueva contraseña.";
            return;
        }

        var error = InventarioService.ValidarPassword(nueva);
        if (error != null)
        {
            LblErrorPassword.Text = error;
            return;
        }

        if (nueva != confirmar)
        {
            LblErrorPassword.Text = "Las contraseñas no coinciden.";
            return;
        }

        var service = new InventarioService();
        var success = await service.UpdatePasswordAsync(user.Id, actual, nueva);

        if (!success)
        {
            LblErrorPassword.Text = "La contraseña actual es incorrecta.";
            return;
        }

        TxtPasswordActual.Password = string.Empty;
        TxtPasswordNueva.Password = string.Empty;
        TxtPasswordConfirmar.Password = string.Empty;
        ShowPanel(PanelInfo);
        MessageBox.Show("Contraseña actualizada correctamente.", "Éxito",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
