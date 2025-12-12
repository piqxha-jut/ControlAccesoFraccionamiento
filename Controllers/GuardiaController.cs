using ControlAccesoFraccionamiento.Data;
using ControlAccesoFraccionamiento.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControlAccesoFraccionamiento.Controllers
{
    public class GuardiaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GuardiaController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        //  VERIFICAR SESIÓN Y ROL
        // =========================================================
        private bool EsGuardiaAutenticado()
        {
            var userId = HttpContext.Session.GetString("UserId");
            var userRol = HttpContext.Session.GetString("UserRol");
            return !string.IsNullOrEmpty(userId) && userRol == "guardia";
        }

        // ---------------------------------------------------------
        // PANEL PRINCIPAL DEL GUARDIA
        // ---------------------------------------------------------
        public IActionResult Index()
        {
            if (!EsGuardiaAutenticado())
            {
                TempData["Error"] = "Acceso no autorizado";
                return RedirectToAction("Login", "Auth");
            }
            ViewBag.Nombre = HttpContext.Session.GetString("Nombre");
            return View();
        }

        // ---------------------------------------------------------
        // REGISTRAR ENTRADA - GET (MODIFICADO)
        // ---------------------------------------------------------
        public IActionResult RegistrarEntrada()
        {
            if (!EsGuardiaAutenticado())
            {
                TempData["Error"] = "Acceso no autorizado";
                return RedirectToAction("Login", "Auth");
            }

            // NO BORRAR TempData AQUÍ
            // NO USAR Origen
            // NO USAR Keep
            // TempData se borrará solo después de mostrarse

            var unidades = _context.Residentes
                .Where(r => r.Activo == true)
                .Select(r => r.Unidad)
                .OrderBy(u => u)
                .ToList();

            ViewBag.Unidades = unidades;

            return View();
        }





        // ---------------------------------------------------------
        // REGISTRAR ENTRADA - POST (COMPLETAMENTE CORREGIDO)
        // ---------------------------------------------------------
        [HttpPost]
        public IActionResult RegistrarEntrada(
    string placa,
    string nombreVisitante,
    string unidadDestino,
    string motivoVisita)
        {
            if (!EsGuardiaAutenticado())
            {
                TempData["Error"] = "Acceso no autorizado";
                return RedirectToAction("Login", "Auth");
            }

            if (string.IsNullOrWhiteSpace(placa) ||
                string.IsNullOrWhiteSpace(unidadDestino))
            {
                TempData["Error"] = "Debes ingresar la placa y la unidad destino.";
                return RedirectToAction("RegistrarEntrada");
            }

            placa = placa.Trim().ToUpper();

            try
            {
                // 1. Validar si ya está dentro
                var vehiculoDentro = _context.RegistrosAccesos
                    .Include(r => r.Vehiculo)
                    .Include(r => r.ResidenteDestino)
                    .FirstOrDefault(r =>
                        r.Vehiculo.Placa == placa &&
                        r.EstadoAcceso == "dentro" &&
                        r.EstadoAutorizacion == "aprobado");

                if (vehiculoDentro != null)
                {
                    TempData["Error"] =
                        $"❌ El vehículo {placa} YA ESTÁ DENTRO del fraccionamiento. " +
                        $"Ingresó el {vehiculoDentro.FechaEntrada:dd/MM/yyyy HH:mm} " +
                        $"a la unidad {vehiculoDentro.ResidenteDestino?.Unidad ?? "Desconocida"}. " +
                        $"Debe registrar su SALIDA primero.";
                    return RedirectToAction("RegistrarEntrada");
                }


                // ***************************************************
                // 1.5 VALIDAR SI YA EXISTE UNA SOLICITUD PENDIENTE
                // ***************************************************
                var accesoPendiente = _context.RegistrosAccesos
                    .Include(r => r.Vehiculo)
                    .FirstOrDefault(r =>
                        r.Vehiculo.Placa == placa &&
                        r.EstadoAutorizacion == "pendiente" &&
                        r.EstadoAcceso == "denegado");

                if (accesoPendiente != null)
                {
                    TempData["Error"] =
                        $"⚠ Ya existe una solicitud PENDIENTE para esta placa ({placa}). " +
                        $"El residente aún no responde. No puedes registrar otra entrada hasta que haya una respuesta.";

                    return RedirectToAction("RegistrarEntrada");
                }


                // ***************************************************
                // OPCIONAL PERO MUY RECOMENDADO:
                // VALIDAR NOTIFICACIÓN 'ENVIADO' SIN RESPONDER
                // ***************************************************
                var notificacionPendiente = _context.Notificaciones
                    .Include(n => n.RegistroAcceso)
                    .ThenInclude(r => r.Vehiculo)
                    .FirstOrDefault(n =>
                        n.RegistroAcceso.Vehiculo.Placa == placa &&
                        n.Estado == "enviado");

                if (notificacionPendiente != null)
                {
                    TempData["Error"] =
                        $"⚠ Ya se envió una solicitud al residente para la placa {placa}. " +
                        $"Aún está PENDIENTE de respuesta.";

                    return RedirectToAction("RegistrarEntrada");
                }

                // 2. Buscar o crear vehículo
                var vehiculo = _context.Vehiculos.FirstOrDefault(v => v.Placa == placa);

                if (vehiculo == null)
                {
                    vehiculo = new Vehiculo
                    {
                        Placa = placa,
                        Tipo = "visitante",
                        FechaCreacion = DateTime.Now
                    };
                    _context.Vehiculos.Add(vehiculo);
                    _context.SaveChanges();
                }

                // 3. Lista negra
                var listaNegra = _context.VehiculosListaNegras
                    .FirstOrDefault(v => v.VehiculoId == vehiculo.Id);

                if (listaNegra != null)
                {
                    TempData["Alerta"] =
                        $"⚠ VEHÍCULO EN LISTA NEGRA. Razón: {listaNegra.Razon}";
                    return RedirectToAction("ListaNegraDetectada");
                }

                // 4. Validar residente destino
                var residenteDestino = _context.Residentes
                    .FirstOrDefault(r => r.Unidad == unidadDestino);

                if (residenteDestino == null)
                {
                    TempData["Error"] = "La unidad destino no existe.";
                    return RedirectToAction("RegistrarEntrada");
                }

                if (residenteDestino.Activo != true)
                {
                    TempData["Error"] = "❌ El residente destino está inactivo.";
                    return RedirectToAction("RegistrarEntrada");
                }

                // 5. Vehículo de residente
                var vehiculoResidente = _context.VehiculosResidentes
                    .FirstOrDefault(v => v.VehiculoId == vehiculo.Id);

                if (vehiculoResidente != null)
                {
                    // Obtener el residente dueño del vehículo
                    var residentePropietario = _context.Residentes
                        .FirstOrDefault(r => r.Id == vehiculoResidente.ResidenteId);

                    if (residentePropietario == null)
                    {
                        TempData["Error"] = "❌ No se encontró información del residente propietario.";
                        return RedirectToAction("RegistrarEntrada");
                    }

                    // Verificar que el residente esté activo
                    if (residentePropietario.Activo != true)
                    {
                        TempData["Error"] = $"❌ El residente {residentePropietario.Unidad} está INACTIVO.";
                        return RedirectToAction("RegistrarEntrada");
                    }

                    // IMPORTANTE: Para residentes, ResidenteId y ResidenteDestinoId deben ser el MISMO
                    // No pueden "visitar" a otra unidad
                    var registro = new RegistrosAcceso
                    {
                        VehiculoId = vehiculo.Id,
                        TipoAcceso = "residente",
                        GuardiaId = ObtenerGuardiaId(),
                        ResidenteId = residentePropietario.Id,
                        ResidenteDestinoId = residentePropietario.Id,  // ¡MISMO QUE ResidenteId!
                        MotivoVisita = "Entrada de residente",  // ← IGNORA COMPLETAMENTE lo que puso el guardia,
                        EstadoAutorizacion = "aprobado",
                        EstadoAcceso = "dentro",
                        FechaEntrada = DateTime.Now
                    };

                    _context.RegistrosAccesos.Add(registro);
                    _context.SaveChanges();

                    TempData["Mensaje"] = $"✅ Entrada de RESIDENTE registrada. Unidad: {residentePropietario.Unidad}";
                    return RedirectToAction("Index");
                }

                // 6. Visitante
                Visitante visitante = null;

                var vehiculoVisitante = _context.VehiculosVisitantes
                    .Include(v => v.Visitante)
                    .FirstOrDefault(v => v.VehiculoId == vehiculo.Id);

                if (vehiculoVisitante != null)
                {
                    visitante = vehiculoVisitante.Visitante;
                }
                else if (!string.IsNullOrWhiteSpace(nombreVisitante))
                {
                    visitante = new Visitante
                    {
                        Nombre = nombreVisitante,
                        FechaCreacion = DateTime.Now
                    };

                    _context.Visitantes.Add(visitante);
                    _context.SaveChanges();

                    _context.VehiculosVisitantes.Add(new VehiculosVisitante
                    {
                        VehiculoId = vehiculo.Id,
                        VisitanteId = visitante.Id
                    });

                    _context.SaveChanges();
                }

                // 7. Preaprobado
                DateOnly hoy = DateOnly.FromDateTime(DateTime.Now);

                bool vehiculoPreaprobado = _context.VehiculosPreaprobados.Any(p =>
                    p.VehiculoId == vehiculo.Id &&
                    p.ResidenteId == residenteDestino.Id &&
                    p.Activo == true &&
                    p.FechaInicio <= hoy &&
                    p.FechaFin >= hoy);

                // 8. Crear registro
                var registroVisita = new RegistrosAcceso
                {
                    VehiculoId = vehiculo.Id,
                    TipoAcceso = "visitante",
                    GuardiaId = ObtenerGuardiaId(),
                    ResidenteDestinoId = residenteDestino.Id,
                    VisitanteId = visitante?.Id,
                    MotivoVisita = motivoVisita ?? "Visita",
                    EstadoAutorizacion = vehiculoPreaprobado ? "aprobado" : "pendiente",
                    EstadoAcceso = vehiculoPreaprobado ? "dentro" : "denegado",
                    FechaEntrada = DateTime.Now
                };

                _context.RegistrosAccesos.Add(registroVisita);
                _context.SaveChanges();

                // 9. Notificar si no está preaprobado
                if (!vehiculoPreaprobado && visitante != null)
                {
                    _context.Notificaciones.Add(new Notificacione
                    {
                        ResidenteId = residenteDestino.Id,
                        RegistroAccesoId = registroVisita.Id,
                        Estado = "enviado",
                        FechaEnvio = DateTime.Now
                    });

                    _context.SaveChanges();
                }

                TempData["Mensaje"] = vehiculoPreaprobado
                    ? "✅ Entrada AUTOMÁTICA - Vehículo preaprobado"
                    : "⚠ Vehículo registrado y marcado como FUERA. Esperando autorización.";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al registrar entrada: {ex.Message}";
                return RedirectToAction("RegistrarEntrada");
            }
        }



        // -------------------- VISITANTE SIN REGISTROS --------------------
        private IActionResult ProcesarVisitanteNormal(
            Vehiculo vehiculo,
            string nombreVisitante,
            string unidadDestino,
            string motivoVisita)
        {
            var residenteDestino = _context.Residentes
                .FirstOrDefault(r => r.Unidad == unidadDestino);

            if (residenteDestino == null)
            {
                TempData["Mensaje"] = "La unidad destino no existe.";
                return RedirectToAction("RegistrarEntrada");
            }

            Visitante visitante = null;
            if (!string.IsNullOrWhiteSpace(nombreVisitante))
            {
                visitante = new Visitante
                {
                    Nombre = nombreVisitante,
                };

                _context.Visitantes.Add(visitante);
                _context.SaveChanges();

                _context.VehiculosVisitantes.Add(new VehiculosVisitante
                {
                    VehiculoId = vehiculo.Id,
                    VisitanteId = visitante.Id
                });

                _context.SaveChanges();
            }

            var registro = new RegistrosAcceso
            {
                VehiculoId = vehiculo.Id,
                TipoAcceso = "visitante",
                GuardiaId = ObtenerGuardiaId(),
                ResidenteDestinoId = residenteDestino.Id,
                VisitanteId = visitante?.Id,
                MotivoVisita = motivoVisita,
                EstadoAutorizacion = "pendiente",
                EstadoAcceso = "denegado",
                FechaEntrada = DateTime.Now
            };

            _context.RegistrosAccesos.Add(registro);
            _context.SaveChanges();

            _context.Notificaciones.Add(new Notificacione
            {
                ResidenteId = residenteDestino.Id,
                RegistroAccesoId = registro.Id,
                Estado = "enviado",
                FechaEnvio = DateTime.Now
            });

            _context.SaveChanges();

            TempData["Mensaje"] =
                "Visita registrada y marcada como PENDIENTE de autorización del residente.";
            return RedirectToAction("Index");
        }

        // ---------------------------------------------------------
        // LISTA NEGRA VIEW
        // ---------------------------------------------------------
        public IActionResult ListaNegraDetectada()
        {
            if (!EsGuardiaAutenticado())
            {
                TempData["Error"] = "Acceso no autorizado";
                return RedirectToAction("Login", "Auth");
            }
            return View();
        }

        // ---------------------------------------------------------
        // REGISTRAR SALIDA
        // ---------------------------------------------------------
        public IActionResult RegistrarSalida()
        {
            if (!EsGuardiaAutenticado())
            {
                TempData["Error"] = "Acceso no autorizado";
                return RedirectToAction("Login", "Auth");
            }

            return View();
        }


        [HttpPost]
        public IActionResult RegistrarSalida(string placa)
        {
            if (!EsGuardiaAutenticado())
            {
                TempData["Error"] = "Acceso no autorizado";
                return RedirectToAction("Login", "Auth");
            }

            placa = placa.Trim().ToUpper();

            // ---------------------------------------------------------
            // 1. Obtener TODOS los registros de la placa que están "dentro"
            // ---------------------------------------------------------
            var registrosDentro = _context.RegistrosAccesos
                .Include(r => r.Vehiculo)
                .Include(r => r.ResidenteDestino)
                .Where(r =>
                    r.Vehiculo.Placa == placa &&
                    r.EstadoAcceso == "dentro")
                .ToList();

            // ---------------------------------------------------------
            // 2. NO EXISTE NINGÚN REGISTRO "DENTRO"
            // ---------------------------------------------------------
            if (registrosDentro.Count == 0)
            {
                bool vehiculoExiste = _context.Vehiculos.Any(v => v.Placa == placa);

                TempData["Error"] = vehiculoExiste
                    ? $"❌ El vehículo {placa} NO ESTÁ DENTRO del fraccionamiento. No se puede registrar salida."
                    : $"❌ No hay registro de entrada para la placa {placa}.";

                return RedirectToAction("RegistrarSalida");
            }

            // ---------------------------------------------------------
            // 3. MÁS DE 1 REGISTRO DENTRO = ERROR DE CONSISTENCIA
            // ---------------------------------------------------------
            if (registrosDentro.Count > 1)
            {
                TempData["Error"] =
                    $"⚠ ERROR GRAVE: La placa {placa} tiene múltiples registros 'DENTRO'. " +
                    $"Esto requiere revisión manual del administrador.";

                return RedirectToAction("RegistrarSalida");
            }

            // Solo 1 registro dentro → OK
            var registro = registrosDentro.First();

            // ---------------------------------------------------------
            // 4. Verificar que la entrada haya sido APROBADA
            // ---------------------------------------------------------
            if (registro.EstadoAutorizacion != "aprobado")
            {
                // Pendiente → No ha sido autorizada aún
                if (registro.EstadoAutorizacion == "pendiente")
                {
                    TempData["Error"] =
                        $"⚠ El vehículo {placa} tiene una solicitud de entrada pendiente. " +
                        $"No puede registrar salida hasta que el residente responda.";

                    return RedirectToAction("RegistrarSalida");
                }

                // Rechazado → nunca entró realmente
                if (registro.EstadoAutorizacion == "rechazado")
                {
                    TempData["Error"] =
                        $"❌ El vehículo {placa} NO TIENE PERMITIDO EL ACCESO. " +
                        $"Su solicitud fue rechazada. No puede registrar salida.";

                    return RedirectToAction("RegistrarSalida");
                }
            }

            // ---------------------------------------------------------
            // 5. Registrar salida
            // ---------------------------------------------------------
            registro.FechaSalida = DateTime.Now;
            registro.EstadoAcceso = "salio";
            _context.SaveChanges();

            TempData["Mensaje"] =
                $"✅ Salida registrada para {placa}. Visitó la unidad {registro.ResidenteDestino?.Unidad ?? "Desconocida"}.";

            return RedirectToAction("Index");
        }


        // ---------------------------------------------------------
        // BUSCAR - VERSIÓN MEJORADA
        // ---------------------------------------------------------
        public IActionResult Buscar()
        {
            if (!EsGuardiaAutenticado())
            {
                TempData["Error"] = "Acceso no autorizado";
                return RedirectToAction("Login", "Auth");
            }
            return View();
        }

        [HttpPost]
        public IActionResult Buscar(string placa, string unidad)
        {
            if (!EsGuardiaAutenticado())
            {
                TempData["Error"] = "Acceso no autorizado";
                return RedirectToAction("Login", "Auth");
            }

            try
            {
                // ================================
                //     BÚSQUEDA POR PLACA
                // ================================
                if (!string.IsNullOrWhiteSpace(placa))
                {
                    placa = placa.Trim().ToUpper();

                    // Encontrar vehículo
                    var vehiculo = _context.Vehiculos.FirstOrDefault(v => v.Placa == placa);

                    if (vehiculo == null)
                    {
                        ViewBag.Error = "Vehículo no encontrado";
                        return View();
                    }

                    ViewBag.TipoBusqueda = "placa";
                    ViewBag.Placa = vehiculo.Placa;
                    ViewBag.Marca = vehiculo.Marca ?? "No especificada";
                    ViewBag.Modelo = vehiculo.Modelo ?? "No especificado";

                    // ¿Es residente?
                    ViewBag.EsResidente = _context.VehiculosResidentes
                        .Any(v => v.VehiculoId == vehiculo.Id);

                    // ¿Está en lista negra?
                    ViewBag.ListaNegra = _context.VehiculosListaNegras
                        .FirstOrDefault(v => v.VehiculoId == vehiculo.Id);


                    // ----------------------------------------------------
                    //   NUEVO: ¿Está el vehículo DENTRO actualmente?
                    // ----------------------------------------------------
                    var registroDentro = _context.RegistrosAccesos
                        .Include(r => r.ResidenteDestino)
                        .FirstOrDefault(r =>
                            r.VehiculoId == vehiculo.Id &&
                            r.EstadoAcceso == "dentro" &&
                            r.EstadoAutorizacion == "aprobado");

                    if (registroDentro != null)
                    {
                        ViewBag.EstadoVehiculo = "dentro";
                        ViewBag.InfoDentro = registroDentro;
                    }
                    else
                    {
                        // ----------------------------------------------------
                        //   NUEVO: ¿Tiene una solicitud PENDIENTE?
                        // ----------------------------------------------------
                        var registroPendiente = _context.RegistrosAccesos
                            .Include(r => r.ResidenteDestino)
                            .FirstOrDefault(r =>
                                r.VehiculoId == vehiculo.Id &&
                                r.EstadoAutorizacion == "pendiente");

                        if (registroPendiente != null)
                        {
                            ViewBag.EstadoVehiculo = "pendiente";
                            ViewBag.InfoPendiente = registroPendiente;
                        }
                        else
                        {
                            ViewBag.EstadoVehiculo = "fuera"; // No está dentro ni pendiente
                        }
                    }

                    return View();
                }

                // ================================
                //     BÚSQUEDA POR UNIDAD
                // ================================
                if (!string.IsNullOrWhiteSpace(unidad))
                {
                    var residente = _context.Residentes
                        .FirstOrDefault(r => r.Unidad == unidad);

                    if (residente == null)
                    {
                        ViewBag.Error = "Unidad no encontrada";
                        return View();
                    }

                    if (residente.Activo == false)
                    {
                        ViewBag.ResidenteInactivo = true;
                        ViewBag.Advertencia =
                            "⚠ El residente está INACTIVO. No se permite gestionar accesos para esta unidad.";
                    }

                    ViewBag.TipoBusqueda = "unidad";
                    ViewBag.Unidad = residente.Unidad;
                    ViewBag.Direccion = residente.Direccion;
                    ViewBag.Estado = residente.Activo == true ? "Activo" : "Inactivo";

                    var vehiculos = _context.VehiculosResidentes
                        .Where(v => v.ResidenteId == residente.Id)
                        .Select(v => v.Vehiculo.Placa)
                        .ToList();

                    ViewBag.Vehiculos = vehiculos;

                    return View();
                }

                ViewBag.Error = "Debes ingresar una placa o una unidad";
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error: " + ex.Message;
                return View();
            }
        }


        // ---------------------------------------------------------
        // PENDIENTES DE AUTORIZACIÓN - CON HISTORIAL RECIENTE
        // ---------------------------------------------------------
        public IActionResult Pendientes()
        {
            if (!EsGuardiaAutenticado())
            {
                TempData["Error"] = "Acceso no autorizado";
                return RedirectToAction("Login", "Auth");
            }

            try
            {
                // 1. Obtener solicitudes PENDIENTES (RegistrosAcceso con estado "pendiente")
                var pendientes = _context.RegistrosAccesos
                    .Include(r => r.Vehiculo)
                    .Include(r => r.Visitante)
                    .Include(r => r.ResidenteDestino)
                    .Where(r => r.EstadoAutorizacion == "pendiente")
                    .OrderByDescending(r => r.FechaEntrada)
                    .ToList();

                // 2. Obtener NOTIFICACIONES RESPONDIDAS en las últimas 24 horas
                var veinticuatroHorasAtras = DateTime.Now.AddHours(-24);

                var notificacionesRespondidas = _context.Notificaciones
                    .Include(n => n.Residente)  // Residente que respondió
                    .Include(n => n.RegistroAcceso)
                        .ThenInclude(r => r.Vehiculo)
                    .Include(n => n.RegistroAcceso)
                        .ThenInclude(r => r.Visitante)
                    .Include(n => n.RegistroAcceso)
                        .ThenInclude(r => r.ResidenteDestino)
                    .Where(n => n.Estado == "respondido" &&
                           n.FechaRespuesta >= veinticuatroHorasAtras)
                    .OrderByDescending(n => n.FechaRespuesta)
                    .Take(20)
                    .ToList();

                // 3. También obtener registros aprobados/rechazados (para compatibilidad)
                var registrosRecientes = _context.RegistrosAccesos
                    .Include(r => r.Vehiculo)
                    .Include(r => r.Visitante)
                    .Include(r => r.ResidenteDestino)
                    .Include(r => r.Notificaciones)  // Incluir notificaciones
                        .ThenInclude(n => n.Residente)
                    .Where(r => (r.EstadoAutorizacion == "aprobado" || r.EstadoAutorizacion == "rechazado") &&
                           r.FechaEntrada >= veinticuatroHorasAtras)
                    .OrderByDescending(r => r.FechaEntrada)
                    .Take(30)
                    .ToList();

                // Pasar a la vista
                ViewBag.Recientes = notificacionesRespondidas;
                ViewBag.RegistrosRecientes = registrosRecientes;

                return View(pendientes);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error al cargar: " + ex.Message;
                return View(new List<RegistrosAcceso>());
            }
        }

        // ---------------------------------------------------------
        // OBTENER GUARDIA ACTUAL
        // ---------------------------------------------------------
        private int ObtenerGuardiaId()
        {
            var userId = HttpContext.Session.GetString("UserId");
            return int.TryParse(userId, out int id) ? id : 0;
        }

        // ---------------------------------------------------------
        // REPORTES DE INCIDENTES – (GUARDIA)
        // ---------------------------------------------------------

        public IActionResult ReportarIncidente()
        {
            if (!EsGuardiaAutenticado())
                return RedirectToAction("Login", "Auth");

            return View();
        }

        [HttpPost]
        public IActionResult ReportarIncidente(string tipo, string descripcion, string placa, string unidad)
        {
            if (!EsGuardiaAutenticado())
                return RedirectToAction("Login", "Auth");

            if (string.IsNullOrWhiteSpace(tipo) || string.IsNullOrWhiteSpace(descripcion))
            {
                TempData["Error"] = "El tipo y la descripción son obligatorios.";
                return RedirectToAction("ReportarIncidente");
            }

            RegistrosAcceso? registro = null;

            // Si el guardia escribió placa → buscar registro activo o más reciente
            if (!string.IsNullOrWhiteSpace(placa))
            {
                registro = _context.RegistrosAccesos
                    .Include(r => r.Vehiculo)
                    .Include(r => r.Visitante)
                    .Include(r => r.ResidenteDestino)
                    .Where(r => r.Vehiculo.Placa == placa.Trim().ToUpper())
                    .OrderByDescending(r => r.FechaEntrada)
                    .FirstOrDefault();
            }

            // Crear incidente
            var incidente = new Incidente
            {
                RegistroAccesoId = registro?.Id,
                ReportadoPor = ObtenerGuardiaId(),
                TipoIncidente = tipo,
                Descripcion = descripcion,
                FechaCreacion = DateTime.Now
            };

            _context.Incidentes.Add(incidente);
            _context.SaveChanges();

            TempData["Mensaje"] = "Incidente reportado correctamente.";
            return RedirectToAction("Index");
        }


    }
}
