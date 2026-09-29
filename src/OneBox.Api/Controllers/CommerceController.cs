using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneBox.Api.Data;
using OneBox.Api.Models;
using OneBox.Api.Services;

namespace OneBox.Api.Controllers;

[Authorize,ApiController,Route("api/commerce")]
public sealed class CommerceController(CommerceCatalogService catalog,OneBoxDb db):ControllerBase
{
    Guid UserId=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("compare")]
    public async Task<IActionResult> Compare([FromQuery]string query,CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(query)) return BadRequest(new{message="Enter a product or model to compare."});
        var items=await catalog.SearchAsync(query,ct);
        if(items.Count==0)
            return StatusCode(StatusCodes.Status503ServiceUnavailable,new{message="No authorized live commerce provider returned offers. Configure the provider APIs before showing prices.",live=false});
        var groups=items.GroupBy(x=>x.ProductId).Select(g=>new{productId=g.Key,title=g.First().Title,offers=g.OrderBy(x=>x.Price).ToList()}).ToList();
        return Ok(new{query,generatedAtUtc=DateTime.UtcNow,live=true,products=groups});
    }

    [HttpPost("select")]
    public async Task<IActionResult> Select(CommerceSelectionRequest r,CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(r.ProviderId)||string.IsNullOrWhiteSpace(r.ProviderName)||string.IsNullOrWhiteSpace(r.ProductId)||string.IsNullOrWhiteSpace(r.Title))
            return BadRequest(new{message="A complete provider offer is required."});
        if(r.Price<=0||r.Price>100000000) return BadRequest(new{message="Invalid offer price."});

        var task=new OneTask{
            UserId=UserId,
            Type="SHOPPING_PURCHASE",
            Title=$"Buy {r.Title}",
            Status="AWAITING_CONFIRMATION",
            RequiresConfirmation=true,
            PayloadJson=JsonSerializer.Serialize(new{
                providerId=r.ProviderId,providerName=r.ProviderName,productId=r.ProductId,
                title=r.Title,price=r.Price,currency=r.Currency??"INR",
                delivery=r.Delivery,seller=r.Seller,productUrl=r.ProductUrl
            })
        };
        db.Tasks.Add(task);
        db.AuditEvents.Add(new AuditEvent{
            UserId=UserId,TaskId=task.Id,EventType="COMMERCE_OFFER_SELECTED",
            Detail=$"Selected {r.ProviderName} offer for {r.Title} at {r.Price:0.00} {r.Currency??"INR"}."
        });
        await db.SaveChangesAsync(ct);
        return Ok(new{
            taskId=task.Id,status=task.Status,requiresConfirmation=true,
            message="Offer selected. Review the provider, product and price before continuing.",
            offer=new{r.ProviderId,r.ProviderName,r.ProductId,r.Title,r.Price,currency=r.Currency??"INR",r.Delivery,r.Seller,r.ProductUrl}
        });
    }
}

public record CommerceSelectionRequest(
    string ProviderId,string ProviderName,string ProductId,string Title,decimal Price,
    string? Currency,string? Delivery,string? Seller,string? ProductUrl);
