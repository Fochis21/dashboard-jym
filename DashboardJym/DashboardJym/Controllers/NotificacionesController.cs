using DashboardJym.Data;
using DashboardJym.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DashboardJym.Controllers;

[Authorize]
[Route("app/notificaciones")]
public class NotificacionesController : Controller
{
    private readonly AppDbContext _context;
    private readonly SesionUtil _sesionUtil;

    public NotificacionesController(AppDbContext context, SesionUtil sesionUtil)
    {
        _context = context;
        _sesionUtil = sesionUtil;
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var usuarioActual = await _sesionUtil.ObtenerUsuarioActualAsync();

        var notificaciones = await _context.Notificaciones
            .Where(n => n.UsuarioId == usuarioActual.Id)
            .OrderByDescending(n => n.FechaCreacion)
            .ToListAsync();

        ViewData["Title"] = "Notificaciones";
        return View("Lista", notificaciones);
    }

    [HttpPost("{id:long}/leer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarLeida(long id)
    {
        var notificacion = await _context.Notificaciones.FindAsync(id);
        if (notificacion == null) return NotFound();

        notificacion.Leida = true;
        notificacion.FechaLectura = DateTime.Now;
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Listar));
    }
}
