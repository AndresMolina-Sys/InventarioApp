# InventarioApp (demo)

Sistema de inventario de escritorio para Windows (WPF). Versión demo de portafolio: funciona únicamente con base de datos local SQLite, sin servidores ni configuración de red.

## Tech Stack

- **Lenguaje:** C# (.NET 8, `net8.0-windows`)
- **UI:** WPF con Material Design (`MaterialDesignThemes`)
- **ORM:** Entity Framework Core 8
- **Base de datos:** SQLite local (`inventario_demo.db`, junto al ejecutable)
- **Seguridad:** Hash de contraseñas con BCrypt
- **Arquitectura:** MVVM (`Models` / `ViewModels` / `Views` + capa de `Services`)

## Características

- Gestión de artículos (CRUD, búsqueda, categorías, precios)
- Dashboard con resumen del inventario
- Gestión de usuarios y roles con autenticación
- Cambio de contraseña obligatorio en primer inicio
- Datos de demostración incluidos (3 categorías, 15 productos)
- Instalador Windows (Inno Setup)

## Estructura del proyecto

```
├── Data/          # DbContext (solo SQLite)
├── Models/        # Entidades (Articulo, Categoria, Usuario)
├── ViewModels/    # Lógica de presentación (MVVM)
├── Views/         # Ventanas y páginas XAML
├── Services/      # Login, caché, logging, semillas demo
├── Resources/     # Logo e icono genéricos
└── Installer/     # Script Inno Setup, seed SQL y utilidades de build
```

## Requisitos

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (para compilar)

## Cómo ejecutar

```powershell
# Compilar
dotnet build

# Ejecutar
dotnet run
```

En el primer arranque la aplicación crea la base local, genera una cuenta `admin` con contraseña temporal (la muestra en pantalla y deberá cambiarse al iniciar sesión) y siembra los datos de demostración.

## Datos demo

El script `Installer/seed_demo.sql` contiene los mismos datos que la app siembra automáticamente (3 categorías y 15 productos ficticios). Para restablecerlos manualmente sobre una base existente:

```powershell
sqlite3 inventario_demo.db < Installer/seed_demo.sql
```

## Licencia

Copyright © 2026 InventarioApp.
