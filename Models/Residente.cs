using System;
using System.Collections.Generic;

namespace ControlAccesoFraccionamiento.Models;

public partial class Residente
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public string? Unidad { get; set; }

    public string? Direccion { get; set; }

    public bool? Activo { get; set; }

    public DateTime? FechaCreacion { get; set; }

    public virtual ICollection<Notificacione> Notificaciones { get; set; } = new List<Notificacione>();

    public virtual ICollection<RegistrosAcceso> RegistrosAccesoResidenteDestinos { get; set; } = new List<RegistrosAcceso>();

    public virtual ICollection<RegistrosAcceso> RegistrosAccesoResidentes { get; set; } = new List<RegistrosAcceso>();

    public virtual Usuario Usuario { get; set; } = null!;

    public virtual ICollection<VehiculosPreaprobado> VehiculosPreaprobados { get; set; } = new List<VehiculosPreaprobado>();

    public virtual ICollection<VehiculosResidente> VehiculosResidentes { get; set; } = new List<VehiculosResidente>();
}
