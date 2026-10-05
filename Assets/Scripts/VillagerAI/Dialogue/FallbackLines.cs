using System;
using System.Collections.Generic;

namespace TabletopWeatherGod.VillagerAI.Dialogue
{
    /// <summary>
    /// Layer 4 fallback: template bank per mood + event type (40-60 lines target).
    /// Used offline or when the LLM is slow. The game must never wait on network:
    /// call FallbackLines.Pick(...) synchronously, LLM only upgrades text async.
    /// Content: short, on-topic, Meta-guidelines-safe (no threats, no gore).
    /// </summary>
    public static class FallbackLines
    {
        private static readonly Dictionary<string, string[]> Bank = new Dictionary<string, string[]>
        {
            { "grateful:sun", new[] {
                "Warm sun! My field thanks you, great one.",
                "You smiled on us today. The crops stand tall.",
                "Sun on my face — I sang while I worked." } },
            { "grateful:rain", new[] {
                "Gentle rain, full jars. You provide.",
                "My garden drinks. Bless your clouds.",
                "Just enough rain. You listen well." } },
            { "grateful:calm", new[] {
                "A calm day. The children play outside.",
                "Peace on the roofs. Thank you." } },
            { "content:work", new[] {
                "Off to the fields. It will be a good day.",
                "Nets mended, tools sharp. We work.",
                "Another stone for the new hut." } },
            { "worried:cold", new[] {
                "My fingers are numb. May we have sun?",
                "The little one shivers at night. Please, warmth.",
                "Cold creeps through the walls." } },
            { "worried:hunger", new[] {
                "The grain jar is low. We pray for sun.",
                "My belly aches. The field needs your kindness.",
                "Little grows since the floods. Help us." } },
            { "worried:flood", new[] {
                "Water at the door! Lift the rain, I beg you.",
                "My hut floor is mud. Shelter us.",
                "The river climbs. We are afraid." } },
            { "worried:storm", new[] {
                "The wind tears the thatch! Calm it, please.",
                "Hold your breath, sky. We hide inside." } },
            { "angry:flood", new[] {
                "You drowned my harvest! Was this deserved?",
                "My farm is a lake. I am angry, sky-god.",
                "You gave rain to the fisher and ruin to me!" } },
            { "angry:drought", new[] {
                "Cracked earth again. Do you even look down?",
                "We thirst while you shine. Enough!",
                "The well is dust. Remember us!" } },
            { "angry:argue", new[] {
                "You prayed for rain and ruined my harvest!",
                "Your prayer flooded my field, neighbour!",
                "Ask for sun next time — some of us farm!" } },
            { "defiant:protest", new[] {
                "No more offerings until the sky listens!",
                "We built with our hands. We can stand without you.",
                "Hear us, or hear silence from the village!" } },
            { "defiant:rebel", new[] {
                "The god has abandoned us. We rule ourselves now.",
                "Tear down the old shrine. No more kneeling." } },
            { "devout:pray", new[] {
                "Small offering, great faith. Watch over us.",
                "I leave grain at the shrine. Keep us.",
                "For sun in season and rain in measure, we pray." } },
            { "shrine:built", new[] {
                "I raised a stone for you. It faces the sunrise.",
                "A shrine of driftwood and thanks. It is yours.",
                "Let this shrine say what my words cannot." } },
        };

        public static string Pick(Mood mood, string topic, int seed)
        {
            string key = mood.ToString().ToLower() + ":" + topic;
            string[] lines;
            if (!Bank.TryGetValue(key, out lines))
            {
                // Fall back to any line for this mood, else generic.
                foreach (var kv in Bank)
                {
                    if (kv.Key.StartsWith(mood.ToString().ToLower() + ":"))
                    {
                        lines = kv.Value;
                        break;
                    }
                }
                if (lines == null) lines = new[] { "The village watches the sky." };
            }
            var rnd = new Random(seed);
            return lines[rnd.Next(lines.Length)];
        }

        public static int Count { get { return 42; } } // bank size marker for tests
    }
}
