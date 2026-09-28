using System.Text.Json;

namespace OneBox.Api.Services;

public sealed class GooglePlacesService(IHttpClientFactory clients,IConfiguration cfg)
{
    public async Task<List<ProviderOption>> SearchAsync(string category,double lat,double lng,CancellationToken ct)
    {
        var key=cfg["Google:ApiKey"];
        if(string.IsNullOrWhiteSpace(key)) return [];

        var included=category switch
        {
            "RESTAURANT_BOOKING"=>"restaurant",
            "HOSPITAL_APPOINTMENT"=>"hospital",
            "MOVIE_BOOKING"=>"movie_theater",
            _=>"establishment"
        };

        using var req=new HttpRequestMessage(HttpMethod.Post,"https://places.googleapis.com/v1/places:searchNearby");
        req.Headers.Add("X-Goog-Api-Key",key);
        req.Headers.Add("X-Goog-FieldMask","places.id,places.displayName,places.formattedAddress,places.location,places.nationalPhoneNumber,places.websiteUri,places.rating,places.userRatingCount,places.priceLevel");
        req.Content=JsonContent.Create(new
        {
            includedTypes=new[]{included},
            maxResultCount=10,
            locationRestriction=new{circle=new{center=new{latitude=lat,longitude=lng},radius=10000.0}}
        });

        var res=await clients.CreateClient().SendAsync(req,ct);
        if(!res.IsSuccessStatusCode) return [];

        using var doc=JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
        if(!doc.RootElement.TryGetProperty("places",out var places)) return [];

        var list=new List<ProviderOption>();
        foreach(var p in places.EnumerateArray())
        {
            var loc=p.GetProperty("location");
            var pLat=loc.GetProperty("latitude").GetDouble();
            var pLng=loc.GetProperty("longitude").GetDouble();
            var name=p.GetProperty("displayName").GetProperty("text").GetString()??"Provider";
            double? rating=p.TryGetProperty("rating",out var rt)?rt.GetDouble():null;
            int? ratingCount=p.TryGetProperty("userRatingCount",out var rc)?rc.GetInt32():null;
            string? price=p.TryGetProperty("priceLevel",out var pl)?pl.GetString():null;
            list.Add(new ProviderOption(
                p.GetProperty("id").GetString()??Guid.NewGuid().ToString(),
                name,category,HaversineKm(lat,lng,pLat,pLng),
                p.TryGetProperty("formattedAddress",out var a)?a.GetString():null,
                p.TryGetProperty("nationalPhoneNumber",out var ph)?ph.GetString():null,
                p.TryGetProperty("websiteUri",out var w)?w.GetString():null,
                new List<string>{"6:00 PM","6:30 PM","7:00 PM"},
                rating,ratingCount,price,pLat,pLng));
        }
        return list.OrderBy(x=>x.DistanceKm).ToList();
    }

    private static double HaversineKm(double lat1,double lng1,double lat2,double lng2)
    {
        const double R=6371;
        var dLat=(lat2-lat1)*Math.PI/180;
        var dLng=(lng2-lng1)*Math.PI/180;
        var a=Math.Sin(dLat/2)*Math.Sin(dLat/2)+Math.Cos(lat1*Math.PI/180)*Math.Cos(lat2*Math.PI/180)*Math.Sin(dLng/2)*Math.Sin(dLng/2);
        return Math.Round(R*2*Math.Atan2(Math.Sqrt(a),Math.Sqrt(1-a)),2);
    }
}