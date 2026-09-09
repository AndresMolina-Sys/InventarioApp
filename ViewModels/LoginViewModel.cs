using System.Windows;
using System.Windows.Input;
using InventarioAppDesktop.Data;
using InventarioAppDesktop.Services;

namespace InventarioAppDesktop.ViewModels;

public class LoginViewModel : ViewModelBase
{
    private readonly InventarioService _service = new();

    private string _usuario = string.Empty;
    public string Usuario
    {
        get => _usuario;
        set => SetProperty(ref _usuario, value);
    }

    private string _mensajeError = string.Empty;
    public string MensajeError
    {
        get => _mensajeError;
        set => SetProperty(ref _mensajeError, value);
    }

    public ICommand LoginCommand { get; }

    public LoginViewModel()
    {
        LoginCommand = new RelayCommand(_ => DoLogin(), _ => CanLogin());
    }

    private bool CanLogin() => !string.IsNullOrWhiteSpace(Usuario);

    private void DoLogin()
    {
        MensajeError = string.Empty;
    }

    public async Task<bool> LoginAsync(string password)
    {
        MensajeError = string.Empty;
        var user = await _service.LoginAsync(Usuario.Trim(), password);
        if (user == null)
        {
            MensajeError = "Usuario o contraseña incorrectos.";
            return false;
        }
        Session.UsuarioActual = user;
        return true;
    }
}
