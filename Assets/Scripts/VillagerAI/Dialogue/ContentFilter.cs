using System;

namespace TabletopWeatherGod.VillagerAI.Dialogue
{
    /// <summary>
    /// LLM-side guardrails: short replies, on-topic, Meta-guidelines-safe.
    /// Dialogue ≤ 25 words, chronicle ≤ 60 words. Small blocklist catches
    /// obvious violations; the proxy runs a stronger filter server-side.
    /// Never waits on network: caller must timeout (1500 ms) and use fallback.
    /// </summary>
    public static class ContentFilter
    {
        public const int MaxDialogueWords = 25;
        public const int MaxChronicleWords = 60;
        public const int TimeoutMs = 1500;

        private static readonly string[] Blocked = {
            "kill", "die", "blood", "gore", "torture", "suicide",
            "hate", "stupid", "idiot", "damn", "hell",
        };

        public static int WordCount(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            return s.Split(new[] { ' ', '\t', '\n', '\r' },
                StringSplitOptions.RemoveEmptyEntries).Length;
        }

        public static bool IsAllowedDialogue(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            if (WordCount(s) > MaxDialogueWords) return false;
            string low = s.ToLowerInvariant();
            foreach (var b in Blocked)
                if (low.Contains(b)) return false;
            return true;
        }

        public static bool IsAllowedChronicle(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            if (WordCount(s) > MaxChronicleWords) return false;
            string low = s.ToLowerInvariant();
            foreach (var b in Blocked)
                if (low.Contains(b)) return false;
            return true;
        }

        /// <returns>Fallback line when candidate fails the filter.</returns>
        public static string OrFallback(string candidate, Mood mood, string topic, int seed)
        {
            if (IsAllowedDialogue(candidate)) return candidate.Trim();
            return FallbackLines.Pick(mood, topic, seed);
        }
    }
}
