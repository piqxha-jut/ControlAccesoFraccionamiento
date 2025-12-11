using System;
using System.Collections.Generic;

namespace ControlAccesoFraccionamiento.Models;

public partial class VehiculosVisitante
{
    public int Id { get; set; }

    public int VisitanteId { get; set; }

    public int VehiculoId { get; set; }

    public virtual Vehiculo Vehiculo { get; set; } = null!;

    public virtual Visitante Visitante { get; set; } = null!;
}
