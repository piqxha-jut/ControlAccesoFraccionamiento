using System;
using System.Collections.Generic;

namespace ControlAccesoFraccionamiento.Models;

public partial class Vehiculo
{
    public int Id { get; set; }

    public string Placa { get; set; } = null!;

    public string? Marca { get; set; }

    public string? Modelo { get; set; }

    public string? Color { get; set; }

    public string? Tipo { get; set; }

    public DateTime? FechaCreacion { get; set; }

    public virtual ICollection<RegistrosAcceso> RegistrosAccesos { get; set; } = new List<RegistrosAcceso>();

    public virtual ICollection<VehiculosListaNegra> VehiculosListaNegras { get; set; } = new List<VehiculosListaNegra>();

    public virtual ICollection<VehiculosPreaprobado> VehiculosPreaprobados { get; set; } = new List<VehiculosPreaprobado>();

    public virtual ICollection<VehiculosResidente> VehiculosResidentes { get; set; } = new List<VehiculosResidente>();

    public virtual ICollection<VehiculosVisitante> VehiculosVisitantes { get; set; } = new List<VehiculosVisitante>();
}
