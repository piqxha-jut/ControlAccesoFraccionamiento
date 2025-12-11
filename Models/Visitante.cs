using System;
using System.Collections.Generic;

namespace ControlAccesoFraccionamiento.Models;

public partial class Visitante
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public DateTime? FechaCreacion { get; set; }

    public virtual ICollection<RegistrosAcceso> RegistrosAccesos { get; set; } = new List<RegistrosAcceso>();

    public virtual ICollection<VehiculosVisitante> VehiculosVisitantes { get; set; } = new List<VehiculosVisitante>();
}
