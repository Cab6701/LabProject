using LabProject.Api.Contracts;
using LabProject.Api.Services;
using LabProject.Application;
using LabProject.Application.DTOs;
using LabProject.Application.Interfaces;
using LabProject.Application.Notifications.SendNotification;
using LabProject.Domain.Entities;
using LabProject.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<CampaignSchedulerHostedService>();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

app.MapHealthChecks("/healthz");
app.MapOpenApi();

app.MapPost("/api/notifications/send", async (
    SendNotificationRequest request,
    ISendNotificationUseCase useCase,
    CancellationToken cancellationToken) =>
{
    var command = new SendNotificationCommand(
        request.UserId,
        request.MessageId,
        request.Channels,
        request.Attempt,
        request.Source,
        request.CreatedAt,
        request.Metadata
    );

    try
    {
        var messageId = await useCase.ExecuteAsync(command, cancellationToken);
        return Results.Accepted($"/api/notifications/{messageId}", new { MessageId = messageId });
    }
    catch (SendNotificationValidationException ex)
    {
        return Results.ValidationProblem(ex.Errors);
    }
});

app.MapPost("/api/notifications/send-bulk", async (
    SendNotificationBulkRequest request,
    INotificationPublisher publisher,
    CancellationToken cancellationToken) =>
{
    var errors = new Dictionary<string, string[]>();
    if (request.UserIds is null || request.UserIds.Length == 0)
    {
        errors["userIds"] = ["userIds is required."];
    }

    if (request.Channels is null || request.Channels.Length == 0)
    {
        errors["channels"] = ["channels is required."];
    }

    if (string.IsNullOrWhiteSpace(request.Source))
    {
        errors["source"] = ["source is required."];
    }

    if (request.Metadata is null)
    {
        errors["metadata"] = ["metadata is required."];
    }

    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    foreach (var userId in request.UserIds!)
    {
        var message = new ContractDTO(
            UserId: userId,
            MessageId: Guid.NewGuid().ToString("N"),
            Channels: request.Channels!,
            Attempt: 0,
            Source: request.Source!,
            CreatedAt: DateTime.UtcNow,
            Metadata: request.Metadata!);
        await publisher.SendNotificationAsync(message, cancellationToken);
    }

    return Results.Accepted("/api/notifications/send-bulk", new { request.UserIds!.Length });
});

app.MapPost("/api/campaigns", async (
    CreateCampaignRequest request,
    ICampaignRepository campaignRepository,
    CancellationToken cancellationToken) =>
{
    var errors = new Dictionary<string, string[]>();
    if (string.IsNullOrWhiteSpace(request.Name)) errors["name"] = ["name is required."];
    if (request.UserIds is null || request.UserIds.Length == 0) errors["userIds"] = ["userIds is required."];
    if (request.Channels is null || request.Channels.Length == 0) errors["channels"] = ["channels is required."];
    if (request.ScheduleTimeUtc is null) errors["scheduleTimeUtc"] = ["scheduleTimeUtc is required."];
    if (request.Metadata is null) errors["metadata"] = ["metadata is required."];
    if (request.RepeatType is null || request.RepeatType is < 0 or > 3) errors["repeatType"] = ["repeatType must be from 0..3."];
    if (errors.Count > 0) return Results.ValidationProblem(errors);

    var utcNow = DateTime.UtcNow;
    var schedule = DateTime.SpecifyKind(request.ScheduleTimeUtc!.Value, DateTimeKind.Utc);
    var repeat = (RepeatType)request.RepeatType!.Value;
    var campaign = new NotificationCampaign(
        Id: string.Empty,
        Name: request.Name!,
        UserIds: request.UserIds!,
        Channels: request.Channels!,
        Metadata: request.Metadata!,
        ScheduleTimeUtc: schedule,
        NextRunAtUtc: schedule,
        RepeatType: repeat,
        Enabled: request.Enabled ?? true,
        Source: string.IsNullOrWhiteSpace(request.Source) ? "campaign" : request.Source!,
        CreatedAtUtc: utcNow,
        UpdatedAtUtc: utcNow);

    var id = await campaignRepository.CreateAsync(campaign, cancellationToken);
    return Results.Created($"/api/campaigns/{id}", new { id });
});

app.MapPatch("/api/campaigns/{id}/enabled", async (
    string id,
    SetCampaignEnabledRequest request,
    ICampaignRepository campaignRepository,
    CancellationToken cancellationToken) =>
{
    var updated = await campaignRepository.SetEnabledAsync(id, request.Enabled, cancellationToken);
    return updated ? Results.Ok(new { id, request.Enabled }) : Results.NotFound();
});

app.MapGet("/api/campaigns/{id}", async (
    string id,
    ICampaignRepository campaignRepository,
    CancellationToken cancellationToken) =>
{
    var campaign = await campaignRepository.GetByIdAsync(id, cancellationToken);
    return campaign is null ? Results.NotFound() : Results.Ok(campaign);
});

app.MapGet("/stats/total", async (
    DateTime? from,
    DateTime? to,
    IAnalyticsStore analyticsStore,
    CancellationToken cancellationToken) =>
{
    var total = await analyticsStore.GetTotalAsync(from, to, cancellationToken);
    return Results.Ok(new { total });
});

app.MapGet("/stats/by-channel", async (
    DateTime? from,
    DateTime? to,
    IAnalyticsStore analyticsStore,
    CancellationToken cancellationToken) =>
{
    var data = await analyticsStore.GetByChannelAsync(from, to, cancellationToken);
    return Results.Ok(data);
});

app.MapGet("/stats/success-rate", async (
    DateTime? from,
    DateTime? to,
    IAnalyticsStore analyticsStore,
    CancellationToken cancellationToken) =>
{
    var successRate = await analyticsStore.GetSuccessRateAsync(from, to, cancellationToken);
    return Results.Ok(new { successRate });
});

app.MapGet("/stats/by-driverCode", async (
    DateTime? from,
    DateTime? to,
    IAnalyticsStore analyticsStore,
    CancellationToken cancellationToken) =>
{
    var data = await analyticsStore.GetByDriverCodeAsync(from, to, cancellationToken);
    return Results.Ok(data);
});

app.UseHttpsRedirection();

app.Run();