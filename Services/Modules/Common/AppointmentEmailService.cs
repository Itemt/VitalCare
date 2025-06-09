using CitasEPS.Models;
using CitasEPS.Models.Modules.Users;
using CitasEPS.Models.Modules.Appointments;
using CitasEPS.Models.Modules.Common;
using CitasEPS.Models.Modules.Common;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Text;

namespace CitasEPS.Services.Modules.Common
{
    public class AppointmentEmailService : IAppointmentEmailService
    {
        private readonly IEmailSender _emailSender;

        public AppointmentEmailService(IEmailSender emailSender)
        {
            _emailSender = emailSender;
        }

        public async Task SendAppointmentCreatedEmailAsync(Appointment appointment, User patient, User doctor)
        {
            var subject = "Cita Médica Agendada - VitalCare";
            var htmlMessage = GenerateAppointmentEmailHtml(
                "Cita Médica Agendada",
                $"Su cita médica ha sido agendada exitosamente.",
                appointment,
                patient,
                doctor,
                "Su cita está pendiente de confirmación por parte del médico."
            );

            await _emailSender.SendEmailAsync(patient.Email!, subject, htmlMessage);
        }

        public async Task SendAppointmentConfirmedEmailAsync(Appointment appointment, User patient, User doctor)
        {
            var subject = "Cita Médica Confirmada - VitalCare";
            var htmlMessage = GenerateAppointmentEmailHtml(
                "Cita Médica Confirmada",
                $"Su cita médica ha sido confirmada por el Dr. {doctor.FirstName} {doctor.LastName}.",
                appointment,
                patient,
                doctor,
                "Por favor, llegue puntualmente a su cita. Recuerde traer sus documentos de identidad y cualquier examen médico previo."
            );

            await _emailSender.SendEmailAsync(patient.Email!, subject, htmlMessage);
        }

        public async Task SendAppointmentModifiedEmailAsync(Appointment appointment, User patient, User doctor, string modificationType)
        {
            var subject = "Cita Médica Modificada - VitalCare";
            var htmlMessage = GenerateAppointmentEmailHtml(
                "Cita Médica Modificada",
                $"Su cita médica ha sido modificada. Tipo de modificación: {modificationType}",
                appointment,
                patient,
                doctor,
                "Por favor, tome nota de los nuevos detalles de su cita."
            );

            await _emailSender.SendEmailAsync(patient.Email!, subject, htmlMessage);
        }

        public async Task SendAppointmentCancelledEmailAsync(Appointment appointment, User patient, User doctor)
        {
            var subject = "Cita Médica Cancelada - VitalCare";
            var htmlMessage = GenerateAppointmentEmailHtml(
                "Cita Médica Cancelada",
                $"Su cita médica ha sido cancelada.",
                appointment,
                patient,
                doctor,
                "Si necesita reagendar, puede hacerlo a través de nuestro sistema o contactando directamente con nosotros."
            );

            await _emailSender.SendEmailAsync(patient.Email!, subject, htmlMessage);
        }

        public async Task SendAppointmentReminderEmailAsync(Appointment appointment, User patient, User doctor)
        {
            var subject = "Recordatorio de Cita Médica - VitalCare";
            var htmlMessage = GenerateAppointmentEmailHtml(
                "Recordatorio de Cita Médica",
                $"Le recordamos que tiene una cita médica programada.",
                appointment,
                patient,
                doctor,
                "Por favor, no olvide asistir a su cita. Llegue puntualmente y traiga sus documentos."
            );

            await _emailSender.SendEmailAsync(patient.Email!, subject, htmlMessage);
        }

        public async Task SendRescheduleRequestedEmailAsync(Appointment appointment, User patient, User doctor)
        {
            var subject = "Solicitud de Reagendamiento Enviada - VitalCare";
            var htmlMessage = GenerateAppointmentEmailHtml(
                "Solicitud de Reagendamiento Enviada",
                $"Su solicitud de reagendamiento ha sido enviada al Dr. {doctor.FirstName} {doctor.LastName}.",
                appointment,
                patient,
                doctor,
                "El médico revisará su solicitud y le notificaremos cuando haya una respuesta. Mientras tanto, su cita mantiene el horario original."
            );

            await _emailSender.SendEmailAsync(patient.Email!, subject, htmlMessage);
        }

        public async Task SendRescheduleApprovedEmailAsync(Appointment appointment, User patient, User doctor)
        {
            var subject = "Reagendamiento Aprobado - VitalCare";
            var htmlMessage = GenerateAppointmentEmailHtml(
                "Reagendamiento Aprobado",
                $"¡Buenas noticias! El Dr. {doctor.FirstName} {doctor.LastName} ha aprobado su solicitud de reagendamiento.",
                appointment,
                patient,
                doctor,
                "Su cita ha sido confirmada en el nuevo horario. Por favor, llegue puntualmente a su nueva cita."
            );

            await _emailSender.SendEmailAsync(patient.Email!, subject, htmlMessage);
        }

        public async Task SendRescheduleRejectedEmailAsync(Appointment appointment, User patient, User doctor, DateTime originalDateTime)
        {
            var subject = "Solicitud de Reagendamiento No Aprobada - VitalCare";
            var htmlMessage = GenerateRescheduleRejectedEmailHtml(
                "Solicitud de Reagendamiento No Aprobada",
                $"Le informamos que el Dr. {doctor.FirstName} {doctor.LastName} no pudo aprobar su solicitud de reagendamiento.",
                appointment,
                patient,
                doctor,
                originalDateTime,
                "Su cita mantiene el horario original. Si necesita reagendar, puede intentar con una nueva propuesta o contactar directamente con el consultorio."
            );

            await _emailSender.SendEmailAsync(patient.Email!, subject, htmlMessage);
        }

        private string GenerateAppointmentEmailHtml(string title, string message, Appointment appointment, User patient, User doctor, string additionalInfo)
        {
            var html = new StringBuilder();
            
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang='es'>");
            html.AppendLine("<head>");
            html.AppendLine("    <meta charset='UTF-8'>");
            html.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1.0'>");
            html.AppendLine($"    <title>{title}</title>");
            html.AppendLine("    <style>");
            html.AppendLine("        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f4f4f4; }");
            html.AppendLine("        .container { max-width: 600px; margin: 0 auto; background-color: #ffffff; box-shadow: 0 0 10px rgba(0,0,0,0.1); }");
            html.AppendLine("        .header { background: linear-gradient(135deg, #007bff 0%, #0056b3 100%); color: white; padding: 20px; text-align: center; }");
            html.AppendLine("        .header h1 { margin: 0; font-size: 24px; }");
            html.AppendLine("        .content { padding: 30px; }");
            html.AppendLine("        .message { font-size: 16px; margin-bottom: 20px; }");
            html.AppendLine("        .appointment-details { background-color: #f8f9fa; border-left: 4px solid #007bff; padding: 20px; margin: 20px 0; }");
            html.AppendLine("        .detail-item { margin-bottom: 10px; }");
            html.AppendLine("        .detail-label { font-weight: bold; color: #007bff; }");
            html.AppendLine("        .footer { background-color: #f8f9fa; padding: 20px; text-align: center; font-size: 14px; color: #666; }");
            html.AppendLine("        .logo { font-size: 20px; font-weight: bold; }");
            html.AppendLine("        .logo .vital { color: #ffffff; }");
            html.AppendLine("        .logo .care { color: #17a2b8; }");
            html.AppendLine("    </style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine("    <div class='container'>");
            html.AppendLine("        <div class='header'>");
            html.AppendLine("            <div class='logo'>");
            html.AppendLine("                <span class='vital'>❤️ Vital</span><span class='care'>Care</span>");
            html.AppendLine("            </div>");
            html.AppendLine($"            <h1>{title}</h1>");
            html.AppendLine("        </div>");
            html.AppendLine("        <div class='content'>");
            html.AppendLine($"            <p class='message'>Estimado/a {patient.FirstName} {patient.LastName},</p>");
            html.AppendLine($"            <p class='message'>{message}</p>");
            html.AppendLine("            <div class='appointment-details'>");
            html.AppendLine("                <h3 style='margin-top: 0; color: #007bff;'>Detalles de la Cita</h3>");
            html.AppendLine($"                <div class='detail-item'><span class='detail-label'>Fecha:</span> {appointment.AppointmentDateTime:dddd, dd 'de' MMMM 'de' yyyy}</div>");
            html.AppendLine($"                <div class='detail-item'><span class='detail-label'>Hora:</span> {appointment.AppointmentDateTime:HH:mm}</div>");
            html.AppendLine($"                <div class='detail-item'><span class='detail-label'>Médico:</span> Dr. {doctor.FirstName} {doctor.LastName}</div>");
            html.AppendLine($"                <div class='detail-item'><span class='detail-label'>Estado:</span> {GetStatusText(appointment)}</div>");
            html.AppendLine($"                <div class='detail-item'><span class='detail-label'>Motivo:</span> {appointment.Notes ?? "No especificado"}</div>");
            html.AppendLine("            </div>");
            html.AppendLine($"            <p class='message'>{additionalInfo}</p>");
            html.AppendLine("            <p class='message'>Si tiene alguna pregunta o necesita realizar cambios, no dude en contactarnos.</p>");
            html.AppendLine("        </div>");
            html.AppendLine("        <div class='footer'>");
            html.AppendLine("            <p><strong>VitalCare - Sistema de Gestión de Citas Médicas</strong></p>");
            html.AppendLine("            <p>Este es un mensaje automático, por favor no responda a este correo.</p>");
            html.AppendLine("        </div>");
            html.AppendLine("    </div>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }

        private string GenerateRescheduleRejectedEmailHtml(string title, string message, Appointment appointment, User patient, User doctor, DateTime originalDateTime, string additionalInfo)
        {
            var html = new StringBuilder();
            
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang='es'>");
            html.AppendLine("<head>");
            html.AppendLine("    <meta charset='UTF-8'>");
            html.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1.0'>");
            html.AppendLine($"    <title>{title}</title>");
            html.AppendLine("    <style>");
            html.AppendLine("        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f4f4f4; }");
            html.AppendLine("        .container { max-width: 600px; margin: 0 auto; background-color: #ffffff; box-shadow: 0 0 10px rgba(0,0,0,0.1); }");
            html.AppendLine("        .header { background: linear-gradient(135deg, #dc3545 0%, #c82333 100%); color: white; padding: 20px; text-align: center; }");
            html.AppendLine("        .header h1 { margin: 0; font-size: 24px; }");
            html.AppendLine("        .content { padding: 30px; }");
            html.AppendLine("        .message { font-size: 16px; margin-bottom: 20px; }");
            html.AppendLine("        .appointment-details { background-color: #f8f9fa; border-left: 4px solid #dc3545; padding: 20px; margin: 20px 0; }");
            html.AppendLine("        .detail-item { margin-bottom: 10px; }");
            html.AppendLine("        .detail-label { font-weight: bold; color: #dc3545; }");
            html.AppendLine("        .footer { background-color: #f8f9fa; padding: 20px; text-align: center; font-size: 14px; color: #666; }");
            html.AppendLine("        .logo { font-size: 20px; font-weight: bold; }");
            html.AppendLine("        .logo .vital { color: #ffffff; }");
            html.AppendLine("        .logo .care { color: #17a2b8; }");
            html.AppendLine("    </style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine("    <div class='container'>");
            html.AppendLine("        <div class='header'>");
            html.AppendLine("            <div class='logo'>");
            html.AppendLine("                <span class='vital'>❤️ Vital</span><span class='care'>Care</span>");
            html.AppendLine("            </div>");
            html.AppendLine($"            <h1>{title}</h1>");
            html.AppendLine("        </div>");
            html.AppendLine("        <div class='content'>");
            html.AppendLine($"            <p class='message'>Estimado/a {patient.FirstName} {patient.LastName},</p>");
            html.AppendLine($"            <p class='message'>{message}</p>");
            html.AppendLine("            <div class='appointment-details'>");
            html.AppendLine("                <h3 style='margin-top: 0; color: #dc3545;'>Detalles de la Cita (Horario Original Mantenido)</h3>");
            html.AppendLine($"                <div class='detail-item'><span class='detail-label'>Fecha:</span> {originalDateTime:dddd, dd 'de' MMMM 'de' yyyy}</div>");
            html.AppendLine($"                <div class='detail-item'><span class='detail-label'>Hora:</span> {originalDateTime:HH:mm}</div>");
            html.AppendLine($"                <div class='detail-item'><span class='detail-label'>Médico:</span> Dr. {doctor.FirstName} {doctor.LastName}</div>");
            html.AppendLine($"                <div class='detail-item'><span class='detail-label'>Estado:</span> Pendiente de Confirmación</div>");
            html.AppendLine($"                <div class='detail-item'><span class='detail-label'>Motivo:</span> {appointment.Notes ?? "No especificado"}</div>");
            html.AppendLine("            </div>");
            html.AppendLine($"            <p class='message'>{additionalInfo}</p>");
            html.AppendLine("            <p class='message'>Si tiene alguna pregunta o necesita realizar cambios, no dude en contactarnos.</p>");
            html.AppendLine("        </div>");
            html.AppendLine("        <div class='footer'>");
            html.AppendLine("            <p><strong>VitalCare - Sistema de Gestión de Citas Médicas</strong></p>");
            html.AppendLine("            <p>Este es un mensaje automático, por favor no responda a este correo.</p>");
            html.AppendLine("        </div>");
            html.AppendLine("    </div>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }

        private string GetStatusText(Appointment appointment)
        {
            if (appointment.IsCancelled)
                return "Cancelada";
            if (appointment.IsCompleted)
                return "Completada";
            if (appointment.IsConfirmed)
                return "Confirmada";
            if (appointment.RescheduleRequested || appointment.DoctorProposedReschedule)
                return "Pendiente de Reagendamiento";
            
            return "Agendada";
        }
    }
} 




