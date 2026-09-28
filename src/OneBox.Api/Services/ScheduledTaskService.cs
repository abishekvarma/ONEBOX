using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OneBox.Api.Data;
using OneBox.Api.Models;

namespace OneBox.Api.Services;

public sealed class ScheduledTaskService(OneBoxDb db)
{
    public async Task<ScheduledTask> CreateAsync(Guid userId, ScheduledTaskRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title)) throw new ArgumentException("Title is required.");
        var trigger = (request.TriggerType ?? "TIME").Trim().ToUpperInvariant();
        if (trigger is not ("TIME" or "PRICE" or "SLOT")) throw new ArgumentException("TriggerType must be TIME, PRICE, or SLOT.");
        if (trigger == "TIME" && request.RunAtUtc is null) throw new ArgumentException("RunAtUtc is required for TIME tasks.");
        if (trigger != "TIME" && string.IsNullOrWhiteSpace(request.ConditionJson)) throw new ArgumentException("ConditionJson is required for conditional tasks.");
        var task = new ScheduledTask
        {
            UserId = userId, Type = string.IsNullOrWhiteSpace(request.Type) ? "GENERAL" : request.Type.Trim().ToUpperInvariant(),
            Title = request.Title.Trim(), TriggerType = trigger, RunAtUtc = request.RunAtUtc,
            ConditionJson = request.ConditionJson, PayloadJson = request.PayloadJson ?? "{}",
            NextCheckAtUtc = request.RunAtUtc ?? DateTime.UtcNow, Status = "WAITING"
        };
        db.ScheduledTasks.Add(task);
        db.AuditEvents.Add(new AuditEvent { UserId = userId, EventType = "SCHEDULED_TASK_CREATED", Detail = task.Title });
        await db.SaveChangesAsync(ct);
        return task;
    }

    public async Task<(OneTask Task,ScheduledTask Schedule)> CreateLinkedAsync(Guid userId, AgentPlan plan, string triggerType, DateTime? runAtUtc, string? conditionJson, CancellationToken ct)
    {
        var task=new OneTask{UserId=userId,Type=plan.Type,Title=plan.Title,Status="WAITING",RequiresConfirmation=true,PayloadJson=JsonSerializer.Serialize(plan)};
        db.Tasks.Add(task);
        var schedule=new ScheduledTask{UserId=userId,TaskId=task.Id,Type=plan.Type,Title=plan.Title,TriggerType=triggerType,RunAtUtc=runAtUtc,ConditionJson=conditionJson,PayloadJson=JsonSerializer.Serialize(plan),NextCheckAtUtc=runAtUtc??DateTime.UtcNow,Status="WAITING"};
        db.ScheduledTasks.Add(schedule);
        db.AuditEvents.Add(new AuditEvent{UserId=userId,TaskId=task.Id,EventType="SCHEDULED_TASK_CREATED",Detail=plan.Title});
        await db.SaveChangesAsync(ct);
        return (task,schedule);
    }

    public async Task<bool> EvaluateAsync(ScheduledTask task, CancellationToken ct)
    {
        task.LastCheckedAtUtc = DateTime.UtcNow;
        if (task.TriggerType == "TIME")
            return task.RunAtUtc <= DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(task.ConditionJson)) return false;
        using var doc = JsonDocument.Parse(task.ConditionJson);
        var root = doc.RootElement;
        if (task.TriggerType == "PRICE")
        {
            if (!root.TryGetProperty("threshold", out var threshold) || !threshold.TryGetDecimal(out var limit)) return false;
            if (!root.TryGetProperty("observedPrice", out var observed) || !observed.TryGetDecimal(out var price)) return false;
            return price <= limit;
        }
        if (task.TriggerType == "SLOT")
            return root.TryGetProperty("available", out var available) && available.ValueKind == JsonValueKind.True;
        return false;
    }

    public async Task UpdateObservationAsync(Guid userId, Guid id, string conditionJson, CancellationToken ct)
    {
        var task = await db.ScheduledTasks.SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct)
            ?? throw new KeyNotFoundException("Scheduled task not found.");
        if (task.Status is "COMPLETED" or "CANCELLED") throw new InvalidOperationException("Scheduled task is no longer active.");
        task.ConditionJson = conditionJson;
        task.NextCheckAtUtc = DateTime.UtcNow;
        task.UpdatedAt = DateTime.UtcNow;

        if (await EvaluateAsync(task, ct))
        {
            var now = DateTime.UtcNow;
            task.Status = "CONDITION_MET";
            task.NotificationStatus = "PENDING_CONFIRMATION";
            task.NextCheckAtUtc = null;
            task.UpdatedAt = now;

            if (task.TaskId is Guid taskId)
            {
                var linked = await db.Tasks.SingleOrDefaultAsync(x => x.Id == taskId && x.UserId == userId, ct);
                if (linked is not null)
                {
                    linked.Status = "AWAITING_CONFIRMATION";
                    linked.UpdatedAt = now;
                }
            }

            db.AuditEvents.Add(new AuditEvent
            {
                UserId = userId,
                TaskId = task.TaskId,
                EventType = "SCHEDULED_CONDITION_MET",
                Detail = task.Title
            });
        }

        await db.SaveChangesAsync(ct);
    }
}
public sealed record ScheduledTaskRequest(string Title,string? Type,string? TriggerType,DateTime? RunAtUtc,string? ConditionJson,string? PayloadJson);
