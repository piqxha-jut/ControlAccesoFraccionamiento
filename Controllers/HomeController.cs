using Microsoft.AspNetCore.Mvc;

namespace ControlAccesoFraccionamiento.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // Verificar si está logueado
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Auth");

            var nombre = HttpContext.Session.GetString("UserName");
            var rol = HttpContext.Session.GetString("UserRol");

            return Content("🏠 PÁGINA PRINCIPAL<br><br>" +
                          $"Usuario: {nombre}<br>" +
                          $"Rol: {rol}<br><br>" +
                          "<a href='/Auth/Logout'>Cerrar sesión</a>");
        }
    }
}
