using DashboardJym.Data;
using DashboardJym.Models;
using DashboardJym.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DashboardJym.Controllers;

[Authorize]
[Route("app/mensajes")]
public class MensajesController : Controller
{
    private readonly AppDbContext _context;
    private readonly SesionUtil _sesionUtil;
    private readonly NotificacionHelper _notificaciones;
    private readonly RegistroActividadHelper _registroActividad;

    public MensajesController(AppDbContext context, SesionUtil sesionUtil, NotificacionHelper notificaciones, RegistroActividadHelper registroActividad)
    {
        _context = context;
        _sesionUtil = sesionUtil;
        _notificaciones = notificaciones;
        _registroActividad = registroActividad;
    }

    // GET /app/mensajes — lista de "conversaciones": todos los demas usuarios,
    // con su conteo de no leidos.
    [HttpGet]
    public async Task<IActionResult> ListarContactos()
    {
        var usuarioActual = await _sesionUtil.ObtenerUsuarioActualAsync();

        var contactos = await _context.Usuarios
            .Where(u => u.Id != usuarioActual.Id)
            .OrderBy(u => u.Nombres)
            .ToListAsync();

        var noLeidosPorContacto = await _context.Mensajes
            .Where(m => m.DestinatarioId == usuarioActual.Id && !m.Leido)
            .GroupBy(m => m.RemitenteId)
            .Select(g => new { RemitenteId = g.Key, Cantidad = g.Count() })
            .ToDictionaryAsync(x => x.RemitenteId, x => x.Cantidad);

        ViewBag.NoLeidosPorContacto = noLeidosPorContacto;
        ViewData["Title"] = "Mensajes";
        return View("Lista", contactos);
    }

    // GET /app/mensajes/{otroUsuarioId}
    [HttpGet("{otroUsuarioId:long}")]
    public async Task<IActionResult> VerConversacion(long otroUsuarioId)
    {
        var usuarioActual = await _sesionUtil.ObtenerUsuarioActualAsync();
        var otroUsuario = await _context.Usuarios.FindAsync(otroUsuarioId);
        if (otroUsuario == null) return NotFound();

        // Marcar como leidos los mensajes que el otro usuario me envio.
        var noLeidos = await _context.Mensajes
            .Where(m => m.DestinatarioId == usuarioActual.Id && m.RemitenteId == otroUsuarioId && !m.Leido)
            .ToListAsync();
        foreach (var m in noLeidos)
        {
            m.Leido = true;
            m.FechaLectura = DateTime.Now;
        }
        if (noLeidos.Count > 0) await _context.SaveChangesAsync();

        var mensajes = await _context.Mensajes
            .Where(m => (m.RemitenteId == usuarioActual.Id && m.DestinatarioId == otroUsuarioId) ||
                        (m.RemitenteId == otroUsuarioId && m.DestinatarioId == usuarioActual.Id))
            .OrderBy(m => m.FechaEnvio)
            .ToListAsync();

        ViewBag.OtroUsuario = otroUsuario;
        ViewBag.UsuarioActualId = usuarioActual.Id;
        ViewData["Title"] = "Conversación";
        return View("Conversacion", mensajes);
    }

    // POST /app/mensajes/{otroUsuarioId}
    [HttpPost("{otroUsuarioId:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enviar(long otroUsuarioId, string contenido, string? asunto)
    {
        if (string.IsNullOrWhiteSpace(contenido))
        {
            return RedirectToAction(nameof(VerConversacion), new { otroUsuarioId });
        }

        var usuarioActual = await _sesionUtil.ObtenerUsuarioActualAsync();
        var destinatario = await _context.Usuarios.FindAsync(otroUsuarioId);
        if (destinatario == null) return NotFound();

        var mensaje = new Mensaje
        {
            RemitenteId = usuarioActual.Id,
            DestinatarioId = otroUsuarioId,
            Asunto = asunto,
            Contenido = contenido,
            Leido = false,
            FechaEnvio = DateTime.Now,
        };
        _context.Mensajes.Add(mensaje);
        await _context.SaveChangesAsync();

        var resumen = contenido.Length > 120 ? contenido[..120] + "..." : contenido;
        await _notificaciones.NotificarAsync(
            destinatario.Id,
            $"Nuevo mensaje de {usuarioActual.Nombres}",
            resumen,
            "MENSAJE",
            mensaje.Id);

        await _registroActividad.RegistrarAsync(usuarioActual.Id, "Envió mensaje", "Mensajes", mensaje.Id,
            $"Envió un mensaje a {destinatario.Nombres} {destinatario.Apellidos}");

        return RedirectToAction(nameof(VerConversacion), new { otroUsuarioId });
    }
}
