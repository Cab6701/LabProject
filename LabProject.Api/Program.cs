using LabProject.Api.Contracts;
using LabProject.Application;
using LabProject.Application.Notifications.SendNotification;
using LabProject.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

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

app.UseHttpsRedirection();

app.Run();