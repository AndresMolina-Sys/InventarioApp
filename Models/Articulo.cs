using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace InventarioAppDesktop.Models;

public class Articulo
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Codigo { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Sku { get; set; }

    [StringLength(100)]
    public string? Marca { get; set; }

    [StringLength(100)]
    public string? Modelo { get; set; }

    [Required]
    [StringLength(100)]
    public string Departamento { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Notas { get; set; }

    [Required]
    public int CategoriaId { get; set; }

    [ForeignKey("CategoriaId")]
    public Categoria? Categoria { get; set; }

    // Precio en dólares. Opcional; decimal para poderse sumar a futuro
    // (valor total del inventario) sin errores de redondeo.
    [Precision(18, 2)]
    public decimal? Precio { get; set; }

    public DateTime FechaModificacion { get; set; } = DateTime.Now;

    public DateTime FechaIngreso { get; set; } = DateTime.Now;
}
