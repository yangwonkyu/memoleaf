using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Portfolio.SessionResults
{
    public enum Outcome { Correct, Incorrect, TimedOut }

    public sealed record Trial<T>(
        int Number, int PresentedAtMs, int? FirstResponseMs, Outcome Outcome, T Details);

    public sealed record Round<T>(int Number, int Level, IReadOnlyList<Trial<T>> Trials);

    public static class StartLevel
    {
        public static int Resolve(int requested, IReadOnlyList<int> supported)
        {
            ArgumentNullException.ThrowIfNull(supported);
            if (supported.Count == 0)
                throw new ArgumentException("At least one level is required.", nameof(supported));

            int lowest = int.MaxValue;
            int? nearest = null;
            foreach (int level in supported)
            {
                if (level < 1)
                    throw new ArgumentException("Levels must be positive.", nameof(supported));
                lowest = Math.Min(lowest, level);
                if (level <= requested && (!nearest.HasValue || level > nearest.Value))
                    nearest = level;
            }

            // A request below the supported range starts at the minimum available level.
            return nearest ?? lowest;
        }
    }

    public static class ResultJson
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        public static string Serialize<T>(Round<T> round)
        {
            ArgumentNullException.ThrowIfNull(round);
            if (round.Number < 1 || round.Level < 1 || round.Trials == null || round.Trials.Count == 0)
                throw new ArgumentException("A round needs a positive number, level, and at least one trial.");

            int previousNumber = 0;
            int previousPresentation = -1;
            foreach (Trial<T> trial in round.Trials)
            {
                if (trial == null || trial.Number <= previousNumber || trial.PresentedAtMs < 0 ||
                    trial.PresentedAtMs < previousPresentation || trial.FirstResponseMs < 0 ||
                    trial.Details is null || !Enum.IsDefined(trial.Outcome))
                    throw new ArgumentException("Invalid trial order, timing, outcome, or details.");

                // A timeout may follow an input. Only a decision requires a response.
                if (trial.Outcome != Outcome.TimedOut && trial.FirstResponseMs == null)
                    throw new ArgumentException("A correct or incorrect trial needs a response time.");

                previousNumber = trial.Number;
                previousPresentation = trial.PresentedAtMs;
            }

            return JsonSerializer.Serialize(round, Options);
        }
    }
}
