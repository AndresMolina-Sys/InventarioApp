using System.Windows;
using InventarioAppDesktop.Models;
using InventarioAppDesktop.Services;

namespace InventarioAppDesktop.Views;

public partial class CategoriaDialog : Window
{
    private readonly InventarioService _service = new();
    private readonly Categoria? _existing;

    public CategoriaDialog(Categoria? existing = null)
    {
        InitializeComponent();
        _existing = existing;

        if (existing != null)
        {
            LblTitulo.Text = "Editar Categoría";
            TxtNombre.Text = existing.Nombre;
        }
        else
        {
            LblTitulo.Text = "Nueva Categoría";
        }
    }

    private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtNombre.Text))
        {
            LblError.Text = "El nombre es obligatorio.";
            return;
        }

        if (_existing != null)
        {
            var ok = await _service.UpdateCategoriaAsync(_existing.Id, TxtNombre.Text.Trim());
            if (!ok)
            {
                LblError.Text = "Ya existe una categoría con ese nombre.";
                return;
            }
        }
        else
        {
            var ok = await _service.CreateCategoriaAsync(TxtNombre.Text.Trim());
            if (!ok)
            {
                LblError.Text = "Ya existe una categoría con ese nombre.";
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
