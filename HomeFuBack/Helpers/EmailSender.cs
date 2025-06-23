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
        private readonly EmailSettings _smtpSettings; // Создайте класс SmtpSettings для привязки из appsettings

        public EmailSender(IOptions<EmailSettings> smtpSettings) // Или IConfiguration
        {
            _smtpSettings = smtpSettings.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_smtpSettings.SenderName, _smtpSettings.SenderEmail));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;
            email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = message };

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(_smtpSettings.SmtpServer, _smtpSettings.SmtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);
                await client.SendAsync(email);
                await client.DisconnectAsync(true);
            }
        }
    }
}
