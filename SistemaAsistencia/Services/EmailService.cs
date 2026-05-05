using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace SistemaAsistencia.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task EnviarAmonestacionAsync(
            string destinatario,
            string nombreTrabajador,
            string tipoAmonestacion,
            string motivo,
            int diasSuspension,
            string fechaEmision)
        {
            var mensaje = new MimeMessage();
            mensaje.From.Add(new MailboxAddress("SIATA - Talma", _config["Email:From"] ?? ""));
            mensaje.To.Add(new MailboxAddress(nombreTrabajador ?? "", destinatario ?? ""));
            mensaje.Subject = $"Notificacion de Amonestacion - {tipoAmonestacion}";

            var tipoLabel = tipoAmonestacion switch
            {
                "AVISO_ESCRITO" => "Aviso Escrito",
                "SUSPENSION_1D" => "Suspension 1 Dia",
                "SUSPENSION_2D" => "Suspension 2 Dias",
                "SUSPENSION_3D" => "Suspension 3 Dias",
                _ => tipoAmonestacion
            };

            var suspensionTexto = diasSuspension > 0
                ? $"<div class='info-row'><span class='info-label'>Dias de suspension:</span><span class='info-val'><strong>{diasSuspension} dia(s)</strong></span></div>"
                : "";

            var html = $@"<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'>
  <style>
    body {{ font-family: Arial, sans-serif; background: #f0f2f5; margin: 0; padding: 20px; }}
    .container {{ max-width: 600px; margin: 0 auto; background: white; border-radius: 12px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }}
    .header {{ background: #003087; padding: 24px; text-align: center; border-bottom: 4px solid #6aaa00; }}
    .header h1 {{ color: white; margin: 0; font-size: 22px; letter-spacing: 2px; }}
    .header p {{ color: #6aaa00; margin: 6px 0 0; font-size: 12px; }}
    .badge {{ display: inline-block; background: #7c1f1f; color: white; padding: 6px 18px; border-radius: 20px; font-size: 14px; font-weight: bold; margin: 20px auto; }}
    .body {{ padding: 28px 32px; }}
    .body h2 {{ color: #1a1a2e; font-size: 18px; margin-bottom: 16px; }}
    .info-row {{ border-bottom: 1px solid #f1f5f9; padding: 12px 0; display: flex; }}
    .info-label {{ font-weight: bold; color: #64748b; font-size: 13px; width: 160px; flex-shrink: 0; }}
    .info-val {{ color: #1e293b; font-size: 13px; }}
    .motivo-box {{ background: #fff5f5; border-left: 4px solid #dc2626; padding: 14px 16px; border-radius: 0 8px 8px 0; margin: 16px 0; }}
    .motivo-box p {{ margin: 0; color: #1e293b; font-size: 14px; }}
    .footer {{ background: #f8fafc; padding: 16px 32px; text-align: center; border-top: 1px solid #e2e8f0; }}
    .footer p {{ margin: 0; color: #94a3b8; font-size: 11px; }}
  </style>
</head>
<body>
  <div class='container'>
    <div class='header'>
      <h1>TALMA - SIATA</h1>
      <p>Sistema de Asistencia Talma Aeroportuario</p>
    </div>
    <div class='body'>
      <div style='text-align:center'>
        <span class='badge'>NOTIFICACION DE AMONESTACION</span>
      </div>
      <h2>Estimado/a {nombreTrabajador},</h2>
      <p style='color:#475569;font-size:14px'>Por medio del presente, se le notifica formalmente la siguiente amonestacion registrada en el sistema:</p>
      <div class='info-row'>
        <span class='info-label'>Tipo:</span>
        <span class='info-val'><strong>{tipoLabel}</strong></span>
      </div>
      <div class='info-row'>
        <span class='info-label'>Fecha de emision:</span>
        <span class='info-val'>{fechaEmision}</span>
      </div>
      {suspensionTexto}
      <div class='info-row' style='border:none'>
        <span class='info-label'>Motivo:</span>
      </div>
      <div class='motivo-box'>
        <p>{motivo}</p>
      </div>
      <p style='color:#475569;font-size:13px;margin-top:20px'>
        Le solicitamos tomar las medidas necesarias para evitar futuras incidencias.
        Si tiene alguna consulta, comuniquese con el area de Recursos Humanos.
      </p>
    </div>
    <div class='footer'>
      <p>Este correo fue generado automaticamente por SIATA - Talma Servicios Aeroportuarios</p>
    </div>
  </div>
</body>
</html>";

            var builder = new BodyBuilder { HtmlBody = html };
            mensaje.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_config["Email:From"] ?? "", _config["Email:Password"] ?? "");
            await smtp.SendAsync(mensaje);
            await smtp.DisconnectAsync(true);
        }
    }
}