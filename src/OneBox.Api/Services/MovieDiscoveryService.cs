using System.Net.Http.Headers;
using System.Text.Json;

namespace OneBox.Api.Services;

public sealed record MovieOption(int Id,string Title,string? Overview,string? ReleaseDate,double Rating,int RatingCount,string? PosterUrl);

public sealed class MovieDiscoveryService(IHttpClientFactory clients,IConfiguration cfg)
{
    public async Task<List<MovieOption>> SearchAsync(string query,CancellationToken ct)
    {
        var key=cfg["TMDB:ApiKey"];
        if(string.IsNullOrWhiteSpace(key)||string.IsNullOrWhiteSpace(query)) return [];
        using var req=new HttpRequestMessage(HttpMethod.Get,$"https://api.themoviedb.org/3/search/movie?query={Uri.EscapeDataString(query)}&language=en-US&region=IN&include_adult=false");
        req.Headers.Authorization=new AuthenticationHeaderValue("Bearer",key);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var res=await clients.CreateClient().SendAsync(req,ct);
        if(!res.IsSuccessStatusCode)return [];
        using var doc=JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
        if(!doc.RootElement.TryGetProperty("results",out var results))return [];
        var list=new List<MovieOption>();
        foreach(var x in results.EnumerateArray().Take(8))
        {
            var title=x.TryGetProperty("title",out var t)?t.GetString():"Movie";
            var overview=x.TryGetProperty("overview",out var o)?o.GetString():null;
            var release=x.TryGetProperty("release_date",out var rd)?rd.GetString():null;
            var rating=x.TryGetProperty("vote_average",out var va)?va.GetDouble():0;
            var count=x.TryGetProperty("vote_count",out var vc)?vc.GetInt32():0;
            var poster=x.TryGetProperty("poster_path",out var pp)&&pp.ValueKind!=JsonValueKind.Null?"https://image.tmdb.org/t/p/w500"+pp.GetString():null;
            list.Add(new MovieOption(x.GetProperty("id").GetInt32(),title,overview,release,rating,count,poster));
        }
        return list;
    }
}