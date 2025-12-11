using System;
using System.Collections.Generic;

namespace ControlAccesoFraccionamiento.Models;

public partial class VehiculosResidente
{
    public int Id { get; set; }

    public int ResidenteId { get; set; }

    public int VehiculoId { get; set; }

    public string? Alias { get; set; }

    public virtual Residente Residente { get; set; } = null!;

    public virtual Vehiculo Vehiculo { get; set; } = null!;
}
