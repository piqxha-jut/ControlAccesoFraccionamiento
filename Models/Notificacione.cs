using System;
using System.Collections.Generic;

namespace ControlAccesoFraccionamiento.Models;

public partial class Notificacione
{
    public int Id { get; set; }

    public int? ResidenteId { get; set; }

    public int? RegistroAccesoId { get; set; }

    public string? Estado { get; set; }

    public string? RespuestaResidente { get; set; }

    public DateTime? FechaEnvio { get; set; }

    public DateTime? FechaRespuesta { get; set; }

    public virtual RegistrosAcceso? RegistroAcceso { get; set; }

    public virtual Residente? Residente { get; set; }
}
