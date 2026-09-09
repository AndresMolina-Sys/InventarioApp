using System.Windows;
using InventarioAppDesktop.Models;
using InventarioAppDesktop.Services;

namespace InventarioAppDesktop.Views;

public partial class UsuarioDialog : Window
{
    private readonly InventarioService _service = new();
    private readonly Usuario? _existing;

    public UsuarioDialog(Usuario? existing = null)
    {
        InitializeComponent();
        _existing = existing;

        if (existing != null)
        {
            LblTitulo.Text = "Editar Usuario";
            TxtNombre.Text = existing.NombreUsuario;
            CmbRol.SelectedIndex = existing.Rol == "admin" ? 0 : 1;
            LblPassword.Text = "Contraseña (dejar vacío para mantener)";
        }
        else
        {
            LblTitulo.Text = "Nuevo Usuario";
        }
    }

    private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtNombre.Text))
        {
            LblError.Text = "El nombre de usuario es obligatorio.";
            return;
        }

        var rol = ((System.Windows.Controls.ComboBoxItem)CmbRol.SelectedItem).Content.ToString()!;

        if (_existing != null)
        {
            var ok = await _service.UpdateUsuarioAsync(_existing.Id, TxtNombre.Text.Trim(),
                TxtPassword.Password, rol);
            if (!ok)
            {
                LblError.Text = "No se pudo guardar. Verifique el nombre, la contraseña (8-64 caracteres) o que no sea el último administrador.";
                return;
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(TxtPassword.Password))
            {
                LblError.Text = "La contraseña es obligatoria.";
                return;
            }
            var error = InventarioService.ValidarPassword(TxtPassword.Password);
            if (error != null)
            {
                LblError.Text = error;
                return;
            }
            var ok = await _service.CreateUsuarioAsync(TxtNombre.Text.Trim(), TxtPassword.Password, rol);
            if (!ok)
            {
                LblError.Text = "El nombre de usuario ya existe o la contraseña no es válida.";
                return;
            }
        }

        DialogResult = true;
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
