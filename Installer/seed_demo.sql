-- =====================================================================
-- seed_demo.sql — Datos ficticios para la demo local de InventarioApp
-- 3 categorías y 15 productos (mismos datos que siembra automáticamente
--  Services/DemoSeedService.cs cuando la base local está vacía).
--
-- Uso (desde la raíz del proyecto):
--   sqlite3 inventario_demo.db < Installer/seed_demo.sql
--
-- NOTA: restablece los datos demo (borra las categorías y artículos
-- existentes antes de insertar). No usar sobre una base con datos reales.
-- =====================================================================

PRAGMA foreign_keys = OFF;

DELETE FROM Articulos;
DELETE FROM Categorias;
DELETE FROM sqlite_sequence WHERE name IN ('Articulos', 'Categorias');

PRAGMA foreign_keys = ON;

INSERT INTO Categorias (Id, Nombre) VALUES
(1, 'Electrónica'),
(2, 'Hogar y Cocina'),
(3, 'Oficina y Papelería');

INSERT INTO Articulos
    (Id, Codigo, Nombre, Sku, Marca, Modelo, Departamento, Notas, CategoriaId, Precio, FechaIngreso, FechaModificacion)
VALUES
-- Electrónica
(1, 'ELEC-001', 'Laptop ultradelgada 14"', 'VOL-V14-AIR', 'Voltedge', 'V14-Air', 'Tecnología', '16 GB RAM, 512 GB SSD.', 1, 899.99, '2026-08-10 09:00:00', '2026-08-10 09:00:00'),
(2, 'ELEC-002', 'Mouse inalámbrico ergonómico', 'VOL-M220', 'Voltedge', 'M220', 'Tecnología', NULL, 1, 29.99, '2026-08-10 09:00:00', '2026-08-10 09:00:00'),
(3, 'ELEC-003', 'Teclado mecánico retroiluminado', 'KEY-TKL-RGB', 'KeyPro', 'TKL-RGB', 'Tecnología', NULL, 1, 79.50, '2026-08-10 09:00:00', '2026-08-10 09:00:00'),
(4, 'ELEC-004', 'Monitor 27" 4K UHD', 'VIS-27UHD', 'Viso', 'U27-Pro', 'Tecnología', 'Panel IPS, HDR10.', 1, 349.00, '2026-08-10 09:00:00', '2026-08-10 09:00:00'),
(5, 'ELEC-005', 'Audífonos bluetooth con micrófono', 'SON-BT500', 'Sonica', 'BT-500', 'Tecnología', NULL, 1, 59.99, '2026-08-10 09:00:00', '2026-08-10 09:00:00'),
-- Hogar y Cocina
(6, 'HOG-001', 'Cafetera espresso 19 bares', 'BRE-ESP19', 'BrewMaster', 'ESP-19', 'Hogar', 'Incluye vaporizador de leche.', 2, 149.99, '2026-08-10 09:00:00', '2026-08-10 09:00:00'),
(7, 'HOG-002', 'Juego de sartenes antiadherentes 5 pzs', 'COC-NS5', 'Cocina+', 'NS-5', 'Hogar', NULL, 2, 89.99, '2026-08-10 09:00:00', '2026-08-10 09:00:00'),
(8, 'HOG-003', 'Aspiradora robot con mapeo láser', 'ROB-VAC-LIDAR', 'RoboHome', 'Vac-Lidar', 'Hogar', NULL, 2, 259.00, '2026-08-10 09:00:00', '2026-08-10 09:00:00'),
(9, 'HOG-004', 'Lámpara LED de escritorio regulable', 'LUM-DESK-DIM', 'Lumina', 'Desk-Dim', 'Hogar', NULL, 2, 34.50, '2026-08-10 09:00:00', '2026-08-10 09:00:00'),
(10, 'HOG-005', 'Organizador de cocina giratorio', 'COC-ORG-360', 'Cocina+', 'ORG-360', 'Hogar', NULL, 2, 24.99, '2026-08-10 09:00:00', '2026-08-10 09:00:00'),
-- Oficina y Papelería
(11, 'OFI-001', 'Silla ergonómica con soporte lumbar', 'ERG-CHAIR-LX', 'ErgoSit', 'LX-200', 'Oficina', NULL, 3, 199.99, '2026-08-10 09:00:00', '2026-08-10 09:00:00'),
(12, 'OFI-002', 'Escritorio ajustable eléctrico', 'ERG-DESK-E', 'ErgoSit', 'Desk-E', 'Oficina', 'Altura 70-120 cm, 2 motores.', 3, 329.00, '2026-08-10 09:00:00', '2026-08-10 09:00:00'),
(13, 'OFI-003', 'Papel bond A4 75 g (caja 5 millares)', 'PAP-A4-5K', 'PaperMax', 'A4-75', 'Oficina', NULL, 3, 42.00, '2026-08-10 09:00:00', '2026-08-10 09:00:00'),
(14, 'OFI-004', 'Set bolígrafos de gel 12 pzs', 'ESCR-GEL12', 'Escribo', 'Gel-12', 'Oficina', NULL, 3, 12.99, '2026-08-10 09:00:00', '2026-08-10 09:00:00'),
(15, 'OFI-005', 'Archivero metálico 4 gavetas', 'MUE-FILE4', 'MetalOffice', 'File-4', 'Oficina', NULL, 3, 139.50, '2026-08-10 09:00:00', '2026-08-10 09:00:00');

-- Verificación: debe mostrar 3 categorías y 15 artículos.
SELECT 'categorias' AS tabla, COUNT(*) AS total FROM Categorias
UNION ALL
SELECT 'articulos', COUNT(*) FROM Articulos;
