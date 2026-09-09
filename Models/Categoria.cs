using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventarioAppDesktop.Models;

public class Categoria
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    public ICollection<Articulo> Articulos { get; set; } = new List<Articulo>();

    // Contador calculado por consulta agregada (COUNT). Evita cargar la colección
    // completa de artículos en memoria solo para mostrar un número.
    [NotMapped]
    public int CantidadArticulos { get; set; }
}
