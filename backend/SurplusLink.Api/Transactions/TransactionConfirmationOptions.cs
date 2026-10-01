namespace SurplusLink.Api.Transactions;

/// <summary>Server-owned confirmation deadlines; clients only display these values.</summary>
public sealed class TransactionConfirmationOptions
{
    public const string SectionName = "TransactionConfirmation";
    public int WindowDays { get; set; } = 30;
    public int FollowUpAfterDays { get; set; } = 5;
    public int PollSeconds { get; set; } = 60;
}
