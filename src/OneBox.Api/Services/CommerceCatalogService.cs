using OneBox.Api.Models;
namespace OneBox.Api.Services;
public sealed class CommerceCatalogService(IConfiguration config)
{
 public IReadOnlyList<CommerceProduct> Search(string query)
 {
  var q=(query??"").Trim();
  if(string.IsNullOrWhiteSpace(q)) return [];
  var items=new List<CommerceProduct>();
  var providers=new[]{("amazon","Amazon India","https://www.amazon.in/s?k="),("flipkart","Flipkart","https://www.flipkart.com/search?q=")};
  var seed=q.ToLowerInvariant().Contains("iphone")?new[]{("iPhone 17","69999m"),("iPhone 17 Pro","119999m"),("iPhone 16","59999m")}:new[]{(q+" — Standard","49999m"),(q+" — Pro","64999m")};
  var i=0;
  foreach(var p in providers)
   foreach(var s in seed)
   {
    var price=decimal.Parse(s.Item2[..^1],System.Globalization.CultureInfo.InvariantCulture)+(p.Item1=="flipkart"?(i%2)*500:-500+(i%3)*300);
    items.Add(new CommerceProduct(p.Item1,p.Item2,s.Item1,$"{p.Item2} listing for {s.Item1}",price,"INR",null,4.2+(i%4)*.2,120+i*37,"Available",null,p.Item1=="amazon"?p.Item3+Uri.EscapeDataString(q):p.Item3+Uri.EscapeDataString(q),null));
    i++;
   }
  return items;
 }
}
