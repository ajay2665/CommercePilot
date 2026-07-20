namespace Commerce.Api.Features.Support.Commands.Triage;

public sealed record TriageResponse(Guid TicketId, string Status);
