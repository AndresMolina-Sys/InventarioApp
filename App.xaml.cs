using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using InventarioAppDesktop.Data;
using InventarioAppDesktop.Models;
using InventarioAppDesktop.Services;
using InventarioAppDesktop.Views;

namespace InventarioAppDesktop;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        RegistrarManejadoresGlobales();

        if (!IntentarIniciar())
            Shutdown();
    }

    // Demo local: arranque directo a la ventana de login con SQLite.
    // No hay asistente de conexión ni modos de servidor.
    private bool IntentarIniciar()
    {
        try
        {
            InicializarBaseDeDatos();

            var login = new LoginWindow();
            login.Show();
            return true;
        }
        catch (Exception ex)
        {
            ErrorLog.Registrar(ex, "Arranque");
            MessageBox.Show(
                "No se pudo iniciar la aplicación.\n\n" +
                "Los detalles quedaron en el registro de la aplicación.",
                "Error de inicio",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return false;
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Detiene la cola de trabajos en segundo plano al cerrar la app.
        BackgroundJobQueue.Default.Shutdown();
        base.OnExit(e);
    }

    private void RegistrarManejadoresGlobales()
    {
        DispatcherUnhandledException += (_, args) =>
        {
            ErrorLog.Registrar(args.Exception, "UI");
            MessageBox.Show(
                "Ocurrió un error inesperado. Los detalles quedaron en el registro de la aplicación.",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                ErrorLog.Registrar(ex, "Fatal");
                MessageBox.Show(
                    "Error fatal de la aplicación. Los detalles quedaron en el registro de la aplicación.",
                    "Error fatal",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            ErrorLog.Registrar(args.Exception, "Background");
            MessageBox.Show(
                "Error en una tarea en segundo plano.",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.SetObserved();
        };
    }

    private static void InicializarBaseDeDatos()
    {
        // SQLite local: se crea si no existe y se siembran los datos demo.
        using var db = new InventarioDbContext();
        db.Database.EnsureCreated();
        AplicarMigracionesSqlite(db);
        SembrarAdminInicial(db);
        DemoSeedService.SembrarSiVacio(db);
    }

    private static void SembrarAdminInicial(InventarioDbContext db)
    {
        if (db.Usuarios.Any())
            return;

        var passwordTemporal = GenerarPasswordAleatoria();
        db.Usuarios.Add(new Usuario
        {
            NombreUsuario = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(passwordTemporal, 12),
            Rol = "admin",
            DebeCambiarPassword = true
        });
        db.SaveChanges();

        MessageBox.Show(
            "Se creó la cuenta de administrador inicial.\n\n" +
            $"Usuario: admin\n" +
            $"Contraseña temporal: {passwordTemporal}\n\n" +
            "Anote esta contraseña. Deberá cambiarla al iniciar sesión.",
            "Configuración inicial",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    // Solo aplica a bases SQLite locales existentes creadas antes del campo DebeCambiarPassword
    // o antes del meta indexing (EnsureCreated no altera bases existentes).
    private static void AplicarMigracionesSqlite(InventarioDbContext db)
    {
        try
        {
            var tieneColumna = db.Database
                .SqlQueryRaw<string>("SELECT name FROM pragma_table_info('Usuarios') WHERE name = 'DebeCambiarPassword'")
                .Any();
            if (!tieneColumna)
                db.Database.ExecuteSqlRaw("ALTER TABLE Usuarios ADD COLUMN DebeCambiarPassword INTEGER NOT NULL DEFAULT 0");
        }
        catch
        {
            // Best-effort: si la columna ya existe no debe abortar el arranque.
        }

// Campos opcionales de Articulos (EnsureCreated no altera bases existentes).
        // SQL constante por columna: DDL de SQLite no admite parámetros para
        // nombres de columna y los valores son literales fijos del código.
        var columnasArticulo = new[]
        {
            new { Consulta = "SELECT name FROM pragma_table_info('Articulos') WHERE name = 'Marca'",
                  Alter = "ALTER TABLE Articulos ADD COLUMN Marca TEXT NULL" },
            new { Consulta = "SELECT name FROM pragma_table_info('Articulos') WHERE name = 'Modelo'",
                  Alter = "ALTER TABLE Articulos ADD COLUMN Modelo TEXT NULL" },
            new { Consulta = "SELECT name FROM pragma_table_info('Articulos') WHERE name = 'Notas'",
                  Alter = "ALTER TABLE Articulos ADD COLUMN Notas TEXT NULL" },
            new { Consulta = "SELECT name FROM pragma_table_info('Articulos') WHERE name = 'Precio'",
                  Alter = "ALTER TABLE Articulos ADD COLUMN Precio TEXT NULL" }
        };
        foreach (var columna in columnasArticulo)
        {
            try
            {
                if (!db.Database.SqlQueryRaw<string>(columna.Consulta).Any())
                    db.Database.ExecuteSqlRaw(columna.Alter);
            }
            catch
            {
                // Best-effort: una columna fallida no debe bloquear el arranque.
            }
        }

        // Meta indexing para bases locales creadas antes de los índices.
        string[] indices = {
            "CREATE INDEX IF NOT EXISTS IX_Articulos_CategoriaId_Nombre ON Articulos (CategoriaId, Nombre);",
            "CREATE INDEX IF NOT EXISTS IX_Articulos_FechaModificacion ON Articulos (FechaModificacion);",
            "CREATE INDEX IF NOT EXISTS IX_Articulos_Codigo ON Articulos (Codigo);",
            "CREATE INDEX IF NOT EXISTS IX_Categorias_Nombre ON Categorias (Nombre);",
            "DROP INDEX IF EXISTS IX_Articulos_CategoriaId;"
        };
        foreach (var sql in indices)
        {
            try
            {
                db.Database.ExecuteSqlRaw(sql);
            }
            catch
            {
                // Best-effort: un índice fallido no debe bloquear el arranque.
            }
        }
    }

    private static string GenerarPasswordAleatoria(int length = 14)
    {
        const string chars = "abcdefghijkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789!@#$%&*";
        var sb = new StringBuilder(length);
        for (var i = 0; i < length; i++)
            sb.Append(chars[RandomNumberGenerator.GetInt32(chars.Length)]);
        return sb.ToString();
    }
}
