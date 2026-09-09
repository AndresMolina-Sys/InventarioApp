using System.ComponentModel.DataAnnotations;

namespace InventarioAppDesktop.Models;

public class Usuario
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [StringLength(20)]
    public string Rol { get; set; } = "usuario";

    public bool DebeCambiarPassword { get; set; }
}
