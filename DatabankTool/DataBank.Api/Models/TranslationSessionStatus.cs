namespace DataBank.Api.Models;

public static class TranslationSessionStatus
{
    public const string Pending = "pending";
    public const string InProgress = "in-progress";
    public const string Completed = "completed";

    public static bool IsValidTransition(string from, string to)
    {
        return (from, to) switch
        {
            (Pending, InProgress) => true,
            (InProgress, Completed) => true,
            _ => false
        };
    }
}
