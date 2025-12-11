using System;
using System.Collections.Generic;

namespace ControlAccesoFraccionamiento.Models;

public partial class VehiculosPreaprobado
{
    public int Id { get; set; }

    public int ResidenteId { get; set; }

    public DateOnly? FechaInicio { get; set; }

    public DateOnly? FechaFin { get; set; }

    public bool? Activo { get; set; }

    public int VehiculoId { get; set; }

    public string? Notas { get; set; }

    public virtual Residente Residente { get; set; } = null!;

    public virtual Vehiculo Vehiculo { get; set; } = null!;
}
