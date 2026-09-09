using InventarioAppDesktop.Models;

namespace InventarioAppDesktop.Data;

public static class Session
{
    public static Usuario? UsuarioActual { get; set; }

    public static bool IsAdmin => UsuarioActual?.Rol == "admin";

    public static void Clear()
    {
        UsuarioActual = null;
    }
}
