namespace Commerce.Api.Features.Platform.Queries.GetStatus;

public sealed record StatusResponse(int Queued, Guid? ProcessingTicketId, string? ProcessingSubject, string? Stage);
