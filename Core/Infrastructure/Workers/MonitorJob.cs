using System.Net.NetworkInformation;
using SPSS.Domain.Enums;
using SPSS.Domain.Models.BL;

namespace SPSS.Infrastructure.Workers;
public class MonitorJob(IScheduleConfig<MonitorJob> config, ILogger<MonitorJob> logger, IServiceProvider services) : CronJobService(config.CronExpression!, config.TimeZoneInfo!)
{
    private readonly ILogger<MonitorJob> _logger = logger;
    private readonly IServiceProvider _services = services;

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Monitor Job Started, next occurrence will be at: {timeStamp} Utc", GetSchedule);
        return base.StartAsync(cancellationToken);
    }

    public override async Task DoWork(CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Monitor Job is working...");

        using var scope = _services.CreateScope();
        var _storageBroker = scope.ServiceProvider.GetRequiredService<IStorageBroker>();
        var _httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();
        try
        {
            var participants = await _storageBroker.Participants.Where(x => x.Active)
            .Include(x => x.Endpoints)
            .Include(x => x.VPNs)
            .ThenInclude(x => x.Tunnels)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

            _httpClient.Timeout = TimeSpan.FromSeconds(8);
            var ping = new Ping();

            foreach (var participant in participants)
            {
                foreach (var vpn in participant.VPNs)
                {
                    foreach (var tunnel in vpn.Tunnels)
                    {
                        var vpnMH = new MonitHistory
                        {
                            ParticipantId = participant.Id!,
                            Address = tunnel.Endpoint,
                            Date = DateTimeOffset.UtcNow.ToUniversalTime(),
                            Type = ServiceType.VPN,
                        };
                        try
                        {
                            var reply = await ping.SendPingAsync(tunnel.Endpoint, 1000);
                            vpnMH.Status = reply.Status == IPStatus.Success ? ServiceStatus.Up : ServiceStatus.Down;
                            vpnMH.RoundtripTime = (int)reply.RoundtripTime;
                            vpnMH.PacketLoss = reply.Status == IPStatus.Success ? 0 : 100;
                            vpnMH.AdditionalInfo = reply.Status.ToString();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "{Participant}: Ping failed for tunnel {Endpoint}", participant.Name, tunnel.Endpoint);
                            // Add a down status
                            vpnMH.Status = ServiceStatus.Down;
                            vpnMH.PacketLoss = 100;
                            vpnMH.AdditionalInfo = ex.Message;
                        }
                        await _storageBroker.MonitHistories.AddAsync(vpnMH, cancellationToken);
                        await _storageBroker.SaveChangesAsync(cancellationToken);
                    }
                }

                // Endpoints
                foreach (var endpoint in participant.Endpoints)
                {
                    var endpointMH = new MonitHistory
                    {
                        ParticipantId = participant.Id!,
                        Date = DateTimeOffset.UtcNow.ToUniversalTime(),
                        Address = endpoint.Address,
                        Type = ServiceType.CallbackEndpoint,
                    };
                    try
                    {
                        var response = await _httpClient.PostAsync(endpoint.Address, null, cancellationToken);

                        endpointMH.RoundtripTime = (int)(response.Headers.Date?.Subtract(DateTimeOffset.UtcNow).TotalMilliseconds ?? 0);
                        endpointMH.Status = response != null ? ServiceStatus.Up : ServiceStatus.Down;
                        endpointMH.PacketLoss = response != null ? 0 : 100;
                        endpointMH.AdditionalInfo = response?.Content != null ? await response.Content.ReadAsStringAsync(cancellationToken) : "No content";
                    }
                    catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogError(ex, "{Participant}: HTTP request timed out for {Address}", participant.Name, endpoint.Address);
                        // Record the timeout without setting status to Down
                        endpointMH.PacketLoss = 100;
                        endpointMH.AdditionalInfo = "Request timed out";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "{Participant}: HTTP request failed for {Address}", participant.Name, endpoint.Address);
                        // Add a down status
                        endpointMH.Status = ServiceStatus.Down;
                        endpointMH.PacketLoss = 100;
                        endpointMH.AdditionalInfo = ex.Message;
                    }
                    await _storageBroker.MonitHistories.AddAsync(endpointMH, cancellationToken);
                    await _storageBroker.SaveChangesAsync(cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Monitor Job failed");
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Monitor Job stopped...");
        return base.StopAsync(cancellationToken);
    }

}