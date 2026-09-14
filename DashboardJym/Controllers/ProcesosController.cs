using DashboardJym.Data;
using DashboardJym.Models;
using DashboardJym.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DashboardJym.Controllers;

[Authorize]
[Route("app/procesos")]
public class ProcesosController : Controller
{
    private readonly AppDbContext _context;
    private readonly SesionUtil _sesionUtil;
    private readonly RegistroActividadHelper _registroActividad;

    public ProcesosController(AppDbContext context, SesionUtil sesionUtil, RegistroActividadHelper registroActividad)
    {
        _context = context;
        _sesionUtil = sesionUtil;
        _registroActividad = registroActividad;
    }

    // GET /app/procesos?q=...&clienteId=...
    [HttpGet]
    public async Task<IActionResult> Listar(string? q, long? clienteId)
    {
        var query = _context.Procesos
            .Include(p => p.Cliente)
            .Include(p => p.TipoProceso)
            .Include(p => p.AsesorResponsable)
            .Include(p => p.AbogadoResponsable)
            .AsQueryable();

        if (clienteId.HasValue)
        {
            query = query.Where(p => p.ClienteId == clienteId.Value)
                         .OrderByDescending(p => p.FechaRegistro);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(q))
            {
                var texto = q.Trim();
                query = query.Where(p =>
                    EF.Functions.ILike(p.Materia, $"%{texto}%") ||
                    EF.Functions.ILike(p.Cliente!.Nombres, $"%{texto}%") ||
                    EF.Functions.ILike(p.Cliente!.Apellidos, $"%{texto}%") ||
                    (p.NumeroExpediente != null && EF.Functions.ILike(p.NumeroExpediente, $"%{texto}%")));
            }
            query = query.OrderByDescending(p => p.FechaRegistro);
        }

        ViewData["Title"] = "Procesos";
        ViewData["Q"] = q;
        var procesos = await query.ToListAsync();
        return View(procesos);
    }

    // GET /app/procesos/nuevo?clienteId=...
    [HttpGet("nuevo")]
    public async Task<IActionResult> FormularioNuevo(long? clienteId)
    {
        var proceso = new Proceso();
        if (clienteId.HasValue)
        {
            proceso.ClienteId = clienteId.Value;
        }

        ViewData["Title"] = "Registrar proceso";
        ViewData["EsNuevo"] = true;
        await CargarListasApoyoAsync();
        return View("Formulario", proceso);
    }

    // POST /app/procesos
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrar(Proceso proceso)
    {
        ModelState.Remove(nameof(Proceso.Cliente));
        ModelState.Remove(nameof(Proceso.TipoProceso));
        ModelState.Remove(nameof(Proceso.AbogadoResponsable));
        ModelState.Remove(nameof(Proceso.AsesorResponsable));
        ModelState.Remove(nameof(Proceso.UsuarioRegistro));

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Registrar proceso";
            ViewData["EsNuevo"] = true;
            await CargarListasApoyoAsync();
            return View("Formulario", proceso);
        }

        var usuarioActual = await _sesionUtil.ObtenerUsuarioActualAsync();

        proceso.UsuarioRegistroId = usuarioActual.Id;
        proceso.FechaRegistro = DateTime.Now;
        proceso.FechaActualizacion = DateTime.Now;

        _context.Procesos.Add(proceso);
        await _context.SaveChangesAsync();

        var cliente = await _context.Clientes.FindAsync(proceso.ClienteId);
        await _registroActividad.RegistrarAsync(usuarioActual.Id, "Registró proceso", "Procesos", proceso.Id,
            $"Registró el proceso \"{proceso.Materia}\" del cliente {cliente?.Nombres} {cliente?.Apellidos}");

        TempData["Mensaje"] = "Proceso registrado correctamente.";
        return RedirectToAction(nameof(Listar));
    }

    // GET /app/procesos/{id}/editar
    [HttpGet("{id:long}/editar")]
    public async Task<IActionResult> FormularioEditar(long id)
    {
        var proceso = await _context.Procesos
            .Include(p => p.Cliente)
            .Include(p => p.TipoProceso)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (proceso == null) return NotFound();

        ViewData["Title"] = "Editar proceso";
        ViewData["EsNuevo"] = false;
        await CargarListasApoyoAsync();
        return View("Formulario", proceso);
    }

    // POST /app/procesos/{id}
    [HttpPost("{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Actualizar(long id, Proceso datos)
    {
        ModelState.Remove(nameof(Proceso.Cliente));
        ModelState.Remove(nameof(Proceso.TipoProceso));
        ModelState.Remove(nameof(Proceso.AbogadoResponsable));
        ModelState.Remove(nameof(Proceso.AsesorResponsable));
        ModelState.Remove(nameof(Proceso.UsuarioRegistro));

        var proceso = await _context.Procesos.FindAsync(id);
        if (proceso == null) return NotFound();

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Editar proceso";
            ViewData["EsNuevo"] = false;
            datos.Id = id;
            await CargarListasApoyoAsync();
            return View("Formulario", datos);
        }

        proceso.ClienteId = datos.ClienteId;
        proceso.TipoProcesoId = datos.TipoProcesoId;
        proceso.Materia = datos.Materia;
        proceso.Descripcion = datos.Descripcion;
        proceso.NumeroExpediente = datos.NumeroExpediente;
        proceso.JuzgadoFiscalia = datos.JuzgadoFiscalia;
        proceso.DistritoJudicial = datos.DistritoJudicial;
        proceso.FechaInicio = datos.FechaInicio;
        proceso.Estado = datos.Estado;
        proceso.Prioridad = datos.Prioridad;
        proceso.AbogadoResponsableId = datos.AbogadoResponsableId;
        proceso.AsesorResponsableId = datos.AsesorResponsableId;
        proceso.Observaciones = datos.Observaciones;
        proceso.FechaActualizacion = DateTime.Now;

        await _context.SaveChangesAsync();

        var usuarioActual = await _sesionUtil.ObtenerUsuarioActualAsync();
        await _registroActividad.RegistrarAsync(usuarioActual.Id, "Editó proceso", "Procesos", proceso.Id,
            $"Editó el proceso \"{proceso.Materia}\"");

        TempData["Mensaje"] = "Proceso actualizado correctamente.";
        return RedirectToAction(nameof(Listar));
    }

    // GET /app/procesos/{id}
    [HttpGet("{id:long}")]
    public async Task<IActionResult> VerDetalle(long id)
    {
        var proceso = await _context.Procesos
            .Include(p => p.Cliente)
            .Include(p => p.TipoProceso)
            .Include(p => p.AbogadoResponsable)
            .Include(p => p.AsesorResponsable)
            .Include(p => p.UsuarioRegistro)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (proceso == null) return NotFound();

        ViewData["Title"] = "Detalle de proceso";
        return View("Detalle", proceso);
    }

    private async Task CargarListasApoyoAsync()
    {
        ViewBag.Clientes = await _context.Clientes.Where(c => c.Estado).OrderBy(c => c.Apellidos).ToListAsync();

        // Se deduplica por nombre en memoria: puede haber tipos de proceso
        // repetidos cargados históricamente en la tabla y no queremos que
        // se vean duplicados en el selector.
        var tipos = await _context.TiposProceso.Where(t => t.Estado).OrderBy(t => t.Nombre).ToListAsync();
        ViewBag.TiposProceso = tipos
            .GroupBy(t => t.Nombre.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(t => t.Nombre)
            .ToList();

        var usuarios = await _context.Usuarios
            .Where(u => u.Estado)
            .Include(u => u.UsuarioRoles).ThenInclude(ur => ur.Rol)
            .OrderBy(u => u.Nombres)
            .ToListAsync();
        ViewBag.Asesores = usuarios.Where(u => u.UsuarioRoles.Any(ur => ur.Rol.Nombre == "ASESOR_LEGAL")).ToList();
        ViewBag.Abogados = usuarios.Where(u => u.UsuarioRoles.Any(ur => ur.Rol.Nombre == "ABOGADO")).ToList();

        // Juzgados/fiscalías ya usados en procesos existentes, sin repetir,
        // para sugerirlos como autocompletado en vez de repetir texto libre.
        ViewBag.Juzgados = await _context.Procesos
            .Where(p => p.JuzgadoFiscalia != null && p.JuzgadoFiscalia != "")
            .Select(p => p.JuzgadoFiscalia!.Trim())
            .Distinct()
            .OrderBy(j => j)
            .ToListAsync();

        ViewBag.Estados = Enum.GetValues<EstadoProceso>();
        ViewBag.Prioridades = Enum.GetValues<PrioridadProceso>();
    }
}
