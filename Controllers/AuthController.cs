using ControlAccesoFraccionamiento.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControlAccesoFraccionamiento.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST Login
        [HttpPost]
        public async Task<IActionResult> Login(string Email, string Contrasena)
        {

            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Contrasena))
            {
                ViewBag.Error = "Debe ingresar email y contraseña";
                return View();
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email != null
                                       && u.Email.ToLower() == Email.ToLower()
                                       && u.Contrasena == Contrasena);

            if (usuario == null)
            {
                ViewBag.Error = "Email o contraseña incorrectos";
                return View();
            }

            // Guardar sesión
            HttpContext.Session.SetString("UserId", usuario.Id.ToString());
            HttpContext.Session.SetString("UserRol", usuario.Rol);
            HttpContext.Session.SetString("UserName", usuario.Nombre);

            // 🔥 NUEVO: Redirección por roles
            // Convertir a minúsculas para coincidir con BD
            var rol = usuario.Rol?.ToLower() ?? "";

            return rol switch
            {
                "admin" => RedirectToAction("Index", "Admin"),
                "guardia" => RedirectToAction("Index", "Guardia"),
                "residente" => RedirectToAction("Index", "Residente"),
                _ => RedirectToAction("Index", "Home") // Rol desconocido
            };
        }

        // Cerrar sesión
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        public IActionResult CambiarContrasena()
        {
            // Verificar si el usuario está logueado
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }

            // Pasar datos a la vista
            ViewBag.Nombre = HttpContext.Session.GetString("UserName");
            ViewBag.Rol = HttpContext.Session.GetString("UserRol");

            // Determinar URL de retorno SEGURA usando Url.Action
            var userRol = HttpContext.Session.GetString("UserRol");
            ViewBag.UrlRetorno = DeterminarUrlRetorno(userRol);

            return View();
        }

        // Método auxiliar CORREGIDO usando Url.Action
        private string DeterminarUrlRetorno(string rol)
        {
            if (string.IsNullOrEmpty(rol))
                return Url.Action("Login", "Auth"); // O "/" si prefieres

            rol = rol.ToLower();

            return rol switch
            {
                "admin" => Url.Action("Index", "Admin"),
                "guardia" => Url.Action("Index", "Guardia"),
                "residente" => Url.Action("Index", "Residente"),
                _ => Url.Action("Login", "Auth") // Por defecto al login
            };
        }

        // POST: Procesar el cambio de contraseña
        [HttpPost]
        public async Task<IActionResult> CambiarContrasena(
            string contrasenaActual,
            string nuevaContrasena,
            string confirmarContrasena)
        {
            // Verificar si el usuario está logueado
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }

            // Validaciones básicas
            if (string.IsNullOrWhiteSpace(contrasenaActual))
            {
                ViewBag.Error = "La contraseña actual es requerida";
                return View();
            }

            if (string.IsNullOrWhiteSpace(nuevaContrasena))
            {
                ViewBag.Error = "La nueva contraseña es requerida";
                return View();
            }

            if (string.IsNullOrWhiteSpace(confirmarContrasena))
            {
                ViewBag.Error = "Debe confirmar la nueva contraseña";
                return View();
            }

            if (nuevaContrasena != confirmarContrasena)
            {
                ViewBag.Error = "Las nuevas contraseñas no coinciden";
                return View();
            }

            if (nuevaContrasena.Length < 4)
            {
                ViewBag.Error = "La nueva contraseña debe tener al menos 4 caracteres";
                return View();
            }

            try
            {
                var id = int.Parse(userId);
                var usuario = await _context.Usuarios.FindAsync(id);

                if (usuario == null)
                {
                    // Si el usuario no existe en BD, limpiar sesión
                    HttpContext.Session.Clear();
                    return RedirectToAction("Login");
                }

                // Verificar contraseña actual (como la guardas actualmente - texto plano)
                if (usuario.Contrasena != contrasenaActual)
                {
                    ViewBag.Error = "La contraseña actual es incorrecta";

                    // Pasar datos a la vista nuevamente
                    ViewBag.Nombre = HttpContext.Session.GetString("UserName");
                    ViewBag.Rol = HttpContext.Session.GetString("UserRol");

                    return View();
                }

                // Actualizar contraseña
                usuario.Contrasena = nuevaContrasena;
                _context.Usuarios.Update(usuario);
                await _context.SaveChangesAsync();

                TempData["Mensaje"] = "✅ Contraseña cambiada exitosamente";
                return RedirectToAction("CambiarContrasena");
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error al cambiar contraseña: {ex.Message}";

                // Pasar datos a la vista nuevamente
                ViewBag.Nombre = HttpContext.Session.GetString("UserName");
                ViewBag.Rol = HttpContext.Session.GetString("UserRol");

                return View();
            }
        }
    }
}
