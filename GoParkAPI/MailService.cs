using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
public class MailService
{
    private readonly IConfiguration _configuration;
    public MailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    public async Task SendEmailAsync(string Email, string subject, string body)
    {
        try
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress("MyGoParking團隊", _configuration["EmailSettings:SenderEmail"]));   
            email.To.Add(MailboxAddress.Parse(Email));
            email.Subject = subject;
            email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = body };
            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_configuration["EmailSettings:SmtpServer"],
            int.Parse(_configuration["EmailSettings:SmtpPort"]), SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_configuration["EmailSettings:Username"], _configuration["EmailSettings:Password"]);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            throw;
        }
    }
    // 新增模板讀取和佔位符替換方法
    public async Task<string> LoadEmailTemplateAsync(string templatePath, Dictionary<string, string> placeholders)
    {
        // 確認模板文件的完整路徑
        string fullTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), templatePath);

        // 讀取 HTML 模板內容
        string emailBody = await File.ReadAllTextAsync(fullTemplatePath);

        // 替換模板中的佔位符
        foreach (var placeholder in placeholders)
        {
            // 構造佔位符名稱，例如將 "username" 轉為 "{{username}}"
            string placeholderKey = $"{{{{{placeholder.Key}}}}}";
            emailBody = emailBody.Replace(placeholderKey, placeholder.Value);
        }

        return emailBody;
    }

}
