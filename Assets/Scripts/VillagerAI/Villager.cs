using System;

namespace TabletopWeatherGod.VillagerAI
{
    /// <summary>Named villager identity. 6-10 in MVP so players care who they help/hurt.</summary>
    [Serializable]
    public class VillagerPersonality
    {
        public string Name = "Asha";
        public string Job = "farmer"; // farmer | fisher | builder | elder | child
        public string Trait = "optimistic"; // optimistic | gruff | anxious | devout | mischievous
        public string Prayer; // daily-prayer request text (session goal)

        public VillagerPersonality() { }

        public VillagerPersonality(string name, string job, string trait)
        {
            Name = name; Job = job; Trait = trait;
        }
    }

    /// <summary>
    /// Aggregate villager: personality + needs + memory. Plain C# (no MonoBehaviour)
    /// so it runs in Unity and in tests. A thin MonoBehaviour can wrap this later
    /// for models/animation/thought icons.
    /// </summary>
    [Serializable]
    public class Villager
    {
        public VillagerPersonality Personality = new VillagerPersonality();
        public VillagerNeeds Needs = new VillagerNeeds();
        public VillagerMemory Memory = new VillagerMemory();
        public VillagerAction LastAction = VillagerAction.Idle;
        public bool HasBuiltShrine = false;

        public Villager() { }

        public Villager(string name, string job, string trait)
        {
            Personality = new VillagerPersonality(name, job, trait);
        }

        public float Relationship { get { return Memory.Relationship(); } }
        public Mood Mood { get { return Needs.CurrentMood(Relationship); } }

        /// <summary>Called every few seconds with current weather (not every frame).</summary>
        public VillagerAction Tick(WeatherState weather, float deltaDays, int day,
            ref float riverLevel, ref float fireRisk, float villageFaith, int lowFaithDays)
        {
            float foodBefore = Needs.Food, waterBefore = Needs.Water;
            float warmthBefore = Needs.Warmth, safetyBefore = Needs.Safety;

            Needs.ApplyWeather(weather, deltaDays, ref riverLevel, ref fireRisk);

            return FinishTick(weather, day, villageFaith, lowFaithDays,
                foodBefore, waterBefore, warmthBefore, safetyBefore);
        }

        /// <summary>
        /// Island-aware tick: island is ticked once by GameSession; this only
        /// touches the villager. Prefer this over Tick for all new code.
        /// </summary>
        public VillagerAction TickWithIsland(WeatherState weather, float deltaDays, int day,
            float islandRiver, float islandFire, float villageFaith, int lowFaithDays)
        {
            float foodBefore = Needs.Food, waterBefore = Needs.Water;
            float warmthBefore = Needs.Warmth, safetyBefore = Needs.Safety;

            Needs.ApplyWeatherWithIsland(weather, deltaDays, islandRiver, islandFire);

            return FinishTick(weather, day, villageFaith, lowFaithDays,
                foodBefore, waterBefore, warmthBefore, safetyBefore);
        }

        private VillagerAction FinishTick(WeatherState weather, int day,
            float villageFaith, int lowFaithDays,
            float foodBefore, float waterBefore, float warmthBefore, float safetyBefore)
        {
            // Remember what the god did to ME this tick (only notable deltas).
            float dFood = Needs.Food - foodBefore, dWater = Needs.Water - waterBefore;
            float dWarmth = Needs.Warmth - warmthBefore, dSafety = Needs.Safety - safetyBefore;
            float worst = Math.Min(Math.Min(dFood, dWater), Math.Min(dWarmth, dSafety));
            float best = Math.Max(Math.Max(dFood, dWater), Math.Max(dWarmth, dSafety));
            if (worst <= -8f)
                Memory.Remember(day, GodVerb(weather) + " hurt me (" + worst.ToString("F0") + ")", worst * 3f, "god:" + weather.Type.ToString().ToLower());
            else if (best >= 8f)
                Memory.Remember(day, GodVerb(weather) + " helped me (+" + best.ToString("F0") + ")", best * 3f, "god:" + weather.Type.ToString().ToLower());

            var action = UtilityAI.ChooseAction(this, weather, villageFaith, lowFaithDays);
            LastAction = action;
            Needs.ApplyActionOutcome(action);
            return action;
        }

        public void EndOfDay(int day)
        {
            Memory.Decay(day);
        }

        private static string GodVerb(WeatherState w)
        {
            switch (w.Type)
            {
                case WeatherType.Sun: return "sun warmed";
                case WeatherType.Rain: return "rain soaked";
                case WeatherType.Wind: return "wind battered";
                case WeatherType.Lightning: return "lightning struck near";
                case WeatherType.Snow: return "snow chilled";
                case WeatherType.Rainbow: return "rainbow blessed";
                default: return "calm soothed";
            }
        }
    }
}
