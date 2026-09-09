using System.Collections.ObjectModel;
using System.Windows.Input;
using InventarioAppDesktop.Data;
using InventarioAppDesktop.Models;
using InventarioAppDesktop.Services;

namespace InventarioAppDesktop.ViewModels;

public class UsuariosViewModel : ViewModelBase
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

    public ObservableCollection<Usuario> Usuarios { get; } = new();
    private List<Usuario> _todosUsuarios = new();

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }

    public event Action<string>? ShowMessage;
    public event Func<string, string, bool>? ConfirmDelete;

    public UsuariosViewModel()
    {
        AddCommand = new RelayCommand(_ => DoAdd());
        EditCommand = new RelayCommand(user => DoEdit((Usuario)user!));
        DeleteCommand = new RelayCommand(user => DoDelete((Usuario)user!));
    }

    public async Task LoadAsync()
    {
        _todosUsuarios = await _service.GetUsuariosAsync();
        AplicarFiltro();
    }

    private void AplicarFiltro()
    {
        Usuarios.Clear();
        var filtered = string.IsNullOrWhiteSpace(Filtro)
            ? _todosUsuarios
            : _todosUsuarios.Where(u => u.NombreUsuario.Contains(Filtro, StringComparison.OrdinalIgnoreCase) ||
                                         u.Rol.Contains(Filtro, StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var u in filtered)
            Usuarios.Add(u);
    }

    private void DoAdd()
    {
        var dialog = new Views.UsuarioDialog();
        if (dialog.ShowDialog() == true)
            _ = LoadAsync();
    }

    private void DoEdit(Usuario user)
    {
        var dialog = new Views.UsuarioDialog(user);
        if (dialog.ShowDialog() == true)
            _ = LoadAsync();
    }

    private async void DoDelete(Usuario user)
    {
        if (user.Id == Session.UsuarioActual?.Id)
        {
            ShowMessage?.Invoke("No puede eliminar su propio usuario.");
            return;
        }
        if (ConfirmDelete?.Invoke("usuario", user.NombreUsuario) == true)
        {
            await _service.DeleteUsuarioAsync(user.Id);
            ShowMessage?.Invoke("Usuario eliminado correctamente.");
            await LoadAsync();
        }
    }
}
