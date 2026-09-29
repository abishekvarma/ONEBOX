using System.Text.Json;
namespace OneBox.Api.Services;
public record AgentPlan(string Type,string Title,bool NeedsConfirmation,string? Date,string? Time,string? Location,string Details,string? MissingInput=null);
public record AgentResponse(Guid TaskId,string Status,bool RequiresConfirmation,string Message,AgentPlan Plan,object? Result);
public record ProviderOption(string Id,string Name,string Category,double? DistanceKm,string? Address,string? Phone,string? WebsiteUrl,List<string> Slots,double? Rating=null,int? RatingCount=null,string? PriceLevel=null,double? Latitude=null,double? Longitude=null);
public record ConnectorResult(bool Success,string Status,string Message,string? ProviderReference,string? ProviderUrl,DateTime? ScheduledAt,decimal Amount);
public record ProviderSelectionRequest(Guid TaskId,string ProviderId,string ProviderName,string Category,double? DistanceKm=null,double? Rating=null,int? RatingCount=null,string? PriceLevel=null,string? Address=null,string? WebsiteUrl=null);
