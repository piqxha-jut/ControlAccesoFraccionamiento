using System;
using System.Collections.Generic;

namespace ControlAccesoFraccionamiento.Models;

public partial class RegistrosAcceso
{
    public int Id { get; set; }

    public int? VehiculoId { get; set; }

    public int? VisitanteId { get; set; }

    public int? ResidenteId { get; set; }

    public int? GuardiaId { get; set; }

    public int? ResidenteDestinoId { get; set; }

    public string? TipoAcceso { get; set; }

    public DateTime? FechaEntrada { get; set; }

    public DateTime? FechaSalida { get; set; }

    public string? MotivoVisita { get; set; }

    public string? EstadoAutorizacion { get; set; }

    public string? EstadoAcceso { get; set; }

    public string? Notas { get; set; }

    public int? TiempoEstancia { get; set; }

    public virtual Usuario? Guardia { get; set; }

    public virtual ICollection<Incidente> Incidentes { get; set; } = new List<Incidente>();

    public virtual ICollection<Notificacione> Notificaciones { get; set; } = new List<Notificacione>();

    public virtual Residente? Residente { get; set; }

    public virtual Residente? ResidenteDestino { get; set; }

    public virtual Vehiculo? Vehiculo { get; set; }

    public virtual Visitante? Visitante { get; set; }
}
