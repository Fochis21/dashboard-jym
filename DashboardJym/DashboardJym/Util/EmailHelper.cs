using System.Net;
using System.Net.Mail;

namespace DashboardJym.Util;

// Envia correos por SMTP (equivalente a JavaMailSender de Spring). La
// configuracion se lee de la seccion "Smtp" de appsettings.json / variables
// de entorno; ver README para como generar una contraseña de aplicacion de
// Gmail si se usa ese proveedor.
public class EmailHelper
{
    private readonly IConfiguration _configuracion;
    private readonly ILogger<EmailHelper> _logger;

    public EmailHelper(IConfiguration configuracion, ILogger<EmailHelper> logger)
    {
        _configuracion = configuracion;
        _logger = logger;
    }

    public async Task EnviarCorreoAsync(string destinatario, string asunto, string cuerpoHtml)
    {
        var host = _configuracion["Smtp:Host"];
        var puertoTexto = _configuracion["Smtp:Puerto"];
        var usuario = _configuracion["Smtp:Usuario"];
        var password = _configuracion["Smtp:Password"];
        var nombreRemitente = _configuracion["Smtp:NombreRemitente"] ?? "Estudio Jurídico JYM";
        var usarSsl = bool.TryParse(_configuracion["Smtp:UsarSsl"], out var ssl) ? ssl : true;

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(password))
        {
            // Falta configurar el SMTP en appsettings/variables de entorno.
            // Se registra en el log en vez de lanzar una excepcion que
            // exponga detalles al usuario final.
            _logger.LogError("No se pudo enviar el correo a {Destinatario}: falta configurar Smtp:Host, Smtp:Usuario o Smtp:Password.", destinatario);
            throw new InvalidOperationException("El servidor de correo no está configurado.");
        }

        var puerto = int.TryParse(puertoTexto, out var p) ? p : 587;

        using var mensaje = new MailMessage
        {
            From = new MailAddress(usuario, nombreRemitente),
            Subject = asunto,
            Body = cuerpoHtml,
            IsBodyHtml = true,
        };
        mensaje.To.Add(destinatario);

        using var cliente = new SmtpClient(host, puerto)
        {
            EnableSsl = usarSsl,
            Credentials = new NetworkCredential(usuario, password),
        };

        await cliente.SendMailAsync(mensaje);
    }
}
