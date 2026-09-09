using System.Collections.ObjectModel;
using System.Windows.Input;
using InventarioAppDesktop.Data;
using InventarioAppDesktop.Models;
using InventarioAppDesktop.Services;

namespace InventarioAppDesktop.ViewModels;

public class CategoriasViewModel : ViewModelBase
{
    private readonly InventarioService _service = new();

    private string _filtro = string.Empty;
    public string Filtro
    {
        get => _filtro;
        set
        {
            if (SetProperty(ref _filtro, value))
                AplicarFiltro();
        }
    }

    public ObservableCollection<Categoria> Categorias { get; } = new();
    private List<Categoria> _todasCategorias = new();

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }

    public event Action<string>? ShowMessage;
    public event Func<string, string, bool>? ConfirmDelete;

    public CategoriasViewModel()
    {
        AddCommand = new RelayCommand(_ => DoAdd());
        EditCommand = new RelayCommand(cat => DoEdit((Categoria)cat!));
        DeleteCommand = new RelayCommand(cat => DoDelete((Categoria)cat!));
    }

    public async Task LoadAsync()
    {
        _todasCategorias = await _service.GetCategoriasAsync();
        AplicarFiltro();
    }

    private void AplicarFiltro()
    {
        Categorias.Clear();
        var filtered = string.IsNullOrWhiteSpace(Filtro)
            ? _todasCategorias
            : _todasCategorias.Where(c => c.Nombre.Contains(Filtro, StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var c in filtered)
            Categorias.Add(c);
    }

    private void DoAdd()
    {
        var dialog = new Views.CategoriaDialog();
        if (dialog.ShowDialog() == true)
            _ = LoadAsync();
    }

    private void DoEdit(Categoria cat)
    {
        var dialog = new Views.CategoriaDialog(cat);
        if (dialog.ShowDialog() == true)
            _ = LoadAsync();
    }

    private async void DoDelete(Categoria cat)
    {
        if (cat.CantidadArticulos > 0)
        {
            ShowMessage?.Invoke("No se puede eliminar una categoría que tiene artículos.");
            return;
        }
        if (ConfirmDelete?.Invoke("categoría", cat.Nombre) == true)
        {
            await _service.DeleteCategoriaAsync(cat.Id);
            ShowMessage?.Invoke("Categoría eliminada correctamente.");
            await LoadAsync();
        }
    }
}
