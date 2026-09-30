using Healthcare.LoadTests.Configuration;
using NBomber.Contracts;
using NBomber.CSharp;

namespace Healthcare.LoadTests.Scenarios;

/// <summary>
/// The five load profiles. Closed model (KeepConstant/RampingConstant) is used
/// throughout because this API is transactional: log in once, then do work and
/// wait for each response — exactly what a constant-concurrency model simulates.
///
/// LEARNING NOTE — reading a profile result:
/// - Baseline answers "is the test itself correct?" (0 errors, sane latency).
/// - Normal answers "how do we feel on a typical day?"
/// - Ramp answers "WHERE does it break?" (find the knee: latency climbs,
///   throughput flattens, errors appear).
/// - Sustained answers "does it DEGRADE over time?" (leaks, pool exhaustion).
/// - Stress answers "what is the ceiling?" (run only when the above are green).
/// </summary>
public static class LoadProfiles
{
    public const string Baseline = "baseline";
    public const string Normal = "normal";
    public const string Ramp = "ramp";
    public const string Sustained = "sustained";
    public const string Stress = "stress";

    public static readonly string[] All = [Baseline, Normal, Ramp, Sustained, Stress];

    /// <summary>
    /// Applies the profile's load simulations + a short warm-up to a scenario.
    /// Warm-up (5 s) lets JIT/GC settle so the first seconds don't pollute P99.
    /// </summary>
    public static ScenarioProps Apply(ScenarioProps scenario, string profile, LoadSettings load)
    {
        var withSims = profile switch
        {
            Baseline => scenario.WithLoadSimulations(
                Simulation.KeepConstant(load.BaselineUsers, TimeSpan.FromSeconds(load.BaselineDurationSeconds))),

            Normal => scenario.WithLoadSimulations(
                Simulation.KeepConstant(load.NormalUsers, TimeSpan.FromSeconds(load.NormalDurationSeconds))),

            // Step through each level; NBomber runs simulations SEQUENTIALLY,
            // so one run shows latency/throughput at 10 -> 25 -> 50 -> 100 users.
            Ramp => scenario.WithLoadSimulations(
                load.RampSteps
                    .Select(copies => Simulation.KeepConstant(copies, TimeSpan.FromSeconds(load.RampStepSeconds)))
                    .ToArray()),

            Sustained => scenario.WithLoadSimulations(
                Simulation.KeepConstant(load.NormalUsers, TimeSpan.FromMinutes(load.SustainedMinutes))),

            // Gentle ramp to the ceiling, then hold it. If the machine or SQL
            // Express struggles, lower StressUsers in appsettings.json first.
            Stress => scenario.WithLoadSimulations(
                Simulation.RampingConstant(load.StressUsers, TimeSpan.FromSeconds(load.StressDurationSeconds)),
                Simulation.KeepConstant(load.StressUsers, TimeSpan.FromSeconds(load.StressDurationSeconds))),

            _ => throw new ArgumentException(
                $"Unknown profile '{profile}'. Valid: {string.Join(", ", All)}", nameof(profile))
        };

        return withSims.WithWarmUpDuration(TimeSpan.FromSeconds(5));
    }

    public static string Describe(string profile, LoadSettings load) => profile switch
    {
        Baseline => $"{load.BaselineUsers} users for {load.BaselineDurationSeconds}s — correctness + baseline P50/P95/P99.",
        Normal => $"{load.NormalUsers} users for {load.NormalDurationSeconds}s — realistic day.",
        Ramp => $"steps [{string.Join("->", load.RampSteps)}] x {load.RampStepSeconds}s — find the knee.",
        Sustained => $"{load.NormalUsers} users for {load.SustainedMinutes} min — detect slow degradation.",
        Stress => $"ramp to {load.StressUsers} + hold {load.StressDurationSeconds}s — find the ceiling.",
        _ => profile
    };
}
