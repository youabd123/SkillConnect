using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.WebUtilities;

namespace SkillConnect.Api.Accounts;

public interface IConfirmationEmailSender
{
    Task SendAsync(string email, Guid userId, string code);
}

public sealed class ConfirmationEmailSender(
    IConfiguration configuration, IWebHostEnvironment environment, ILogger<ConfirmationEmailSender> logger)
    : IConfirmationEmailSender
{
    public async Task SendAsync(string email, Guid userId, string code)
    {
        var settings = configuration.GetSection("Accounts");
        var origin = settings["PublicOrigin"] ?? (environment.IsDevelopment() ? "http://localhost:5173" : null);
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var publicOrigin) ||
            (publicOrigin.Scheme != Uri.UriSchemeHttps &&
                !(environment.IsDevelopment() && publicOrigin.Scheme == Uri.UriSchemeHttp)))
        {
            throw new InvalidOperationException("Set Accounts:PublicOrigin to the public frontend origin.");
        }

        var link = QueryHelpers.AddQueryString(new Uri(publicOrigin, "/confirm-email").AbsoluteUri,
            new Dictionary<string, string?> { ["userId"] = userId.ToString(), ["code"] = code });
        var host = settings["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host) && environment.IsDevelopment())
        {
            logger.LogInformation("Development email confirmation link: {ConfirmationLink}", link);
            return;
        }

        var from = settings["Smtp:From"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
        {
            throw new InvalidOperationException("Set Accounts:Smtp:Host and Accounts:Smtp:From to send confirmation emails.");
        }

        using var message = new MailMessage(from, email, "Bekräfta din e-postadress för SkillConnect",
            $"Öppna länken för att bekräfta din e-postadress:\n\n{link}");
        using var smtp = new SmtpClient(host, settings.GetValue("Smtp:Port", 587)) { EnableSsl = true };
        if (settings["Smtp:Username"] is { Length: > 0 } username)
        {
            smtp.Credentials = new NetworkCredential(username, settings["Smtp:Password"]);
        }

        await smtp.SendMailAsync(message);
    }
}
