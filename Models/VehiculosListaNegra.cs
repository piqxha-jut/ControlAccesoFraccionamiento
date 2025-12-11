using System;
using System.Collections.Generic;

namespace ControlAccesoFraccionamiento.Models;

public partial class VehiculosListaNegra
{
    public int Id { get; set; }

    public int VehiculoId { get; set; }

    public string? Razon { get; set; }

    public int? AgregadoPor { get; set; }

    public DateTime? FechaAgregado { get; set; }

    public virtual Usuario? AgregadoPorNavigation { get; set; }

    public virtual Vehiculo Vehiculo { get; set; } = null!;
}
