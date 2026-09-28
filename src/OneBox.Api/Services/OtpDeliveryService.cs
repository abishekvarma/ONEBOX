using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
namespace OneBox.Api.Services;

public sealed class OtpDeliveryService(IHttpClientFactory clients,IConfiguration cfg)
{
    public async Task<bool> SendAsync(string email,string? phone,string otp,CancellationToken ct)
    {
        var provider=cfg["OTP:Provider"]?.Trim().ToLowerInvariant()??"development";
        if(provider=="resend"||provider=="email")
        {
            var key=cfg["OTP:ResendApiKey"];
            var from=cfg["OTP:FromEmail"];
            if(string.IsNullOrWhiteSpace(key)||string.IsNullOrWhiteSpace(from)) return false;
            using var req=new HttpRequestMessage(HttpMethod.Post,"https://api.resend.com/emails");
            req.Headers.Authorization=new AuthenticationHeaderValue("Bearer",key);
            req.Content=JsonContent.Create(new{from,to=new[]{email},subject="Your ONEBOX verification code",html=$"<p>Your ONEBOX verification code is <strong>{otp}</strong>.</p><p>This code expires in 5 minutes.</p>"});
            var res=await clients.CreateClient().SendAsync(req,ct);
            return res.IsSuccessStatusCode;
        }
        return false;
    }
}