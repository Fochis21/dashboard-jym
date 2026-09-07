using System.ComponentModel.DataAnnotations;

namespace DashboardJym.Models.ViewModels;

public class RegistroViewModel
{
    [Required, MaxLength(100)]
    public string Nombres { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Apellidos { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Dni { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Telefono { get; set; }

    [Required, EmailAddress, MaxLength(150)]
    public string Correo { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;

    // Código compartido solo con el equipo del estudio; evita que cualquier
    // persona en internet pueda crear su propia cuenta desde /auth/registro.
    [Required]
    public string CodigoInvitacion { get; set; } = string.Empty;

    // Solo define el rol elegido en el registro; ABOGADO y ASESOR_LEGAL
    // no tienen ninguna diferencia de permisos en el sistema.
    [Required]
    public string Rol { get; set; } = string.Empty;
}
