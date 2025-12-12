using ControlAccesoFraccionamiento.Data;
using ControlAccesoFraccionamiento.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ControlAccesoFraccionamiento.Controllers
{
    public class ResidenteController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ResidenteController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        //  VERIFICAR SESIÓN Y ROL
        // =========================================================
        private bool EsResidenteAutenticado()
        {
            var userId = HttpContext.Session.GetString("UserId");
            var userRol = HttpContext.Session.GetString("UserRol");
            return !string.IsNullOrEmpty(userId) && userRol == "residente";
        }

        private int ObtenerResidenteId()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (int.TryParse(userId, out int id))
            {
                var residente = _context.Residentes.FirstOrDefault(r => r.UsuarioId == id);
                return residente?.Id ?? 0;
            }
            return 0;
        }

        // =========================================================
        //  DASHBOARD DEL RESIDENTE
        // =========================================================
        public IActionResult Index()
        {
            if (!EsResidenteAutenticado())
                return RedirectToAction("Login", "Auth");

            var residenteId = ObtenerResidenteId();

            // Obtener datos del residente actual
            var residente = _context.Residentes
                .Include(r => r.Usuario)
                .Include(r => r.VehiculosResidentes)
                    .ThenInclude(vr => vr.Vehiculo)
                .FirstOrDefault(r => r.Id == residenteId);

            if (residente == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Auth");
            }

            // Contar notificaciones pendientes
            ViewBag.NotificacionesPendientes = _context.Notificaciones
                .Count(n => n.ResidenteId == residenteId && n.Estado == "enviado");

            HttpContext.Session.SetInt32("NotificacionesPendientes", (int)ViewBag.NotificacionesPendientes);

            // =========================================================
            // NUEVO: OBTENER TODOS LOS ACCESOS DEL RESIDENTE
            // =========================================================

            // 1. Obtener IDs de vehículos del residente
            var vehiculosDelResidente = _context.VehiculosResidentes
                .Where(vr => vr.ResidenteId == residenteId)
                .Select(vr => vr.VehiculoId)
                .ToList();

            // 2. Obtener TODOS los accesos relevantes:
            // - Visitantes aprobados que van al residente
            // - Entradas del propio residente
            ViewBag.AccesosRecientes = _context.RegistrosAccesos
                .Include(r => r.Vehiculo)
                .Include(r => r.Visitante)
                .Include(r => r.ResidenteDestino)
                .Where(r =>
                    // Caso 1: Visitantes que van al residente (aprobados)
                    (r.ResidenteDestinoId == residenteId &&
                     r.TipoAcceso == "visitante" &&
                     r.EstadoAutorizacion == "aprobado") ||
                    // Caso 2: Entradas del residente (sus propios vehículos)
                    (vehiculosDelResidente.Contains(r.VehiculoId.Value) &&  // ← AQUÍ: .Value
                     r.TipoAcceso == "residente" &&
                     r.EstadoAutorizacion == "aprobado")
                )
                .OrderByDescending(r => r.FechaEntrada)
                .Take(8)  // Un poco más que antes
                .ToList();

            return View(residente);
        }

        // =========================================================
        //  NOTIFICACIONES / SOLICITUDES DE ACCESO
        // =========================================================
        public IActionResult Notificaciones()
        {
            if (!EsResidenteAutenticado())
                return RedirectToAction("Login", "Auth");

            var residenteId = ObtenerResidenteId();
            if (residenteId == 0)
                return RedirectToAction("Login", "Auth");

            // Obtener TODAS las notificaciones del residente
            var todasNotificaciones = _context.Notificaciones
                .Include(n => n.RegistroAcceso)
                    .ThenInclude(r => r.Vehiculo)
                .Include(n => n.RegistroAcceso)
                    .ThenInclude(r => r.Visitante)
                .Include(n => n.RegistroAcceso)
                    .ThenInclude(r => r.ResidenteDestino)
                .Where(n => n.ResidenteId == residenteId)
                .OrderByDescending(n => n.FechaEnvio)
                .ToList();

            // Separar en pendientes y respondidas
            var pendientes = todasNotificaciones.Where(n => n.Estado == "enviado").ToList();
            var respondidas = todasNotificaciones.Where(n => n.Estado == "respondido")
                                                 .OrderByDescending(n => n.FechaRespuesta)
                                                 .Take(10)
                                                 .ToList();

            // Pasar a ViewBag para la vista
            ViewBag.Pendientes = pendientes;
            ViewBag.Respondidas = respondidas;
            // Actualizar la sesión con el número de pendientes
            HttpContext.Session.SetInt32("NotificacionesPendientes", pendientes.Count);
            // Mantener compatibilidad con la vista existente
            // Enviar solo las pendientes como modelo
            return View(pendientes);
        }

        // =========================================================
        //  AUTORIZAR O RECHAZAR VISITA
        // =========================================================
        [HttpPost]
        public IActionResult ResponderNotificacion(int notificacionId, bool autorizar)
        {
            if (!EsResidenteAutenticado())
                return RedirectToAction("Login", "Auth");

            var residenteId = ObtenerResidenteId();
            if (residenteId == 0)
                return RedirectToAction("Login", "Auth");

            try
            {
                var notificacion = _context.Notificaciones
                    .Include(n => n.RegistroAcceso)
                        .ThenInclude(r => r.Vehiculo)
                    .Include(n => n.RegistroAcceso)
                        .ThenInclude(r => r.Visitante)
                    .FirstOrDefault(n => n.Id == notificacionId);

                // Validaciones existentes...
                if (notificacion == null)
                {
                    TempData["Error"] = "Notificación no encontrada.";
                    return RedirectToAction("Notificaciones");
                }

                if (notificacion.ResidenteId != residenteId)
                {
                    TempData["Error"] = "No puedes responder solicitudes de otro residente.";
                    return RedirectToAction("Notificaciones");
                }

                if (notificacion.Estado != "enviado")
                {
                    TempData["Error"] = "Esta solicitud ya fue procesada.";
                    return RedirectToAction("Notificaciones");
                }

                // Actualizar notificación
                notificacion.Estado = "respondido";
                notificacion.RespuestaResidente = autorizar ? "aprobado" : "rechazado";
                notificacion.FechaRespuesta = DateTime.Now;

                // Actualizar registro de acceso
                if (notificacion.RegistroAcceso != null)
                {
                    var registro = notificacion.RegistroAcceso;
                    registro.EstadoAutorizacion = autorizar ? "aprobado" : "rechazado";

                    if (autorizar)
                    {
                        // ✅ NUEVO: REGISTRAR ENTRADA CUANDO SE APRUEBA
                        registro.EstadoAcceso = "dentro";  // ← IMPORTANTE
                        registro.FechaEntrada = DateTime.Now;  // ← Registrar hora real de entrada

                        TempData["Mensaje"] = "✅ Visita autorizada. El visitante puede ingresar.";
                    }
                    else
                    {
                        // Rechazo (existente)
                        registro.EstadoAcceso = "denegado";

                        // Registrar incidente por rechazo
                        var incidente = new Incidente
                        {
                            RegistroAccesoId = registro.Id,
                            ReportadoPor = residenteId,
                            TipoIncidente = "Visita rechazada por residente",
                            Descripcion = $"El residente rechazó la visita de {registro.Visitante?.Nombre}",
                            FechaCreacion = DateTime.Now
                        };
                        _context.Incidentes.Add(incidente);

                        TempData["Mensaje"] = "❌ Visita rechazada. Se notificará al guardia.";
                    }
                }

                _context.SaveChanges();
                return RedirectToAction("Notificaciones");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al procesar: " + ex.Message;
                return RedirectToAction("Notificaciones");
            }
        }

        // =========================================================
        //  HISTORIAL DE VISITAS
        // =========================================================
        public IActionResult Historial()
        {
            if (!EsResidenteAutenticado())
                return RedirectToAction("Login", "Auth");

            var residenteId = ObtenerResidenteId();
            if (residenteId == 0)
                return RedirectToAction("Login", "Auth");

            var historial = _context.RegistrosAccesos
                .Include(r => r.Vehiculo)
                .Include(r => r.Visitante)
                .Where(r => r.ResidenteDestinoId == residenteId &&
                       r.TipoAcceso == "visitante")  // ← ¡AGREGA ESTE FILTRO!
                .OrderByDescending(r => r.FechaEntrada)
                .Take(50)
                .ToList();

            return View(historial);
        }
        // =========================================================
        //  VEHÍCULOS PREAPROBADOS (ACTUALIZADO)
        // =========================================================
        public IActionResult Preaprobados()
        {
            if (!EsResidenteAutenticado())
                return RedirectToAction("Login", "Auth");

            var residenteId = ObtenerResidenteId();
            if (residenteId == 0)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Auth");
            }

            var hoy = DateOnly.FromDateTime(DateTime.Now);

            // Obtener vehículos preaprobados para este residente
            var preaprobados = _context.VehiculosPreaprobados
                .Include(p => p.Vehiculo)
                .Where(p => p.ResidenteId == residenteId)
                .OrderBy(p => p.Vehiculo.Placa)
                .ToList();

            // Agregar información de vigencia
            ViewBag.Hoy = hoy;

            return View(preaprobados);
        }

        // =========================================================
        //  MIS VEHÍCULOS
        // =========================================================
        public IActionResult Vehiculos()
        {
            if (!EsResidenteAutenticado())
                return RedirectToAction("Login", "Auth");

            var residenteId = ObtenerResidenteId();
            if (residenteId == 0)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Auth");
            }

            var vehiculos = _context.VehiculosResidentes
                .Include(vr => vr.Vehiculo)
                    .ThenInclude(v => v.VehiculosListaNegras)
                .Include(vr => vr.Vehiculo)
                    .ThenInclude(v => v.RegistrosAccesos)  // ← ¡IMPORTANTE! Incluir registros
                        .ThenInclude(ra => ra.ResidenteDestino)
                .Where(vr => vr.ResidenteId == residenteId)
                .ToList();

            return View(vehiculos);
        }
    }
}