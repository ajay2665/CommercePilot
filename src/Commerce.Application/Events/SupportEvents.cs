namespace Commerce.Application.Events;

public sealed record TicketCreated(Guid TicketId);

public sealed record TicketEscalated(Guid TicketId, string Reason);
