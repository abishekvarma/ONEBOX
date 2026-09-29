using System.Net.Http.Headers;
using System.Text.Json;
using OneBox.Api.Models;

namespace OneBox.Api.Services;

public sealed class CommerceCatalogService(IHttpClientFactory clients, IConfiguration config)
{
    public async Task<IReadOnlyList<CommerceProduct>> SearchAsync(string query, CancellationToken ct)
    {
        var q=(query??"").Trim();
        if(string.IsNullOrWhiteSpace(q)) return [];

        if(string.Equals(config["Commerce:Mode"],"ci",StringComparison.OrdinalIgnoreCase))
            return TestCatalog(q);

        var results=new List<CommerceProduct>();
        foreach(var provider in new[]{("Amazon India","Amazon"),("Flipkart","Flipkart")})
        {
            var endpoint=config[$"Commerce:Providers:{provider.Item2}:Endpoint"];
            if(string.IsNullOrWhiteSpace(endpoint)) continue;
            var apiKey=config[$"Commerce:Providers:{provider.Item2}:ApiKey"];
            var url=endpoint.Replace("{query}",Uri.EscapeDataString(q),StringComparison.Ordinal);
            try
            {
                using var req=new HttpRequestMessage(HttpMethod.Get,url);
                if(!string.IsNullOrWhiteSpace(apiKey)) req.Headers.Authorization=new AuthenticationHeaderValue("Bearer",apiKey);
                using var res=await clients.CreateClient().SendAsync(req,ct);
                if(!res.IsSuccessStatusCode) continue;
                using var doc=JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
                var root=doc.RootElement;
                var array=root.ValueKind==JsonValueKind.Array?root:(root.TryGetProperty("products",out var products)?products:default);
                if(array.ValueKind!=JsonValueKind.Array) continue;
                foreach(var item in array.EnumerateArray())
                {
                    var mapped=Map(provider.Item2,provider.Item1,item,q);
                    if(mapped is not null) results.Add(mapped);
                }
            }
            catch(OperationCanceledException) when(ct.IsCancellationRequested){throw;}
            catch{ }
        }
        return results;
    }

    private static CommerceProduct? Map(string providerId,string providerName,JsonElement item,string query)
    {
        if(!item.TryGetProperty("productId",out var id)||!item.TryGetProperty("title",out var title)||!item.TryGetProperty("price",out var price))
            return null;
        if(!price.TryGetDecimal(out var amount)||amount<=0)return null;
        return new CommerceProduct(
            providerId,providerName,id.GetString()??query,title.GetString()??query,amount,
            item.TryGetProperty("currency",out var currency)?currency.GetString()??"INR":"INR",
            item.TryGetProperty("originalPrice",out var original)&&original.ValueKind==JsonValueKind.Number&&original.TryGetDecimal(out var op)?op:null,
            item.TryGetProperty("rating",out var rating)&&rating.TryGetDouble(out var rt)?rt:null,
            item.TryGetProperty("ratingCount",out var rc)&&rc.TryGetInt32(out var rci)?rci:null,
            item.TryGetProperty("delivery",out var delivery)?delivery.GetString():null,
            item.TryGetProperty("seller",out var seller)?seller.GetString():null,
            item.TryGetProperty("productUrl",out var url)?url.GetString():null,
            item.TryGetProperty("imageUrl",out var image)?image.GetString():null);
    }

    private static IReadOnlyList<CommerceProduct> TestCatalog(string q)
    {
        var seed=q.Contains("iphone",StringComparison.OrdinalIgnoreCase)
            ?new[]{("iPhone 17","69999"),("iPhone 17 Pro","119999"),("iPhone 16","59999")}
            :new[]{(q+" — Standard","49999"),(q+" — Pro","64999")};
        var list=new List<CommerceProduct>();
        foreach(var provider in new[]{("amazon","Amazon India"),("flipkart","Flipkart")})
            foreach(var s in seed)
                list.Add(new CommerceProduct(provider.Item1,provider.Item2,$"ci-{provider.Item1}-{s.Item1}",s.Item1,decimal.Parse(s.Item2), "INR",null,4.5,100,"CI TEST DATA","CI TEST SELLER","https://example.invalid/ci",null));
        return list;
    }
}