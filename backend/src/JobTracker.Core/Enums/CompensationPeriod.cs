namespace JobTracker.Core.Enums;

/// <summary>The time unit a compensation amount is quoted in, so hourly, daily and yearly offers can be told apart.</summary>
public enum CompensationPeriod
{
    /// <summary>Per hour, typical for contracts and freelance work.</summary>
    Hour,

    /// <summary>Per day, typical for UK and EU contracts.</summary>
    Day,

    /// <summary>Per month.</summary>
    Month,

    /// <summary>Per year, typical for permanent roles.</summary>
    Year
}
