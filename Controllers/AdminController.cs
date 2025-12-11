using ControlAccesoFraccionamiento.Data;
using ControlAccesoFraccionamiento.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControlAccesoFraccionamiento.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ---------------------------------------------------------
        // VERIFICAR SESIÓN Y ROL
        // ---------------------------------------------------------
        private bool EsAdminAutenticado()
        {
            var userId = HttpContext.Session.GetString("UserId");
            var userRol = HttpContext.Session.GetString("UserRol");
            return !string.IsNullOrEmpty(userId) && userRol == "admin";
        }

        // ---------------------------------------------------------
        // PANEL PRINCIPAL DEL ADMIN
        // ---------------------------------------------------------
        public IActionResult Index()
        {
            if (!EsAdminAutenticado())
                return RedirectToAction("Login", "Auth");

            ViewBag.Nombre = HttpContext.Session.GetString("UserName");

            var hoy = DateTime.Today;

            ViewBag.TotalResidentes = _context.Residentes.Count();
            ViewBag.TotalVisitantes = _context.Visitantes.Count();
            ViewBag.TotalVehiculos = _context.Vehiculos.Count();

            ViewBag.AccesosHoy = _context.RegistrosAccesos
                .Count(r => r.FechaEntrada.HasValue &&
                            r.FechaEntrada.Value.Date == hoy);

            ViewBag.PendientesHoy = _context.RegistrosAccesos
                .Count(r => r.EstadoAutorizacion == "pendiente" &&
                            r.FechaEntrada.HasValue &&
                            r.FechaEntrada.Value.Date == hoy);

            ViewBag.IncidentesHoy = _context.Incidentes
                .Count(i => i.FechaCreacion.HasValue &&
                            i.FechaCreacion.Value.Date == hoy);

            return View();
        }

        // ---------------------------------------------------------
        // GESTIÓN DE USUARIOS
        // ---------------------------------------------------------
        public IActionResult Usuarios()
        {
            if (!EsAdminAutenticado())
                return RedirectToAction("Login", "Auth");

            var usuarios = _context.Usuarios
                .Include(u => u.Residente)
                .OrderBy(u => u.Rol)
                .ThenBy(u => u.Nombre)
                .ToList();

            return View(usuarios);
        }

        [HttpPost]
        public IActionResult CrearUsuario(string nombre, string email, string rol, string telefono)
        {
            if (!EsAdminAutenticado())
                return RedirectToAction("Login", "Auth");

            try
            {
                if (_context.Usuarios.Any(u => u.Email == email))
                {
                    TempData["Error"] = "El email ya está registrado";
                    return RedirectToAction("Usuarios");
                }

                var usuario = new Usuario
                {
                    Nombre = nombre,
                    Email = email,
                    Rol = rol,
                    Telefono = telefono,
                    Contrasena = "123",
                    FechaCreacion = DateTime.Now
                };

                _context.Usuarios.Add(usuario);
                _context.SaveChanges();

                if (rol == "residente")
                {
                    var residente = new Residente
                    {
                        UsuarioId = usuario.Id,
                        Unidad = "Por asignar",
                        Direccion = "Por asignar",
                        Activo = true,
                        FechaCreacion = DateTime.Now
                    };

                    _context.Residentes.Add(residente);
                    _context.SaveChanges();
                }

                TempData["Mensaje"] = "Usuario creado exitosamente";
                return RedirectToAction("Usuarios");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al crear usuario: " + ex.Message;
                return RedirectToAction("Usuarios");
            }
        }

        [HttpPost]
        public IActionResult ActualizarUsuario(int id, string nombre, string email, string rol, string telefono)
        {
            if (!EsAdminAutenticado())
                return RedirectToAction("Login", "Auth");

            var usuario = _context.Usuarios.Find(id);
            if (usuario == null)
            {
                TempData["Error"] = "Usuario no encontrado";
                return RedirectToAction("Usuarios");
            }

            usuario.Nombre = nombre;
            usuario.Email = email;
            usuario.Rol = rol;
            usuario.Telefono = telefono;

            _context.SaveChanges();
            TempData["Mensaje"] = "Usuario actualizado correctamente";

            return RedirectToAction("Usuarios");
        }

        [HttpPost]
        public IActionResult EliminarUsuario(int id)
        {
            if (!EsAdminAutenticado())
                return RedirectToAction("Login", "Auth");

            var usuario = _context.Usuarios.FirstOrDefault(u => u.Id == id);
            if (usuario == null)
            {
                TempData["Error"] = "Usuario no encontrado";
                return RedirectToAction("Usuarios");
            }

            _context.Usuarios.Remove(usuario);
            _context.SaveChanges();

            TempData["Mensaje"] = "Usuario eliminado correctamente";
            return RedirectToAction("Usuarios");
        }


        // ---------------------------------------------------------
        // GESTIÓN DE RESIDENTES
        // ---------------------------------------------------------
        public IActionResult Residentes()
        {
            if (!EsAdminAutenticado())
                return RedirectToAction("Login", "Auth");

            var residentes = _context.Residentes
                .Include(r => r.Usuario)
                .Include(r => r.VehiculosResidentes)
                .ThenInclude(vr => vr.Vehiculo)
                .OrderBy(r => r.Unidad)
                .ToList();

            return View(residentes);
        }

        [HttpPost]
        public IActionResult ActualizarResidente(int id, string unidad, string direccion, bool activo)
        {
            if (!EsAdminAutenticado())
                return RedirectToAction("Login", "Auth");

            try
            {
                var residente = _context.Residentes.Find(id);
                if (residente == null)
                {
                    TempData["Error"] = "Residente no encontrado";
                    return RedirectToAction("Residentes");
                }

                residente.Unidad = unidad;
                residente.Direccion = direccion;
                residente.Activo = activo;

                _context.SaveChanges();
                TempData["Mensaje"] = "Residente actualizado exitosamente";
                return RedirectToAction("Residentes");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al actualizar: " + ex.Message;
                return RedirectToAction("Residentes");
            }
        }

        [HttpPost]
        public IActionResult CrearResidente(string nombre, string email, string telefono, string unidad, string direccion)
        {
            if (!EsAdminAutenticado())
                return RedirectToAction("Login", "Auth");

            try
            {
                if (_context.Usuarios.Any(u => u.Email == email))
                {
                    TempData["Error"] = "El email ya está registrado.";
                    return RedirectToAction("Residentes");
                }

                // Crear usuario
                var usuario = new Usuario
                {
                    Nombre = nombre,
                    Email = email,
                    Telefono = telefono,
                    Rol = "residente",
                    Contrasena = "123",
                    FechaCreacion = DateTime.Now
                };

                _context.Usuarios.Add(usuario);
                _context.SaveChanges();

                // Crear residente asociado
                var residente = new Residente
                {
                    UsuarioId = usuario.Id,
                    Unidad = unidad,
                    Direccion = direccion,
                    Activo = true,
                    FechaCreacion = DateTime.Now
                };

                _context.Residentes.Add(residente);
                _context.SaveChanges();

                TempData["Mensaje"] = "Residente creado exitosamente.";
                return RedirectToAction("Residentes");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al crear residente: " + ex.Message;
                return RedirectToAction("Residentes");
            }
        }

        [HttpPost]
        public IActionResult EliminarResidente(int id)
        {
            if (!EsAdminAutenticado())
                return RedirectToAction("Login", "Auth");

            try
            {
                var residente = _context.Residentes
                    .Include(r => r.Usuario)
                    .FirstOrDefault(r => r.Id == id);

                if (residente == null)
                {
                    TempData["Error"] = "Residente no encontrado.";
                    return RedirectToAction("Residentes");
                }

                // Eliminar usuario primero
                _context.Usuarios.Remove(residente.Usuario);
                _context.Residentes.Remove(residente);
                _context.SaveChanges();

                TempData["Mensaje"] = "Residente eliminado correctamente.";
                return RedirectToAction("Residentes");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al eliminar residente: " + ex.Message;
                return RedirectToAction("Residentes");
            }
        }


        // ---------------------------------------------------------
        // GESTIÓN DE VEHÍCULOS
        // ---------------------------------------------------------
        public IActionResult Vehiculos()
        {
            if (!EsAdminAutenticado())
                return RedirectToAction("Login", "Auth");

            var vehiculos = _context.Vehiculos
                .Include(v => v.VehiculosResidentes)
                .ThenInclude(vr => vr.Residente)
                .ThenInclude(r => r.Usuario)
                .Include(v => v.VehiculosVisitantes)
                .ThenInclude(vv => vv.Visitante)
                .OrderBy(v => v.Placa)
                .ToList();

            // 🔥 Traer residentes activos para crear/editar vehículo
            ViewBag.Residentes = _context.Residentes
                .Include(r => r.Usuario)
                .Where(r => r.Activo == true)
                .OrderBy(r => r.Unidad)
                .ToList();

            return View(vehiculos);
        }

        // ---------------------------------------------------------
        // CREAR VEHÍCULO (POST) - Solo asociado a residentes
        // ---------------------------------------------------------
        [HttpPost]
        public IActionResult CrearVehiculo(string placa, string marca, string modelo, string color, int residenteId)
        {
            if (!EsAdminAutenticado())
                return RedirectToAction("Login", "Auth");

            if (string.IsNullOrWhiteSpace(placa) || residenteId == 0)
            {
                TempData["Error"] = "La placa y el residente son obligatorios.";
                return RedirectToAction("Vehiculos");
            }

            placa = placa.Trim().ToUpper();

            try
            {
                if (_context.Vehiculos.Any(v => v.Placa == placa))
                {
                    TempData["Error"] = "Ya existe un vehículo con esa placa.";
                    return RedirectToAction("Vehiculos");
                }

                var vehiculo = new Vehiculo
                {
                    Placa = placa,
                    Marca = string.IsNullOrWhiteSpace(marca) ? null : marca.Trim(),
                    Modelo = string.IsNullOrWhiteSpace(modelo) ? null : modelo.Trim(),
                    Color = string.IsNullOrWhiteSpace(color) ? null : color.Trim(),
                    Tipo = "residente",
                    FechaCreacion = DateTime.Now
                };

                _context.Vehiculos.Add(vehiculo);
                _context.SaveChanges();

                // Asociar al residente
                var vehRes = new VehiculosResidente
                {
                    VehiculoId = vehiculo.Id,
                    ResidenteId = residenteId
                };
                _context.VehiculosResidentes.Add(vehRes);
                _context.SaveChanges();

                TempData["Mensaje"] = "Vehículo creado y asociado al residente.";
                return RedirectToAction("Vehiculos");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al crear vehículo: " + ex.Message;
                return RedirectToAction("Vehiculos");
            }
        }

        // ---------------------------------------------------------
        // EDITAR VEHÍCULO (POST) - actualizar datos y asociación
        // ---------------------------------------------------------
        [HttpPost]
        public IActionResult EditarVehiculo(int id, string marca, string modelo, string color, int residenteId)
        {
            if (!EsAdminAutenticado())
                return RedirectToAction("Login", "Auth");

            try
            {
                var vehiculo = _context.Vehiculos
                    .Include(v => v.VehiculosResidentes)
                    .FirstOrDefault(v => v.Id == id);

                if (vehiculo == null)
                {
                    TempData["Error"] = "Vehículo no encontrado.";
                    return RedirectToAction("Vehiculos");
                }

                vehiculo.Marca = string.IsNullOrWhiteSpace(marca) ? null : marca.Trim();
                vehiculo.Modelo = string.IsNullOrWhiteSpace(modelo) ? null : modelo.Trim();
                vehiculo.Color = string.IsNullOrWhiteSpace(color) ? null : color.Trim();

                // Actualizar asociación con residente: eliminamos asociaciones previas y ponemos la nueva
                var actuales = vehiculo.VehiculosResidentes.ToList();
                foreach (var ar in actuales)
                {
                    _context.VehiculosResidentes.Remove(ar);
                }
                _context.SaveChanges();

                if (residenteId != 0)
                {
                    _context.VehiculosResidentes.Add(new VehiculosResidente
                    {
                        VehiculoId = vehiculo.Id,
                        ResidenteId = residenteId
                    });
                }

                _context.SaveChanges();

                TempData["Mensaje"] = "Vehículo actualizado correctamente.";
                return RedirectToAction("Vehiculos");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al actualizar vehículo: " + ex.Message;
                return RedirectToAction("Vehiculos");
            }
        }

        // ---------------------------------------------------------
        // ELIMINAR VEHÍCULO (POST)
        // ---------------------------------------------------------
        [HttpPost]
        public IActionResult EliminarVehiculo(int id)
        {
            if (!EsAdminAutenticado())
                return RedirectToAction("Login", "Auth");

            try
            {
                var vehiculo = _context.Vehiculos
                    .Include(v => v.VehiculosResidentes)
                    .Include(v => v.VehiculosVisitantes)
                    .Include(v => v.VehiculosListaNegras)
                    .FirstOrDefault(v => v.Id == id);

                if (vehiculo == null)
                {
                    TempData["Error"] = "Vehículo no encontrado.";
                    return RedirectToAction("Vehiculos");
                }

                // Eliminar asociaciones (residente / visitante / lista negra)
                foreach (var vr in vehiculo.VehiculosResidentes.ToList())
                    _context.VehiculosResidentes.Remove(vr);

                foreach (var vv in vehiculo.VehiculosVisitantes.ToList())
                    _context.VehiculosVisitantes.Remove(vv);

                foreach (var ln in vehiculo.VehiculosListaNegras.ToList())
                    _context.VehiculosListaNegras.Remove(ln);

                _context.Vehiculos.Remove(vehiculo);
                _context.SaveChanges();

                TempData["Mensaje"] = "Vehículo eliminado correctamente.";
                return RedirectToAction("Vehiculos");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al eliminar vehículo: " + ex.Message;
                return RedirectToAction("Vehiculos");
            }
        }


        // ---------------------------------------------------------
        // LISTA NEGRA
        // ---------------------------------------------------------
        public IActionResult ListaNegra()
        {
            if (!EsAdminAutenticado())
                return RedirectToAction("Login", "Auth");

            var listaNegra = _context.VehiculosListaNegras
                .Include(ln => ln.Vehiculo)
                .Include(ln => ln.AgregadoPorNavigation)
                .OrderByDescending(ln => ln.FechaAgregado)
                .ToList();

            // 🔥 Traer TODOS los vehículos del sistema CON SUS RELACIONES
            ViewBag.Vehiculos = _context.Vehiculos
                .Include(v => v.VehiculosResidentes)     // ← NECESARIO para ver tipo
                .Include(v => v.VehiculosVisitantes)     // ← NECESARIO para ver tipo
                .Include(v => v.VehiculosListaNegras)    // ← NECESARIO para ver si ya está en lista
                .Include(v => v.VehiculosPreaprobados)   // ← Opcional pero útil
                .OrderBy(v => v.Placa)
                .ToList();

            return View(listaNegra);
        }


        [HttpPost]
        public IActionResult AgregarListaNegra(int vehiculoId, string razon)
        {
            if (!EsAdminAutenticado())
                return RedirectToAction("Login", "Auth");

            try
            {
                if (_context.VehiculosListaNegras.Any(ln => ln.VehiculoId == vehiculoId))
                {
                    TempData["Error"] = "El vehículo ya está en lista negra";
                    return RedirectToAction("ListaNegra");
                }

                var listaNegra = new VehiculosListaNegra
                {
                    VehiculoId = vehiculoId,
                    Razon = razon,
                    AgregadoPor = ObtenerAdminId(),
                    FechaAgregado = DateTime.Now
                };

                _context.VehiculosListaNegras.Add(listaNegra);
                _context.SaveChanges();

                TempData["Mensaje"] = "Vehículo agregado a lista negra exitosamente";
                return RedirectToAction("ListaNegra");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al agregar a lista negra: " + ex.Message;
                return RedirectToAction("ListaNegra");
            }
        }

        [HttpPost]
        public IActionResult RemoverListaNegra(int id)
        {
            if (!EsAdminAutenticado())
                return RedirectToAction("Login", "Auth");

            try
            {
                var item = _context.VehiculosListaNegras.Find(id);
                if (item == null)
                {
                    TempData["Error"] = "Registro no encontrado";
                    return RedirectToAction("ListaNegra");
                }

                _context.VehiculosListaNegras.Remove(item);
                _context.SaveChanges();

                TempData["Mensaje"] = "Vehículo removido de lista negra";
                return RedirectToAction("ListaNegra");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al remover: " + ex.Message;
                return RedirectToAction("ListaNegra");
            }
        }

        // ---------------------------------------------------------
        // PREAPROBADOS
        // ---------------------------------------------------------
        public IActionResult Preaprobados()
        {
            if (!EsAdminAutenticado())
                return RedirectToAction("Login", "Auth");

            var preaprobados = _context.VehiculosPreaprobados
                .Include(p => p.Residente)
                .ThenInclude(r => r.Usuario)
                .Include(p => p.Vehiculo)
                .OrderByDescending(p => p.Activo)
                .ThenBy(p => p.Vehiculo.Placa)
                .ToList();

            ViewBag.Residentes = _context.Residentes
                .Include(r => r.Usuario)
                .Where(r => r.Activo == true)
                .OrderBy(r => r.Unidad)
                .ToList();

            // 🔥 FILTRAR: Solo vehículos que NO sean de residentes
            // Obtener IDs de vehículos que ya son de residentes
            var vehiculosResidentesIds = _context.VehiculosResidentes
                .Select(vr => vr.VehiculoId)
                .ToList();

            ViewBag.Vehiculos = _context.Vehiculos
                .Where(v => !vehiculosResidentesIds.Contains(v.Id)) // ← EXCLUIR residentes
                .OrderBy(v => v.Placa)
                .ToList();

            return View(preaprobados);
        }

        [HttpPost]
        public IActionResult CrearPreaprobado(int residenteId, int vehiculoId,
    DateTime fechaInicio, DateTime fechaFin, string? notas = null)
        {
            if (!EsAdminAutenticado())
                return RedirectToAction("Login", "Auth");

            try
            {
                // 1. Validar que el vehículo exista
                var vehiculo = _context.Vehiculos.Find(vehiculoId);
                if (vehiculo == null)
                {
                    TempData["Error"] = "El vehículo seleccionado no existe en el sistema.";
                    return RedirectToAction("Preaprobados");
                }

                // 2. Validar que el residente exista y esté activo
                var residente = _context.Residentes
                    .Include(r => r.Usuario)
                    .FirstOrDefault(r => r.Id == residenteId && r.Activo == true);

                if (residente == null)
                {
                    TempData["Error"] = "El residente seleccionado no existe o está inactivo.";
                    return RedirectToAction("Preaprobados");
                }

                // 3. Validar que el vehículo NO sea de residente
                bool esVehiculoResidente = _context.VehiculosResidentes
                    .Any(vr => vr.VehiculoId == vehiculoId);

                if (esVehiculoResidente)
                {
                    TempData["Error"] = $"El vehículo {vehiculo.Placa} ya pertenece a un residente. " +
                                       "Los residentes no necesitan preaprobación.";
                    return RedirectToAction("Preaprobados");
                }

                // 4. Validar que el vehículo NO esté en lista negra
                bool enListaNegra = _context.VehiculosListaNegras
                    .Any(ln => ln.VehiculoId == vehiculoId);

                if (enListaNegra)
                {
                    TempData["Error"] = $"El vehículo {vehiculo.Placa} está en lista negra. " +
                                       "No se puede preaprobar.";
                    return RedirectToAction("Preaprobados");
                }

                // 5. Validar que no exista ya preaprobado para este residente-vehículo
                if (_context.VehiculosPreaprobados
                    .Any(p => p.ResidenteId == residenteId && p.VehiculoId == vehiculoId))
                {
                    TempData["Error"] = $"El vehículo {vehiculo.Placa} ya está preaprobado " +
                                       $"para el residente {residente.Unidad}.";
                    return RedirectToAction("Preaprobados");
                }

                // 6. Validar fechas
                if (fechaFin < fechaInicio)
                {
                    TempData["Error"] = "La fecha fin debe ser posterior a la fecha inicio.";
                    return RedirectToAction("Preaprobados");
                }

                if (fechaInicio.Date < DateTime.Today)
                {
                    TempData["Error"] = "La fecha inicio no puede ser anterior a hoy.";
                    return RedirectToAction("Preaprobados");
                }

                // 7. Crear la preaprobación
                var preaprobado = new VehiculosPreaprobado
                {
                    ResidenteId = residenteId,
                    VehiculoId = vehiculoId,
                    FechaInicio = DateOnly.FromDateTime(fechaInicio),
                    FechaFin = DateOnly.FromDateTime(fechaFin),
                    Activo = true,
                    Notas = notas
                };

                _context.VehiculosPreaprobados.Add(preaprobado);
                _context.SaveChanges();

                // 8. Mensaje de éxito con detalles
                TempData["Mensaje"] = $"✅ Vehículo {vehiculo.Placa} preaprobado exitosamente " +
                                     $"para {residente.Unidad} ({residente.Usuario.Nombre}). " +
                                     $"Válido del {fechaInicio:dd/MM/yyyy} al {fechaFin:dd/MM/yyyy}.";

                return RedirectToAction("Preaprobados");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al crear preaprobación: {ex.Message}";
                return RedirectToAction("Preaprobados");
            }
        }

        // Opcional: Agregar métodos para activar/desactivar/eliminar
        [HttpPost]
        public IActionResult CambiarEstadoPreaprobado(int id, bool activo)
        {
            if (!EsAdminAutenticado())
                return RedirectToAction("Login", "Auth");

            try
            {
                var preaprobado = _context.VehiculosPreaprobados.Find(id);
                if (preaprobado == null)
                {
                    TempData["Error"] = "Preaprobación no encontrada";
                    return RedirectToAction("Preaprobados");
                }

                preaprobado.Activo = activo;
                _context.SaveChanges();

                TempData["Mensaje"] = $"Preaprobación {(activo ? "activada" : "desactivada")} correctamente";
                return RedirectToAction("Preaprobados");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cambiar estado: " + ex.Message;
                return RedirectToAction("Preaprobados");
            }
        }

        [HttpPost]
        public IActionResult EliminarPreaprobado(int id)
        {
            if (!EsAdminAutenticado())
                return RedirectToAction("Login", "Auth");

            try
            {
                var preaprobado = _context.VehiculosPreaprobados.Find(id);
                if (preaprobado == null)
                {
                    TempData["Error"] = "Preaprobación no encontrada";
                    return RedirectToAction("Preaprobados");
                }

                _context.VehiculosPreaprobados.Remove(preaprobado);
                _context.SaveChanges();

                TempData["Mensaje"] = "Preaprobación eliminada correctamente";
                return RedirectToAction("Preaprobados");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al eliminar: " + ex.Message;
                return RedirectToAction("Preaprobados");
            }
        }

        // ---------------------------------------------------------
        // REPORTES Y ESTADÍSTICAS (CORREGIDO DEFINITIVO)
        // ---------------------------------------------------------
        public IActionResult Reportes()
        {
            if (!EsAdminAutenticado())
                return RedirectToAction("Login", "Auth");

            var hoy = DateTime.Today;
            var fechaInicio7Dias = hoy.AddDays(-6);

            // ----------------------------
            // ACCESOS POR DÍA (últimos 7)
            // ----------------------------
            var accesosPorDia = _context.RegistrosAccesos
                .Where(r => r.FechaEntrada.Value.Date >= fechaInicio7Dias)
                .GroupBy(r => r.FechaEntrada.Value.Date)
                .Select(g => new
                {
                    Fecha = g.Key,
                    Total = g.Count(),
                    Residentes = g.Count(r => r.TipoAcceso == "residente"),
                    Visitantes = g.Count(r => r.TipoAcceso == "visitante")
                })
                .OrderBy(x => x.Fecha)
                .ToList();

            ViewBag.AccesosPorDia = accesosPorDia;

            ViewBag.Ultimos7Dias = Enumerable.Range(0, 7)
                .Select(i => fechaInicio7Dias.AddDays(i))
                .ToList();

            // ----------------------------
            // TOP UNIDADES (último mes)
            // ----------------------------
            var haceUnMes = hoy.AddMonths(-1);

            var topUnidades = _context.RegistrosAccesos
                .Where(r => r.FechaEntrada.Value.Date >= haceUnMes)
                .GroupBy(r => r.ResidenteDestino.Unidad)
                .Select(g => new
                {
                    Unidad = g.Key,
                    Visitas = g.Count()
                })
                .OrderByDescending(x => x.Visitas)
                .Take(10)
                .ToList();

            ViewBag.TopUnidades = topUnidades;

            // ----------------------------
            // RESUMEN RÁPIDO
            // ----------------------------
            ViewBag.TotalResidentes = _context.Residentes.Count();
            ViewBag.TotalVisitantes = _context.Visitantes.Count();
            ViewBag.TotalVehiculos = _context.Vehiculos.Count();

            ViewBag.AccesosHoy = _context.RegistrosAccesos
                .Count(r => r.FechaEntrada.Value.Date == hoy);

            ViewBag.PendientesHoy = _context.RegistrosAccesos
                .Count(r => r.EstadoAutorizacion == "pendiente" &&
                            r.FechaEntrada.Value.Date == hoy);

            ViewBag.IncidentesHoy = _context.Incidentes
                .Count(i => i.FechaCreacion.Value.Date == hoy);

            return View();
        }



        // ---------------------------------------------------------
        // REPORTE DIARIO (CORREGIDO - solo Value.Date)
        // ---------------------------------------------------------
        public IActionResult ReporteDiario(DateTime? fecha)
        {
            if (!EsAdminAutenticado())
                return RedirectToAction("Login", "Auth");

            var fechaReporte = fecha ?? DateTime.Today;

            var registros = _context.RegistrosAccesos
                .Include(r => r.Vehiculo)
                .Include(r => r.Visitante)
                .Include(r => r.ResidenteDestino)
                .Where(r => r.FechaEntrada.Value.Date == fechaReporte.Date)
                .OrderByDescending(r => r.FechaEntrada)
                .ToList();

            ViewBag.FechaReporte = fechaReporte;
            ViewBag.TotalEntradas = registros.Count;
            ViewBag.TotalSalidas = registros.Count(r => r.FechaSalida != null);
            ViewBag.Pendientes = registros.Count(r => r.EstadoAutorizacion == "pendiente");

            return View(registros);
        }



        // ---------------------------------------------------------
        // INCIDENTES
        // ---------------------------------------------------------
        public IActionResult Incidentes()
        {
            if (!EsAdminAutenticado())
                return RedirectToAction("Login", "Auth");

            var incidentes = _context.Incidentes
                .Include(i => i.RegistroAcceso)
                .ThenInclude(r => r.Vehiculo)
                .OrderByDescending(i => i.FechaCreacion)
                .ToList();

            // 🔥 Agregar diccionario de usuarios para poder mostrar nombres
            ViewBag.Usuarios = _context.Usuarios
                .ToDictionary(u => u.Id, u => u.Nombre);

            return View(incidentes);
        }

        // ---------------------------------------------------------
        // HISTORIAL COMPLETO
        // ---------------------------------------------------------
        public IActionResult Historial()
        {
            if (!EsAdminAutenticado())
                return RedirectToAction("Login", "Auth");

            var historial = _context.RegistrosAccesos
                .Include(r => r.Vehiculo)
                .Include(r => r.Visitante)
                .Include(r => r.ResidenteDestino)
                .Include(r => r.Guardia)
                .OrderByDescending(r => r.FechaEntrada)
                .Take(100)
                .ToList();

            return View(historial);
        }

        // ---------------------------------------------------------
        // UTILIDADES
        // ---------------------------------------------------------
        private int ObtenerAdminId()
        {
            var userId = HttpContext.Session.GetString("UserId");
            return int.TryParse(userId, out int id) ? id : 0;
        }
    }
}
