namespace OneBox.Api.Models;
public sealed record CommerceProduct(
 string ProviderId,string ProviderName,string ProductId,string Title,
 decimal Price,string Currency,decimal? OriginalPrice,double? Rating,int? RatingCount,
 string? Delivery,string? Seller,string? ProductUrl,string? ImageUrl);
