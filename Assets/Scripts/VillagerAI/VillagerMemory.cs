using System;
using System.Collections.Generic;

namespace TabletopWeatherGod.VillagerAI
{
    /// <summary>
    /// Layer 2: memory (on-device). Each villager keeps an event log of what
    /// the god did, when, and how it affected them, e.g. "flooded my farm,
    /// day 4, -30 food". Salience decays; big events decay slowly.
    /// Relationship = weighted sum of remembered events. Opinions of other
    /// villagers enable arguments ("You prayed for rain and ruined my harvest").
    /// </summary>
    [Serializable]
    public class MemoryEvent
    {
        public int Day;
        public string Description;   // e.g. "flooded my farm"
        public float Valence;        // -100..100, how good/bad for me
        public float InitialSalience;// 0..1 at write time
        public float Salience;       // current, decays
        public string Cause;         // "god:sun" | "god:rain" | "neighbour:Mara" | ...

        public MemoryEvent(int day, string description, float valence, float salience, string cause)
        {
            Day = day;
            Description = description;
            Valence = valence;
            InitialSalience = salience;
            Salience = salience;
            Cause = cause;
        }
    }

    [Serializable]
    public class VillagerMemory
    {
        public List<MemoryEvent> Events = new List<MemoryEvent>();
        public Dictionary<string, float> Opinions = new Dictionary<string, float>();
        public const int MaxEvents = 30;

        public void Remember(int day, string description, float valence, string cause)
        {
            float salience = SalienceFor(valence);
            Events.Add(new MemoryEvent(day, description, valence, salience, cause));
            if (Events.Count > MaxEvents)
                Events.RemoveAt(0); // forget oldest, lowest detail first in practice
        }

        public static float SalienceFor(float valence)
        {
            float a = Math.Abs(valence) / 100f;
            if (a < 0.15f) return 0.25f;
            if (a < 0.4f) return 0.55f;
            if (a < 0.7f) return 0.8f;
            return 1.0f;
        }

        /// <summary>Call once per in-game day. Big events decay slowly.</summary>
        public void Decay(int currentDay)
        {
            foreach (var e in Events)
            {
                int ageDays = Math.Max(0, currentDay - e.Day);
                float dailyKeep = e.InitialSalience > 0.8f ? 0.95f : 0.85f;
                e.Salience = e.InitialSalience * (float)Math.Pow(dailyKeep, ageDays);
            }
            Events.RemoveAll(e => e.Salience < 0.05f);
        }

        /// <summary>Weighted sum of remembered events, -100..100. Drives talk + acts.</summary>
        public float Relationship()
        {
            float sum = 0f;
            foreach (var e in Events)
                sum += e.Valence * e.Salience;
            if (sum < -100f) return -100f;
            if (sum > 100f) return 100f;
            return sum;
        }

        public List<MemoryEvent> TopMemories(int n)
        {
            var copy = new List<MemoryEvent>(Events);
            copy.Sort((a, b) =>
            {
                float sa = Math.Abs(a.Valence) * a.Salience;
                float sb = Math.Abs(b.Valence) * b.Salience;
                return sb.CompareTo(sa);
            });
            if (copy.Count > n) copy.RemoveRange(n, copy.Count - n);
            return copy;
        }

        public float OpinionOf(string otherName)
        {
            float v;
            if (Opinions.TryGetValue(otherName, out v)) return v;
            return 0f;
        }

        public void AdjustOpinion(string otherName, float delta)
        {
            float v = OpinionOf(otherName) + delta;
            if (v < -100f) v = -100f;
            if (v > 100f) v = 100f;
            Opinions[otherName] = v;
        }
    }
}
