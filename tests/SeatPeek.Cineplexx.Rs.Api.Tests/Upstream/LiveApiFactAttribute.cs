namespace SeatPeek.Cineplexx.Rs.Api.Tests.Upstream;

[AttributeUsage(AttributeTargets.Method)]
public sealed class LiveApiFactAttribute : FactAttribute
{
    public LiveApiFactAttribute()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("RUN_LIVE_API_CONTRACT_TESTS"),
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            Skip = "Set RUN_LIVE_API_CONTRACT_TESTS=true to run live API contract tests.";
        }
    }
}
