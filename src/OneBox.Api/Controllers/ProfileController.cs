using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using Microsoft.EntityFrameworkCore;using OneBox.Api.Data;using OneBox.Api.Models;
namespace OneBox.Api.Controllers;
[Authorize,ApiController,Route("api/profile")]
public sealed class ProfileController(OneBoxDb db):ControllerBase{
 Guid UserId=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 [HttpGet]public async Task<IActionResult> Get(CancellationToken ct){var p=await db.UserProfiles.SingleOrDefaultAsync(x=>x.UserId==UserId,ct);return Ok(p is null?new ProfileResponse(null,null,null,null,null,null,"en",null,null):ToResponse(p));}
 [HttpPut]public async Task<IActionResult> Update(ProfileRequest r,CancellationToken ct){
  if(r.PostalCode?.Length>20||r.Country?.Length>100||r.PreferredLanguage?.Length>10)return BadRequest(new{message="Profile field is too long."});
  var p=await db.UserProfiles.SingleOrDefaultAsync(x=>x.UserId==UserId,ct);if(p is null){p=new UserProfile{UserId=UserId};db.UserProfiles.Add(p);}
  p.AddressLine1=C(r.AddressLine1);p.AddressLine2=C(r.AddressLine2);p.City=C(r.City);p.State=C(r.State);p.PostalCode=C(r.PostalCode);p.Country=C(r.Country);p.PreferredLanguage=C(r.PreferredLanguage)??"en";p.EmergencyContactName=C(r.EmergencyContactName);p.EmergencyContactPhone=C(r.EmergencyContactPhone);p.UpdatedAt=DateTime.UtcNow;
  db.AuditEvents.Add(new AuditEvent{UserId=UserId,EventType="PROFILE_UPDATED",Detail="User profile updated."});await db.SaveChangesAsync(ct);return Ok(ToResponse(p));
 }
 [HttpPost("autofill")]public async Task<IActionResult> Autofill(AutofillRequest r,CancellationToken ct){
  var p=await db.UserProfiles.SingleOrDefaultAsync(x=>x.UserId==UserId,ct);if(p is null)return Ok(new{fields=new Dictionary<string,string?>(),usedProfile=false});
  var allowed=new Dictionary<string,string?>(StringComparer.OrdinalIgnoreCase){{"addressLine1",p.AddressLine1},{"addressLine2",p.AddressLine2},{"city",p.City},{"state",p.State},{"postalCode",p.PostalCode},{"country",p.Country},{"preferredLanguage",p.PreferredLanguage},{"emergencyContactName",p.EmergencyContactName},{"emergencyContactPhone",p.EmergencyContactPhone}};
  var fields=new Dictionary<string,string?>(StringComparer.OrdinalIgnoreCase);foreach(var key in r.Fields??[])if(allowed.TryGetValue(key,out var value)&&!string.IsNullOrWhiteSpace(value))fields[key]=value;
  db.AuditEvents.Add(new AuditEvent{UserId=UserId,EventType="PROFILE_AUTOFILL_USED",Detail=$"Autofilled {fields.Count} approved profile fields."});await db.SaveChangesAsync(ct);return Ok(new{fields,usedProfile:fields.Count>0});
 }
 static string? C(string? v)=>string.IsNullOrWhiteSpace(v)?null:v.Trim();static ProfileResponse ToResponse(UserProfile p)=>new(p.AddressLine1,p.AddressLine2,p.City,p.State,p.PostalCode,p.Country,p.PreferredLanguage,p.EmergencyContactName,p.EmergencyContactPhone);
}
public record ProfileRequest(string? AddressLine1,string? AddressLine2,string? City,string? State,string? PostalCode,string? Country,string? PreferredLanguage,string? EmergencyContactName,string? EmergencyContactPhone);
public record AutofillRequest(string[]? Fields);
public record ProfileResponse(string? AddressLine1,string? AddressLine2,string? City,string? State,string? PostalCode,string? Country,string? PreferredLanguage,string? EmergencyContactName,string? EmergencyContactPhone);