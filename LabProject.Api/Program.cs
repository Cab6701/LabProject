using LabProject.Api.Contracts;
using LabProject.Application.DTOs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/healthz");
app.MapOpenApi();

app.MapPost("/api/notifications/send", (SendNotificationRequest request) =>
{
    var errors = ValidateSendNotification(request);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }
    var dto = new ContractDTO(
        UserId: request.UserId!,
        MessageId: string.IsNullOrWhiteSpace(request.MessageId) ? Guid.NewGuid().ToString("N") : request.MessageId!,
        Channels: request.Channels!,
        Attempt: request.Attempt ?? 0,
        Source: request.Source!,
        CreatedAt: request.CreatedAt ?? DateTime.UtcNow,
        Metadata: request.Metadata ?? new Dictionary<string, string>()
    );
    // TODO phase 1 step tiếp theo: publish dto lên Kafka notification-topic
    return Results.Accepted($"/api/notifications/{dto.MessageId}", new { dto.MessageId });
});

app.UseHttpsRedirection();

app.Run();


static Dictionary<string, string[]> ValidateSendNotification(SendNotificationRequest request)
{
    var errors = new Dictionary<string, string[]>();
    var allowedChannels = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "email", "firebase", "inapp" };
    if (string.IsNullOrWhiteSpace(request.UserId))
        errors["userId"] = ["userId is required."];
    if (string.IsNullOrWhiteSpace(request.Source))
        errors["source"] = ["source is required."];
    if (request.Channels is null || request.Channels.Length == 0)
        errors["channels"] = ["channels is required and must contain at least 1 item."];
    else if (request.Channels.Any(c => string.IsNullOrWhiteSpace(c) || !allowedChannels.Contains(c)))
        errors["channels"] = ["channels contains invalid value. Allowed: email, firebase, inapp."];
    if (request.Attempt is < 0)
        errors["attempt"] = ["attempt must be >= 0."];
    if (request.Metadata is null)
        errors["metadata"] = ["metadata is required (can be empty object)."];
    return errors;
}