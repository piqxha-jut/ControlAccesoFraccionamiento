using System;
using System.Collections.Generic;

namespace ControlAccesoFraccionamiento.Models;

public partial class Usuario
{
    public int Id { get; set; }

    public string Rol { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string? Email { get; set; }

    public string? Contrasena { get; set; }

    public string? Telefono { get; set; }

    public DateTime? UltimoAcceso { get; set; }

    public DateTime? FechaCreacion { get; set; }

    public virtual ICollection<Incidente> Incidentes { get; set; } = new List<Incidente>();

    public virtual ICollection<RegistrosAcceso> RegistrosAccesos { get; set; } = new List<RegistrosAcceso>();

    public virtual Residente? Residente { get; set; }

    public virtual ICollection<VehiculosListaNegra> VehiculosListaNegras { get; set; } = new List<VehiculosListaNegra>();
}
