namespace Commerce.Application.Options;

public sealed class SupportOptions
{
    public string[] Brands { get; set; } = [];

    /// <summary>High-urgency tickets below this classification confidence get escalated.</summary>
    public double EscalationConfidenceThreshold { get; set; } = 0.6;

    public string DefaultTeam { get; set; } = "General Support";

    public string EscalationChannel { get; set; } = "Escalations";
}
