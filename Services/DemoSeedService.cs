using InventarioAppDesktop.Data;
using InventarioAppDesktop.Models;

namespace InventarioAppDesktop.Services;

// Datos ficticios para la demo local: 3 categorías y 15 productos.
// Son los mismos datos del script seed_demo.sql; aquí se
// siembran automáticamente cuando la base local está vacía.
public static class DemoSeedService
{
    public static void SembrarSiVacio(InventarioDbContext db)
    {
        if (db.Categorias.Any())
            return;

        var electronica = new Categoria { Nombre = "Electrónica" };
        var hogar = new Categoria { Nombre = "Hogar y Cocina" };
        var oficina = new Categoria { Nombre = "Oficina y Papelería" };
        db.Categorias.AddRange(electronica, hogar, oficina);
        db.SaveChanges();

        var f = new DateTime(2026, 8, 10, 9, 0, 0);
        db.Articulos.AddRange(
            // Electrónica
            new Articulo { Codigo = "ELEC-001", Nombre = "Laptop ultradelgada 14\"", Sku = "VOL-V14-AIR", Marca = "Voltedge", Modelo = "V14-Air", Departamento = "Tecnología", Notas = "16 GB RAM, 512 GB SSD.", CategoriaId = electronica.Id, Precio = 899.99m, FechaIngreso = f, FechaModificacion = f },
            new Articulo { Codigo = "ELEC-002", Nombre = "Mouse inalámbrico ergonómico", Sku = "VOL-M220", Marca = "Voltedge", Modelo = "M220", Departamento = "Tecnología", CategoriaId = electronica.Id, Precio = 29.99m, FechaIngreso = f, FechaModificacion = f },
            new Articulo { Codigo = "ELEC-003", Nombre = "Teclado mecánico retroiluminado", Sku = "KEY-TKL-RGB", Marca = "KeyPro", Modelo = "TKL-RGB", Departamento = "Tecnología", CategoriaId = electronica.Id, Precio = 79.50m, FechaIngreso = f, FechaModificacion = f },
            new Articulo { Codigo = "ELEC-004", Nombre = "Monitor 27\" 4K UHD", Sku = "VIS-27UHD", Marca = "Viso", Modelo = "U27-Pro", Departamento = "Tecnología", Notas = "Panel IPS, HDR10.", CategoriaId = electronica.Id, Precio = 349.00m, FechaIngreso = f, FechaModificacion = f },
            new Articulo { Codigo = "ELEC-005", Nombre = "Audífonos bluetooth con micrófono", Sku = "SON-BT500", Marca = "Sonica", Modelo = "BT-500", Departamento = "Tecnología", CategoriaId = electronica.Id, Precio = 59.99m, FechaIngreso = f, FechaModificacion = f },
            // Hogar y Cocina
            new Articulo { Codigo = "HOG-001", Nombre = "Cafetera espresso 19 bares", Sku = "BRE-ESP19", Marca = "BrewMaster", Modelo = "ESP-19", Departamento = "Hogar", Notas = "Incluye vaporizador de leche.", CategoriaId = hogar.Id, Precio = 149.99m, FechaIngreso = f, FechaModificacion = f },
            new Articulo { Codigo = "HOG-002", Nombre = "Juego de sartenes antiadherentes 5 pzs", Sku = "COC-NS5", Marca = "Cocina+", Modelo = "NS-5", Departamento = "Hogar", CategoriaId = hogar.Id, Precio = 89.99m, FechaIngreso = f, FechaModificacion = f },
            new Articulo { Codigo = "HOG-003", Nombre = "Aspiradora robot con mapeo láser", Sku = "ROB-VAC-LIDAR", Marca = "RoboHome", Modelo = "Vac-Lidar", Departamento = "Hogar", CategoriaId = hogar.Id, Precio = 259.00m, FechaIngreso = f, FechaModificacion = f },
            new Articulo { Codigo = "HOG-004", Nombre = "Lámpara LED de escritorio regulable", Sku = "LUM-DESK-DIM", Marca = "Lumina", Modelo = "Desk-Dim", Departamento = "Hogar", CategoriaId = hogar.Id, Precio = 34.50m, FechaIngreso = f, FechaModificacion = f },
            new Articulo { Codigo = "HOG-005", Nombre = "Organizador de cocina giratorio", Sku = "COC-ORG-360", Marca = "Cocina+", Modelo = "ORG-360", Departamento = "Hogar", CategoriaId = hogar.Id, Precio = 24.99m, FechaIngreso = f, FechaModificacion = f },
            // Oficina y Papelería
            new Articulo { Codigo = "OFI-001", Nombre = "Silla ergonómica con soporte lumbar", Sku = "ERG-CHAIR-LX", Marca = "ErgoSit", Modelo = "LX-200", Departamento = "Oficina", CategoriaId = oficina.Id, Precio = 199.99m, FechaIngreso = f, FechaModificacion = f },
            new Articulo { Codigo = "OFI-002", Nombre = "Escritorio ajustable eléctrico", Sku = "ERG-DESK-E", Marca = "ErgoSit", Modelo = "Desk-E", Departamento = "Oficina", Notas = "Altura 70-120 cm, 2 motores.", CategoriaId = oficina.Id, Precio = 329.00m, FechaIngreso = f, FechaModificacion = f },
            new Articulo { Codigo = "OFI-003", Nombre = "Papel bond A4 75 g (caja 5 millares)", Sku = "PAP-A4-5K", Marca = "PaperMax", Modelo = "A4-75", Departamento = "Oficina", CategoriaId = oficina.Id, Precio = 42.00m, FechaIngreso = f, FechaModificacion = f },
            new Articulo { Codigo = "OFI-004", Nombre = "Set bolígrafos de gel 12 pzs", Sku = "ESCR-GEL12", Marca = "Escribo", Modelo = "Gel-12", Departamento = "Oficina", CategoriaId = oficina.Id, Precio = 12.99m, FechaIngreso = f, FechaModificacion = f },
            new Articulo { Codigo = "OFI-005", Nombre = "Archivero metálico 4 gavetas", Sku = "MUE-FILE4", Marca = "MetalOffice", Modelo = "File-4", Departamento = "Oficina", CategoriaId = oficina.Id, Precio = 139.50m, FechaIngreso = f, FechaModificacion = f }
        );
        db.SaveChanges();
    }
}
