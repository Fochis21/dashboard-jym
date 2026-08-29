using DashboardJym.Data;
using DashboardJym.Models;
using DashboardJym.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DashboardJym.Controllers;

[Authorize]
[Route("app/clientes")]
public class ClientesController : Controller
{
    private readonly AppDbContext _context;
    private readonly SesionUtil _sesionUtil;
    private readonly RegistroActividadHelper _registroActividad;

    public ClientesController(AppDbContext context, SesionUtil sesionUtil, RegistroActividadHelper registroActividad)
    {
        _context = context;
        _sesionUtil = sesionUtil;
        _registroActividad = registroActividad;
    }

    // GET /app/clientes?q=...
    [HttpGet]
    public async Task<IActionResult> Listar(string? q)
    {
        var query = _context.Clientes.Where(c => c.Estado);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var texto = q.Trim();
            query = query.Where(c =>
                EF.Functions.ILike(c.Nombres, $"%{texto}%") ||
                EF.Functions.ILike(c.Apellidos, $"%{texto}%") ||
                c.Dni.Contains(texto));
        }

        ViewData["Title"] = "Clientes";
        ViewData["Q"] = q;
        var clientes = await query.OrderBy(c => c.Apellidos).ToListAsync();
        return View(clientes);
    }

    // GET /app/clientes/nuevo
    [HttpGet("nuevo")]
    public IActionResult FormularioNuevo()
    {
        ViewData["Title"] = "Registrar cliente";
        ViewData["EsNuevo"] = true;
        return View("Formulario", new Cliente());
    }

    // POST /app/clientes
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrar(Cliente cliente)
    {
        // Solo se toman los campos del formulario simplificado; el resto
        // (usuario, estado, fechas) se completa aquí, igual que en el service Java.
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Registrar cliente";
            ViewData["EsNuevo"] = true;
            return View("Formulario", cliente);
        }

        var usuarioActual = await _sesionUtil.ObtenerUsuarioActualAsync();

        cliente.UsuarioRegistroId = usuarioActual.Id;
        cliente.Estado = true;
        cliente.FechaRegistro = DateTime.Now;
        cliente.FechaActualizacion = DateTime.Now;

        _context.Clientes.Add(cliente);
        await _context.SaveChangesAsync();

        await _registroActividad.RegistrarAsync(usuarioActual.Id, "Registró cliente", "Clientes", cliente.Id,
            $"Registró al cliente {cliente.Nombres} {cliente.Apellidos}");

        TempData["Mensaje"] = "Cliente registrado correctamente.";
        return RedirectToAction(nameof(Listar));
    }

    // GET /app/clientes/{id}/editar
    [HttpGet("{id:long}/editar")]
    public async Task<IActionResult> FormularioEditar(long id)
    {
        var cliente = await _context.Clientes.FindAsync(id);
        if (cliente == null) return NotFound();

        ViewData["Title"] = "Editar cliente";
        ViewData["EsNuevo"] = false;
        return View("Formulario", cliente);
    }

    // POST /app/clientes/{id}
    [HttpPost("{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Actualizar(long id, Cliente datos)
    {
        var cliente = await _context.Clientes.FindAsync(id);
        if (cliente == null) return NotFound();

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Editar cliente";
            ViewData["EsNuevo"] = false;
            datos.Id = id;
            return View("Formulario", datos);
        }

        // Mismos campos editables que en el service Java: no se toca
        // usuario_registro_id, estado ni fecha_registro.
        cliente.Nombres = datos.Nombres;
        cliente.Apellidos = datos.Apellidos;
        cliente.Dni = datos.Dni;
        cliente.FechaNacimiento = datos.FechaNacimiento;
        cliente.Whatsapp = datos.Whatsapp;
        cliente.Correo = datos.Correo;
        cliente.Observaciones = datos.Observaciones;
        cliente.FechaActualizacion = DateTime.Now;

        await _context.SaveChangesAsync();

        var usuarioActual = await _sesionUtil.ObtenerUsuarioActualAsync();
        await _registroActividad.RegistrarAsync(usuarioActual.Id, "Editó cliente", "Clientes", cliente.Id,
            $"Editó los datos del cliente {cliente.Nombres} {cliente.Apellidos}");

        TempData["Mensaje"] = "Cliente actualizado correctamente.";
        return RedirectToAction(nameof(Listar));
    }

    // GET /app/clientes/{id}
    [HttpGet("{id:long}")]
    public async Task<IActionResult> VerDetalle(long id)
    {
        var cliente = await _context.Clientes
            .Include(c => c.UsuarioRegistro)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cliente == null) return NotFound();

        ViewData["Title"] = "Detalle de cliente";
        return View("Detalle", cliente);
    }

    // POST /app/clientes/{id}/desactivar
    [HttpPost("{id:long}/desactivar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Desactivar(long id)
    {
        var cliente = await _context.Clientes.FindAsync(id);
        if (cliente == null) return NotFound();

        // Eliminacion logica: el cliente permanece en la BD.
        cliente.Estado = false;
        cliente.FechaActualizacion = DateTime.Now;
        await _context.SaveChangesAsync();

        var usuarioActual = await _sesionUtil.ObtenerUsuarioActualAsync();
        await _registroActividad.RegistrarAsync(usuarioActual.Id, "Desactivó cliente", "Clientes", cliente.Id,
            $"Desactivó al cliente {cliente.Nombres} {cliente.Apellidos}");

        TempData["Mensaje"] = "Cliente desactivado correctamente.";
        return RedirectToAction(nameof(Listar));
    }
}
