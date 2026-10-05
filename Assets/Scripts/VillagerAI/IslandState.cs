using System;

namespace TabletopWeatherGod.VillagerAI
{
    /// <summary>
    /// Island simulation (authoritative, ticked ONCE per step by GameSession).
    /// Crops/river/fire 0..100. Huts + temple visualize faith growth.
    /// NOTE: VillagerNeeds.ApplyWeather still drifts a per-villager river/fire
    /// copy from Phase 1; the game should use IslandState.Tick for the island
    /// plus VillagerNeeds.ApplyWeatherWithIsland (no island mutation) per villager.
    /// Numbers mirror Tools/BalanceSim/sim.py island_tick — keep in sync.
    /// </summary>
    [Serializable]
    public class IslandState
    {
        public float Crops = 50f;
        public float River = 40f;
        public float FireRisk = 20f;
        public int Huts = 3;
        public float TempleProgress = 0f; // 0..100, shrine/protest driven
        public bool Flooded = false;

        /// <summary>Tick island once per step. This is the "make it rain and a crop grows" proof.</summary>
        public void Tick(WeatherState weather, float deltaDays)
        {
            float i = weather.Intensity;
            Flooded = false;
            switch (weather.Type)
            {
                case WeatherType.Sun:
                    // Sun grows crops only when there is water to drink (irrigation).
                    float irrigation = River > 25f ? 1f : 0.2f;
                    Crops += 8f * i * deltaDays * irrigation;
                    River -= 6f * deltaDays * i;
                    FireRisk += 8f * i * deltaDays;
                    break;
                case WeatherType.Rain:
                    River += 14f * i * deltaDays;
                    FireRisk -= 12f * i * deltaDays;
                    if (i > 0.75f || River > 85f)
                    {
                        Crops -= 12f * i * deltaDays; // flood ruins fields
                        Flooded = true;
                    }
                    else
                    {
                        Crops += 10f * i * deltaDays; // rain grows crops
                    }
                    break;
                case WeatherType.Wind:
                    Crops -= 3f * i * deltaDays;
                    River -= 2f * i * deltaDays;
                    break;
                case WeatherType.Calm:
                default:
                    Crops += 1f * deltaDays;
                    River -= 4f * deltaDays;
                    break;
                case WeatherType.Lightning:
                    FireRisk += 12f * i * deltaDays;
                    Crops -= 5f * i * deltaDays;
                    break;
                case WeatherType.Snow:
                    Crops -= 4f * i * deltaDays;
                    River += 4f * i * deltaDays;
                    break;
                case WeatherType.Rainbow:
                    break;
            }
            // Drought withers fields; untended fire spreads.
            if (River < 15f) Crops -= 6f * deltaDays;
            Crops = VillagerNeeds.Clamp(Crops);
            River = VillagerNeeds.Clamp(River);
            FireRisk = VillagerNeeds.Clamp(FireRisk);
        }

        /// <summary>Village growth visuals: called at day end with current faith/shrines.</summary>
        public void TickGrowth(float villageFaith, int shrines, int protests)
        {
            if (villageFaith > 65f && shrines > 0) TempleProgress += 20f;
            TempleProgress = VillagerNeeds.Clamp(TempleProgress);
            int wantHuts = 3;
            if (villageFaith > 60f) wantHuts = 4;
            if (villageFaith > 75f || shrines >= 2) wantHuts = 5;
            if (villageFaith > 85f && shrines >= 3) wantHuts = 6;
            if (villageFaith < 25f && protests > 0) wantHuts = Math.Max(2, wantHuts - 1); // rebellion ruins a hut
            Huts = wantHuts;
        }
    }
}
