using System.Net.Http.Headers;using System.Text.Json;
namespace OneBox.Api.Services;
public sealed class GooglePlacesService(IHttpClientFactory clients,IConfiguration cfg){
 public async Task<List<ProviderOption>> SearchAsync(string category,double lat,double lng,CancellationToken ct){
  var key=cfg["Google:ApiKey"];if(string.IsNullOrWhiteSpace(key))return [];
  var included=category switch{"RESTAURANT_BOOKING"=>"restaurant","HOSPITAL_APPOINTMENT"=>"hospital",_=>"establishment"};
  using var req=new HttpRequestMessage(HttpMethod.Post,"https://places.googleapis.com/v1/places:searchNearby");req.Headers.Add("X-Goog-Api-Key",key);req.Headers.Add("X-Goog-FieldMask","places.id,places.displayName,places.formattedAddress,places.location,places.nationalPhoneNumber,places.websiteUri");req.Content=JsonContent.Create(new{includedTypes=new[]{included},maxResultCount=10,locationRestriction=new{circle=new{center=new{latitude=lat,longitude=lng},radius=10000.0}}});
  var res=await clients.CreateClient().SendAsync(req,ct);if(!res.IsSuccessStatusCode)return [];
  using var doc=JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));var list=new List<ProviderOption>();
  foreach(var p in doc.RootElement.GetProperty("places").EnumerateArray()){var loc=p.GetProperty("location");var name=p.GetProperty("displayName").GetProperty("text").GetString()??"Provider";list.Add(new ProviderOption(p.GetProperty("id").GetString()??Guid.NewGuid().ToString(),name,category,null,p.TryGetProperty("formattedAddress",out var a)?a.GetString():null,p.TryGetProperty("nationalPhoneNumber",out var ph)?ph.GetString():null,p.TryGetProperty("websiteUri",out var w)?w.GetString():null,new List<string>{"6:00 PM","6:30 PM","7:00 PM"}));}
  return list;
 }
}
