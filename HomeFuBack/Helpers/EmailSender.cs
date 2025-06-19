using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using HomeFuBack.Models;
using Microsoft.Extensions.Options;
using HomeFuBack.Models.Users;
using HomeFuBack.Helpers.Interfaces;

namespace HomeFuBack.Helpers
{
    public class EmailSender : IEmailSender
    {
        private readonly EmailSettings _emailSettings;

        public EmailSender(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;
            email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = message }; // Или TextFormat.Plain для обычного текста

            using var smtp = new SmtpClient();
            try
            {
                // Подключение
                await smtp.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, _emailSettings.EnableSsl);

                // Аутентификация
                // SmtpSecurity.StartTlsWhenAvailable или SmtpSecurity.SslOnConnect
                // SecureSocketOptions.Auto будет пытаться определить автоматически
                await smtp.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);

                // Отправка письма
                await smtp.SendAsync(email);
            }
            catch (Exception ex)
            {
                // TODO: Залогируйте ошибку здесь.
                Console.WriteLine($"Error sending email to {toEmail}: {ex.Message}");
                // В продакшене лучше использовать полноценный логгер
                throw; // Перебросить исключение, чтобы контроллер мог его обработать
            }
            finally
            {
                await smtp.DisconnectAsync(true);
            }
        }
    }
}
