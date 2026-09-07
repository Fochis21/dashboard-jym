using System.Security.Claims;
using DashboardJym.Data;
using DashboardJym.Models;
using DashboardJym.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DashboardJym.Controllers;

public class AuthController : Controller
{
    // Los dos roles disponibles en el autorregistro. Ambos tienen exactamente
    // los mismos permisos dentro del sistema (equivalente a que en Java las
    // rutas /app/** se autoricen igual sin distinguir el rol).
    private static readonly string[] RolesPermitidos = { "ABOGADO", "ASESOR_LEGAL" };

    private readonly AppDbContext _context;
    private readonly IConfiguration _configuracion;

    public AuthController(AppDbContext context, IConfiguration configuracion)
    {
        _context = context;
        _configuracion = configuracion;
    }

    [HttpGet("/auth/login")]
    [AllowAnonymous]
    public IActionResult Login(bool registrado = false, bool error = false)
    {
        ViewData["Registrado"] = registrado;
        ViewData["Error"] = error;
        return View(new LoginViewModel());
    }

    [HttpPost("/auth/login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var usuario = await _context.Usuarios
            .Include(u => u.UsuarioRoles).ThenInclude(ur => ur.Rol)
            .FirstOrDefaultAsync(u => u.Correo == modelo.Correo);

        // Misma validacion que hacia CustomUserDetailsService: usuario debe
        // existir, estar activo, y la contraseña debe coincidir con el hash.
        if (usuario == null || !usuario.Estado ||
            !BCrypt.Net.BCrypt.Verify(modelo.Password, usuario.Password))
        {
            return RedirectToAction(nameof(Login), new { error = true });
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Correo),
            new(ClaimTypes.GivenName, usuario.NombreCompleto),
        };
        claims.AddRange(usuario.UsuarioRoles.Select(ur => new Claim(ClaimTypes.Role, ur.Rol.Nombre)));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return RedirectToAction("Index", "Home");
    }

    [HttpGet("/auth/registro")]
    [AllowAnonymous]
    public IActionResult Registro()
    {
        return View(new RegistroViewModel());
    }

    [HttpPost("/auth/registro")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registro(RegistroViewModel modelo)
    {
        // Bloquea el autorregistro a cualquiera de internet: el codigo debe
        // coincidir con el configurado en el servidor (variable de entorno
        // "CodigoInvitacion"), que solo conoce el equipo del estudio.
        var codigoValido = _configuracion["CodigoInvitacion"];
        if (string.IsNullOrEmpty(codigoValido) || modelo.CodigoInvitacion != codigoValido)
        {
            ModelState.AddModelError(nameof(modelo.CodigoInvitacion), "Código de invitación inválido.");
        }

        if (!RolesPermitidos.Contains(modelo.Rol))
        {
            ModelState.AddModelError(nameof(modelo.Rol), "Selecciona un rol válido.");
        }

        if (await _context.Usuarios.AnyAsync(u => u.Correo == modelo.Correo))
        {
            ModelState.AddModelError(nameof(modelo.Correo), "Ya existe una cuenta con este correo.");
        }

        if (await _context.Usuarios.AnyAsync(u => u.Dni == modelo.Dni))
        {
            ModelState.AddModelError(nameof(modelo.Dni), "Ya existe una cuenta con este DNI.");
        }

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var rol = await _context.Roles.FirstAsync(r => r.Nombre == modelo.Rol);

        var usuario = new Usuario
        {
            Nombres = modelo.Nombres,
            Apellidos = modelo.Apellidos,
            Dni = modelo.Dni,
            Telefono = modelo.Telefono,
            Correo = modelo.Correo,
            Password = BCrypt.Net.BCrypt.HashPassword(modelo.Password),
            Estado = true,
            FechaCreacion = DateTime.Now,
            FechaActualizacion = DateTime.Now,
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        _context.UsuariosRoles.Add(new UsuarioRol { UsuarioId = usuario.Id, RolId = rol.Id });
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Login), new { registrado = true });
    }

    [HttpPost("/logout")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }
}
