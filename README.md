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

## Estructura del proyecto

```
├── Data/          # DbContext (solo SQLite)
├── Models/        # Entidades (Articulo, Categoria, Usuario)
├── ViewModels/    # Lógica de presentación (MVVM)
├── Views/         # Ventanas y páginas XAML
├── Services/      # Login, caché, logging, semillas demo
├── Resources/     # Logo e icono genéricos
└── seed_demo.sql  # Datos ficticios (3 categorías, 15 productos)
```

## Requisitos

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (para compilar)

## Cómo ejecutar

```powershell
# 1. Clonar el repositorio
git clone https://github.com/AndresMolina-Sys/InventarioApp.git
cd InventarioApp

# 2. Ejecutar (compila automáticamente la primera vez)
dotnet run
```

Alternativa: abrir `InventarioAppDesktop.csproj` en Visual Studio 2022 y presionar F5.

En el primer arranque la aplicación crea la base local (`inventario_demo.db` junto al ejecutable), muestra una cuenta `admin` con contraseña temporal (deberá cambiarse al iniciar sesión) y siembra los datos de demostración.

## Datos demo

El script `seed_demo.sql` contiene los mismos datos que la app siembra automáticamente (3 categorías y 15 productos ficticios). Para restablecerlos manualmente sobre una base existente:

```powershell
sqlite3 inventario_demo.db < seed_demo.sql
```

## Licencia

Copyright © 2026 InventarioApp.
