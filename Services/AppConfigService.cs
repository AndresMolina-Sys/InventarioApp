using System.IO;

namespace InventarioAppDesktop.Services;

// Demo local: la única base de datos es SQLite, en un archivo junto al
// ejecutable. No hay modos de conexión, ni asistente de configuración,
// ni credenciales externas que gestionar.
public static class AppConfigService
{
    public static string RutaBaseDatosLocal()
        => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "inventario_demo.db");
}
