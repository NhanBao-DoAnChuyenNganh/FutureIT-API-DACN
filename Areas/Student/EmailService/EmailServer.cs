using System.Net.Mail;
using System.Net;

namespace DoAnCoSo_Web.Areas.Student.EmailService
{
    public class EmailServer
    {
        private readonly IConfiguration _configuration;
        public EmailServer(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public void SendEmail(string toEmail, string subject, string body)
        {

            var fromEmail = _configuration["Email:From"];
            var smtpServer = _configuration["Email:SmtpServer"];
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"]);
            var smtpUser = _configuration["Email:SmtpUser"];
            var smtpPass = _configuration["Email:SmtpPass"];
            using (var client = new SmtpClient(smtpServer, smtpPort))
            {
                client.Credentials = new NetworkCredential(smtpUser, smtpPass);
                client.EnableSsl = true;
                var mailMessage = new MailMessage(fromEmail, toEmail, subject, body);
                mailMessage.IsBodyHtml = true;
                client.Send(mailMessage);
            }
        }
    }
}
