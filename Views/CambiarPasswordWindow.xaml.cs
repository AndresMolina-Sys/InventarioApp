using System.Windows;
using InventarioAppDesktop.Services;

namespace InventarioAppDesktop.Views;

public partial class CambiarPasswordWindow : Window
{
    private readonly int _userId;
    private readonly InventarioService _service = new();

    public CambiarPasswordWindow(int userId)
    {
        InitializeComponent();
        _userId = userId;
        Loaded += (_, _) => TxtNueva.Focus();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
    {
        LblError.Text = string.Empty;

        var nueva = TxtNueva.Password;
        var confirmar = TxtConfirmar.Password;

        if (nueva.Length < 8)
        {
            LblError.Text = "La contraseña debe tener al menos 8 caracteres.";
            return;
        }
        if (nueva.Length > 64)
        {
            LblError.Text = "La contraseña no puede superar los 64 caracteres.";
            return;
        }
        if (nueva != confirmar)
        {
            LblError.Text = "Las contraseñas no coinciden.";
            return;
        }

        var ok = await _service.ForcePasswordChangeAsync(_userId, nueva);
        if (!ok)
        {
            LblError.Text = "No se pudo cambiar la contraseña. Intente nuevamente.";
            return;
        }

        DialogResult = true;
        Close();
    }
}
