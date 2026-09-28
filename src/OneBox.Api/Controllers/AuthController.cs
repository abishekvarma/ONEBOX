using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;using Microsoft.AspNetCore.RateLimiting;using Microsoft.AspNetCore.Mvc;using Microsoft.EntityFrameworkCore;using OneBox.Api.Data;using OneBox.Api.Models;using OneBox.Api.Security;
namespace OneBox.Api.Controllers;
[ApiController,Route("api/auth"),EnableRateLimiting("auth")]
public sealed class AuthController(OneBoxDb db,IPasswordHasher<AppUser> hasher,IConfiguration cfg,OtpDeliveryService otpDelivery):ControllerBase{
 [HttpPost("register")]public async Task<IActionResult> Register(RegisterRequest r,CancellationToken ct){
  if(string.IsNullOrWhiteSpace(r.Name)||string.IsNullOrWhiteSpace(r.Email)||r.Password.Length<8||string.IsNullOrWhiteSpace(r.Phone))return BadRequest(new{message="Name, email, phone and an 8+ character password are required."});
  var email=r.Email.Trim().ToLowerInvariant(); var phone=r.Phone.Trim();
  if(await db.Users.AnyAsync(x=>x.Email==email,ct))return Conflict(new{message="Email already registered."});
  var u=new AppUser{Name=r.Name.Trim(),Email=email,Phone=phone,Role="USER",PhoneVerified=false};u.PasswordHash=hasher.HashPassword(u,r.Password);IssueOtp(u);db.Users.Add(u);await db.SaveChangesAsync(ct);await otpDelivery.SendAsync(email,phone,u.TempOtp!,ct);
  var response=new{message=cfg["OTP:Provider"] is "resend" or "email" ? "Account created. A verification code was sent to your email." : "Registration created. Verify the mobile OTP before login."};
  if(cfg.GetValue<bool>("Security:ExposeOtpInDevelopment") && !builderEnvironmentIsProduction()) return Ok(new{response,developmentOtp=GetDevelopmentOtp(u)});
  return Ok(response);
 }
 [HttpPost("verify-otp")]public async Task<IActionResult> VerifyOtp(VerifyOtpRequest r,CancellationToken ct){
  var email=r.Email.Trim().ToLowerInvariant();var u=await db.Users.SingleOrDefaultAsync(x=>x.Email==email,ct);if(u is null)return BadRequest(new{message="Invalid verification request."});
  if(u.PhoneVerified)return Ok(new{message="Phone already verified."});
  if(u.OtpExpiresAt is null||u.OtpExpiresAt<DateTime.UtcNow)return BadRequest(new{message="OTP expired. Request a new OTP."});
  if(u.OtpAttempts>=5)return BadRequest(new{message="Too many OTP attempts. Request a new OTP."});
  u.OtpAttempts++;if(!VerifyOtpHash(u.OtpHash,r.Otp)){await db.SaveChangesAsync(ct);return BadRequest(new{message="Invalid OTP."});}
  u.PhoneVerified=true;u.OtpHash=null;u.OtpExpiresAt=null;u.OtpAttempts=0;await db.SaveChangesAsync(ct);var token=Jwt.Create(u,cfg["Jwt:Key"]!);return Ok(new{message="Mobile number verified. You are now signed in.",token,user=Dto(u)});
 }
 [HttpPost("resend-otp")]public async Task<IActionResult> ResendOtp(ResendOtpRequest r,CancellationToken ct){
  var email=r.Email.Trim().ToLowerInvariant();var u=await db.Users.SingleOrDefaultAsync(x=>x.Email==email,ct);if(u is null)return Ok(new{message="If the account exists, a new OTP has been requested."});
  if(u.PhoneVerified)return Ok(new{message="Mobile number already verified."});IssueOtp(u);await db.SaveChangesAsync(ct);await otpDelivery.SendAsync(email,u.Phone,u.TempOtp!,ct);var response=new{message="If the account exists, a new OTP has been requested."};if(cfg.GetValue<bool>("Security:ExposeOtpInDevelopment")&&!builderEnvironmentIsProduction())return Ok(new{response,developmentOtp=GetDevelopmentOtp(u)});return Ok(response);
 }
 [HttpPost("login")]public async Task<IActionResult> Login(LoginRequest r,CancellationToken ct){
  var u=await db.Users.SingleOrDefaultAsync(x=>x.Email==r.Email.Trim().ToLowerInvariant(),ct);if(u is null||hasher.VerifyHashedPassword(u,u.PasswordHash,r.Password)==PasswordVerificationResult.Failed)return Unauthorized(new{message="Invalid email or password."});
  if(!u.PhoneVerified)return StatusCode(403,new{code="PHONE_NOT_VERIFIED",message="Verify your mobile number before logging in."});
  return Ok(new{token=Jwt.Create(u,cfg["Jwt:Key"]!),user=Dto(u)});
 }
 static void IssueOtp(AppUser u){var otp=RandomNumberGenerator.GetInt32(100000,1000000).ToString();u.OtpHash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(otp))).ToLowerInvariant();u.OtpExpiresAt=DateTime.UtcNow.AddMinutes(5);u.OtpAttempts=0;u.TempOtp=otp;}
 static bool VerifyOtpHash(string? hash,string otp)=>!string.IsNullOrWhiteSpace(hash)&&CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(hash),Encoding.UTF8.GetBytes(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(otp))).ToLowerInvariant()));
 static string GetDevelopmentOtp(AppUser u)=>u.TempOtp??"";
 static bool builderEnvironmentIsProduction()=>string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),"Production",StringComparison.OrdinalIgnoreCase);
 static object Dto(AppUser u)=>new{u.Id,u.Name,u.Email,u.Phone,u.Role,u.PhoneVerified,u.Latitude,u.Longitude,u.LocationAllowed};
}
public record RegisterRequest(string Name,string Email,string Password,string? Phone);public record LoginRequest(string Email,string Password);public record VerifyOtpRequest(string Email,string Otp);public record ResendOtpRequest(string Email);
