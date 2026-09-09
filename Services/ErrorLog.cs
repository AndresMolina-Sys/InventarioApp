using System;
using System.IO;

namespace InventarioAppDesktop.Services;

// Log de errores local en %AppData%\InventarioApp\InventarioAppDesktop\logs.
// Los mensajes mostrados al usuario son genéricos; el detalle queda aquí.
public static class ErrorLog
{
    private static readonly object Gate = new();

    public static void Registrar(Exception ex, string contexto)
    {
        try
        {
            var carpeta = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "InventarioApp", "InventarioAppDesktop", "logs");
            Directory.CreateDirectory(carpeta);
            var archivo = Path.Combine(carpeta, $"error-{DateTime.Now:yyyyMMdd}.log");
            lock (Gate)
            {
                File.AppendAllText(archivo,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{contexto}]\n{ex}\n\n");
            }
        }
        catch
        {
            // Nunca romper el flujo por fallar el propio log.
        }
    }
}
