using System.Windows;
using InventarioAppDesktop.Models;
using InventarioAppDesktop.Services;

namespace InventarioAppDesktop.Views;

public partial class ArticuloDialog : Window
{
    private readonly IArticulosService _service = new ArticulosService();
    private readonly int _categoriaId;
    private readonly Articulo? _existing;

    public ArticuloDialog(int categoriaId, Articulo? existing = null)
    {
        InitializeComponent();
        _categoriaId = categoriaId;
        _existing = existing;
        NumericInput.AdjuntarFiltroPrecio(TxtPrecio);

        if (existing != null)
        {
            LblTitulo.Text = "Editar Artículo";
            TxtCodigo.Text = existing.Codigo;
            TxtNombre.Text = existing.Nombre;
            TxtMarca.Text = existing.Marca ?? "";
            TxtModelo.Text = existing.Modelo ?? "";
            TxtSku.Text = existing.Sku ?? "";
            TxtDepartamento.Text = existing.Departamento;
            TxtPrecio.Text = existing.Precio?.ToString("0.##") ?? "";
            TxtNotas.Text = existing.Notas ?? "";
        }
        else
        {
            LblTitulo.Text = "Nuevo Artículo";
        }
    }

    private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtCodigo.Text) || string.IsNullOrWhiteSpace(TxtNombre.Text) ||
            string.IsNullOrWhiteSpace(TxtDepartamento.Text))
        {
            LblError.Text = "Complete todos los campos obligatorios.";
            return;
        }

        var precio = NumericInput.ParsePrecio(TxtPrecio.Text);
        if (precio == null && !string.IsNullOrWhiteSpace(TxtPrecio.Text))
        {
            LblError.Text = "El costo debe ser un número válido (ej: 12.50).";
            return;
        }

        if (_existing != null)
        {
            _existing.Codigo = TxtCodigo.Text.Trim();
            _existing.Nombre = TxtNombre.Text.Trim();
            _existing.Marca = string.IsNullOrWhiteSpace(TxtMarca.Text) ? null : TxtMarca.Text.Trim();
            _existing.Modelo = string.IsNullOrWhiteSpace(TxtModelo.Text) ? null : TxtModelo.Text.Trim();
            _existing.Sku = string.IsNullOrWhiteSpace(TxtSku.Text) ? null : TxtSku.Text.Trim();
            _existing.Departamento = TxtDepartamento.Text.Trim();
            _existing.Notas = string.IsNullOrWhiteSpace(TxtNotas.Text) ? null : TxtNotas.Text.Trim();
            _existing.Precio = precio;
            await _service.UpdateArticuloAsync(_existing);
        }
        else
        {
            var art = new Articulo
            {
                Codigo = TxtCodigo.Text.Trim(),
                Nombre = TxtNombre.Text.Trim(),
                Marca = string.IsNullOrWhiteSpace(TxtMarca.Text) ? null : TxtMarca.Text.Trim(),
                Modelo = string.IsNullOrWhiteSpace(TxtModelo.Text) ? null : TxtModelo.Text.Trim(),
                Sku = string.IsNullOrWhiteSpace(TxtSku.Text) ? null : TxtSku.Text.Trim(),
                Departamento = TxtDepartamento.Text.Trim(),
                Notas = string.IsNullOrWhiteSpace(TxtNotas.Text) ? null : TxtNotas.Text.Trim(),
                Precio = precio,
                CategoriaId = _categoriaId
            };
            await _service.CreateArticuloAsync(art);
        }

        DialogResult = true;
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
