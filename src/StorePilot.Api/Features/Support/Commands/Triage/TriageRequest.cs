namespace StorePilot.Api.Features.Support.Commands.Triage;

public sealed class TriageRequest
{
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
    public string Sender { get; set; } = "";
}
