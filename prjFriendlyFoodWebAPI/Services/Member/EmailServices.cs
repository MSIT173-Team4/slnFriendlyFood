using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.Models;
using System.Net;
using System.Net.Mail;

namespace prjFriendlyFoodWebAPI.Services.Member
{
    public class EmailServices
    {
        private readonly IConfiguration _config;
        private readonly FriendlyFoodDbContext _db;
        public EmailServices(IConfiguration configuration,FriendlyFoodDbContext db)
        {
            _db = db;
            _config = configuration;
        
        }
        public async Task SendEmailAsync(
        string toEmail,
        string subject,
        string body)
        {
            string smtpServer =
                _config["EmailSettings:SmtpServer"]!;

            int port =
                int.Parse(_config["EmailSettings:Port"]!);

            string username =
                _config["EmailSettings:Username"]!;

            string password =
                _config["EmailSettings:Password"]!;

            string senderEmail =
                _config["EmailSettings:SenderEmail"]!;

            using var smtp = new SmtpClient(smtpServer, port);

            smtp.EnableSsl = true;

            smtp.Credentials =
                new NetworkCredential(username, password);

            using var mail = new MailMessage();

            mail.From = new MailAddress(senderEmail);
            mail.To.Add(toEmail);
            mail.Subject = subject;
            mail.Body = body;
            mail.IsBodyHtml = true;

            await smtp.SendMailAsync(mail);
        }
        public async Task EmailVerify(string token)
        {
            TEmailVerification? tokenData =
                await _db.TEmailVerifications
                    .FirstOrDefaultAsync(e =>
                        e.FToken == token &&
                        e.FType == "EmailVerification"
                    );

            if (tokenData == null)
            {
                throw new ArgumentException("無效的驗證連結");
            }

            if (tokenData.FUsed)
            {
                throw new ArgumentException("此驗證連結已使用");
            }

            if (tokenData.FExpireAt < DateTime.UtcNow)
            {
                throw new ArgumentException("驗證連結已過期");
            }

            TUser? user =
                await _db.TUsers
                    .FirstOrDefaultAsync(e =>
                        e.FId == tokenData.FUserId
                    );

            if (user == null)
            {
                throw new ArgumentException("找不到會員");
            }

            if (!user.FIsActive)
            {
                user.FIsActive = true;
            }

            tokenData.FUsed = true;

            await _db.SaveChangesAsync();


        }
    }
}
