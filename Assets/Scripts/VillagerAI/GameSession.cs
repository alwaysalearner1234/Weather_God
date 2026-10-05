using System;
using System.Collections.Generic;
using TabletopWeatherGod.VillagerAI.Dialogue;
using TabletopWeatherGod.VillagerAI.Save;

namespace TabletopWeatherGod.VillagerAI
{
    /// <summary>
    /// Session composer: owns Village + Island + Prayer + day flow.
    /// One session = one in-game day (5-8 min). Unity calls:
    ///   session = new GameSession(8);           // arrival (<10 s)
    ///   session.Prayer                          // "daily prayer" goal UI
    ///   session.ApplyWeather(w, deltaDays);     // gesture loop
    ///   icons = ThoughtIcons.GetIcon(...)       // per villager for sprites
    ///   intents = VillagerDirector.NextIntent(...) // movement targets
    ///   chronicle = session.EndDay();           // day-end scroll + save data
    /// Tick villagers every few seconds, NOT every frame. Island ticks once.
    /// </summary>
    public class GameSession
    {
        public VillageState Village = new VillageState();
        public IslandState Island = new IslandState();
        public DailyPrayer Prayer;
        public WeatherState CurrentWeather = WeatherState.Calm;
        public WeatherType DominantWeather = WeatherType.Calm;
        private readonly Dictionary<WeatherType, float> weatherTime = new Dictionary<WeatherType, float>();
        private float faithAtDawn;
        private readonly List<string> dayEvents = new List<string>();

        public GameSession(int villagerCount)
        {
            Village = VillageState.CreateDefault(villagerCount);
            Island = new IslandState();
            Prayer = DailyPrayer.Pick(Village);
            faithAtDawn = Village.Faith();
        }

        /// <summary>One AI step (call every ~2-3 s): island once, then villagers.</summary>
        public void ApplyWeather(WeatherState weather, float deltaDays)
        {
            CurrentWeather = weather;
            if (!weatherTime.ContainsKey(weather.Type)) weatherTime[weather.Type] = 0f;
            weatherTime[weather.Type] += deltaDays;
            float best = -1f;
            foreach (var kv in weatherTime)
                if (kv.Value > best) { best = kv.Value; DominantWeather = kv.Key; }

            Island.Tick(weather, deltaDays);
            float faith = Village.Faith();
            for (int i = 0; i < Village.Villagers.Count; i++)
            {
                var v = Village.Villagers[i];
                var act = v.TickWithIsland(weather, deltaDays, Village.Day,
                    Island.River, Island.FireRisk, faith, Village.LowFaithDays);
                if (act == VillagerAction.BuildShrine && !v.HasBuiltShrine)
                {
                    v.HasBuiltShrine = true;
                    Village.Shrines++;
                    dayEvents.Add(v.Personality.Name + " raised a shrine.");
                }
                if (act == VillagerAction.Protest) Village.Protests++;
            }
            Village.RiverLevel = Island.River;
            Village.FireRisk = Island.FireRisk;
            if (Island.Flooded && dayEvents.Count < 6) dayEvents.Add("The river flooded a field.");
        }

        /// <returns>Day-end chronicle text (offline template; LLM upgrade later).</returns>
        public string EndDay()
        {
            float faithEnd = Village.Faith();
            string chronicle = ChronicleGenerator.Generate(
                Village.Day, DominantWeather, dayEvents, faithAtDawn, faithEnd);
            Village.TickDay();
            Island.TickGrowth(Village.Faith(), Village.Shrines, Village.Protests);
            Village.SessionCount++;
            // Reward fulfilled prayer.
            foreach (var v in Village.Villagers)
            {
                if (Prayer != null && v.Personality.Name == Prayer.VillagerName && Prayer.IsFulfilled(v))
                    v.Needs.Faith = VillagerNeeds.Clamp(v.Needs.Faith + Prayer.RewardFaith);
            }
            Prayer = DailyPrayer.Pick(Village);
            faithAtDawn = Village.Faith();
            dayEvents.Clear();
            weatherTime.Clear();
            DominantWeather = WeatherType.Calm;
            return chronicle;
        }
    }
}
