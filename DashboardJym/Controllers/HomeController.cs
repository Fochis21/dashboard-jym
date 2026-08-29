using DashboardJym.Data;
using DashboardJym.Models;
using DashboardJym.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DashboardJym.Controllers;

[Authorize]
public class HomeController : Controller
{
    private const int LimitePanel = 5;

    private readonly AppDbContext _context;
    private readonly SesionUtil _sesionUtil;

    public HomeController(AppDbContext context, SesionUtil sesionUtil)
    {
        _context = context;
        _sesionUtil = sesionUtil;
    }

    // GET / o /inicio - panel de inicio: pagos pendientes, proximas citas,
    // proximas audiencias, mensajes recientes, notificaciones. Sin
    // estadisticas ni graficos, igual que en el proyecto Java.
    [Route("/")]
    [Route("/inicio")]
    public async Task<IActionResult> Index()
    {
        var usuarioActual = await _sesionUtil.ObtenerUsuarioActualAsync();
        var hoy = DateTime.Today;

        ViewBag.PagosPendientes = await _context.Pagos
            .Include(p => p.Cliente)
            .Include(p => p.UsuarioRegistro)
            .Where(p => p.Estado == EstadoPago.PENDIENTE)
            .OrderByDescending(p => p.FechaRegistro)
            .Take(LimitePanel)
            .ToListAsync();

        var proximas = await _context.Agenda
            .Include(a => a.Cliente)
            .Where(a => a.Fecha >= hoy)
            .OrderBy(a => a.Fecha).ThenBy(a => a.Hora)
            .ToListAsync();

        ViewBag.ProximasCitas = proximas.Where(a => a.Tipo == TipoAgenda.CITA).Take(LimitePanel).ToList();
        ViewBag.ProximasAudiencias = proximas.Where(a => a.Tipo == TipoAgenda.AUDIENCIA).Take(LimitePanel).ToList();

        ViewBag.MensajesRecientes = await _context.Mensajes
            .Include(m => m.Remitente)
            .Where(m => m.DestinatarioId == usuarioActual.Id)
            .OrderByDescending(m => m.FechaEnvio)
            .Take(LimitePanel)
            .ToListAsync();

        ViewBag.NotificacionesRecientes = await _context.Notificaciones
            .Where(n => n.UsuarioId == usuarioActual.Id)
            .OrderByDescending(n => n.FechaCreacion)
            .Take(LimitePanel)
            .ToListAsync();

        ViewData["Title"] = "Inicio";
        return View();
    }

    // GET /app/mi-perfil
    [HttpGet("/app/mi-perfil")]
    public async Task<IActionResult> MiPerfil()
    {
        var usuarioActual = await _sesionUtil.ObtenerUsuarioActualAsync();
        ViewData["Title"] = "Mi perfil";
        return View(usuarioActual);
    }

    public IActionResult Error()
    {
        return Content("Ocurrió un error.");
    }
}
