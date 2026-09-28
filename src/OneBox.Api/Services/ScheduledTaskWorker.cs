using Microsoft.EntityFrameworkCore;
using OneBox.Api.Data;

namespace OneBox.Api.Services;

public sealed class ScheduledTaskWorker(IServiceScopeFactory scopes,ILogger<ScheduledTaskWorker> logger):BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope=scopes.CreateScope();
                var db=scope.ServiceProvider.GetRequiredService<OneBoxDb>();
                var scheduler=scope.ServiceProvider.GetRequiredService<ScheduledTaskService>();
                var now=DateTime.UtcNow;
                var tasks=await db.ScheduledTasks.Where(x=>(x.Status=="WAITING"||x.Status=="PLANNED") && (x.NextCheckAtUtc==null || x.NextCheckAtUtc<=now)).OrderBy(x=>x.CreatedAt).Take(50).ToListAsync(stoppingToken);
                foreach(var task in tasks)
                {
                    if(await scheduler.EvaluateAsync(task,stoppingToken))
                    {
                        task.Status="CONDITION_MET";
                        task.NotificationStatus="PENDING";
                        task.NextCheckAtUtc=null;
                        task.UpdatedAt=now;
                        db.AuditEvents.Add(new OneBox.Api.Models.AuditEvent{UserId=task.UserId,EventType="SCHEDULED_CONDITION_MET",Detail=task.Title});
                    }
                    else
                    {
                        task.NextCheckAtUtc=now.AddSeconds(30);
                        task.UpdatedAt=now;
                    }
                }
                if(tasks.Count>0) await db.SaveChangesAsync(stoppingToken);
            }
            catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested){break;}
            catch(Exception ex){logger.LogError(ex,"Scheduled task worker iteration failed.");}
            await Task.Delay(TimeSpan.FromSeconds(5),stoppingToken);
        }
    }
}
