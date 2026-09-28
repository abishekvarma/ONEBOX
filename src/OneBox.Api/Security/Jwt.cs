using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using OneBox.Api.Models;
namespace OneBox.Api.Security;
public static class Jwt {
 public static string Create(AppUser u,string key){
  var k=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
  var c=new[]{new Claim(JwtRegisteredClaimNames.Sub,u.Id.ToString()),new Claim(ClaimTypes.NameIdentifier,u.Id.ToString()),new Claim(ClaimTypes.Name,u.Name),new Claim(ClaimTypes.Email,u.Email),new Claim(ClaimTypes.Role,u.Role)};
  return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(issuer:"onebox",audience:"onebox-web",claims:c,notBefore:DateTime.UtcNow,expires:DateTime.UtcNow.AddMinutes(30),signingCredentials:new SigningCredentials(k,SecurityAlgorithms.HmacSha256)));
 }
}
