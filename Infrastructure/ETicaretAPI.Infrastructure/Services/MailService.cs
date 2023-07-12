using Azure.Core;
using ETicaretAPI.Application.Abstractions.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace ETicaretAPI.Infrastructure.Services
{
    public class MailService : IMailService
    {
        readonly IConfiguration _configuration;

        public MailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendMailAsync(string to, string subject, string body, bool isBodyHtml = true)
        {
            await SendMailAsync(new[] { to }, subject, body, isBodyHtml);
        }

        public async Task SendMailAsync(string[] tos, string subject, string body, bool isBodyHtml = true)
        {
            //MailMessage mail = new();
            //mail.IsBodyHtml = isBodyHtml;
            //foreach (var to in tos)
            //    mail.To.Add(to);
            //mail.Subject = subject;
            //mail.Body = body;
            //mail.From = new(_configuration["Mail:Username"], "NG E-Ticaret", System.Text.Encoding.UTF8);

            //SmtpClient smtp = new();
            //smtp.Credentials = new NetworkCredential(_configuration["Mail:Username"], _configuration["Mail:Password"]);
            //smtp.Port = 587;
            //smtp.EnableSsl = true;
            //smtp.Host = _configuration["Mail:Host"];
            //await smtp.SendMailAsync(mail);

            using var message = new MailMessage();
            foreach (var to in tos)
            message.To.Add(to);
            message.From = new MailAddress("alperensazak@yandex.com");
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = true;

            using var client = new SmtpClient("smtp.yandex.com.tr", 587);
            client.EnableSsl = true;
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential("alperensazak@yandex.com", "mwntnpaedluuuatp");

            await client.SendMailAsync(message);

        }

        public async Task SendPasswordResetMailAsync(string to, string userId, string resetToken)
        {
            StringBuilder mail = new();

            mail.AppendLine("<html>\r\n        <body style='font-family: Arial, sans-serif; font-size: 14px; color: #000000;'>\r\n            <p style='color: #e50914;'>Merhaba,</p>\r\n            <p>Eğer Yeni Bir Şifre Talebinde Bulunduysanız Aşağıdaki Linkten Şifrenizi Yenileyebilirsiniz.</p>\r\n            <p></p>\r\n            <a href='http://localhost:4200/update-password/" + userId + "/" + resetToken + "' style='background-color: #e50914; border: none; color: #ffffff; padding: 10px 15px; text-align: center; text-decoration: none; display: inline-block; font-size: 16px; margin-bottom: 15px;'>Yeni Şifre İçin Tıklayınız...</a>\r\n            <p>Eğer Şifre Talebi Tarafınızca Gerçekleştirilmediyse Bu Maili Dikkate Almayınız.</p>\r\n            <p></p>\r\n            <p style='color: #e50914; font-weight: bold;'>Saygılarımızla...</p>\r\n        </body>\r\n    </html>");

            await SendMailAsync(to, "Şifre Yenileme Talebi", mail.ToString());
        }

        public async Task SendCompletedOrderMailAsync(string to, string orderCode, DateTime orderDate, string userName)
        {
            string mail = $"Sayın {userName} Merhaba<br>" +
                $"{orderDate} Tarihinde Vermiş Olduğunuz {orderCode} Kodlu Siparişiniz Tamamlanmış Ve Kargo Firmasına Verilmiştir.<br>Bizi Tercih Ettiğiniz İçin Teşekkür Ederiz. İyi Günler...";

            await SendMailAsync(to, $"{orderCode} Sipariş Numaralı Siparişiniz Tamamlandı", mail);
        }
    }
}
