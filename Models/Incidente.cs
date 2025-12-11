using System;
using System.Collections.Generic;

namespace ControlAccesoFraccionamiento.Models;

public partial class Incidente
{
    public int Id { get; set; }

    public int? RegistroAccesoId { get; set; }

    public int? ReportadoPor { get; set; }

    public string? TipoIncidente { get; set; }

    public string? Descripcion { get; set; }

    public DateTime? FechaCreacion { get; set; }

    public virtual RegistrosAcceso? RegistroAcceso { get; set; }

    public virtual Usuario? ReportadoPorNavigation { get; set; }
}
