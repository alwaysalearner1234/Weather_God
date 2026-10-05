using System;

namespace TabletopWeatherGod.VillagerAI
{
    /// <summary>
    /// Layer 1: needs (on-device, every tick). All needs 0..100.
    /// Weather deltas are per in-game day; Tick scales by deltaDays.
    /// Numbers are mirrored in Tools/BalanceSim/sim.py — keep in sync.
    /// </summary>
    [Serializable]
    public class VillagerNeeds
    {
        public float Food = NeedsTuning.StartNeed;
        public float Water = NeedsTuning.StartNeed;
        public float Warmth = NeedsTuning.StartNeed;
        public float Safety = NeedsTuning.StartNeed;
        public float Faith = NeedsTuning.StartFaith;

        public float LowestNeed()
        {
            return Math.Min(Math.Min(Food, Water), Math.Min(Warmth, Safety));
        }

        public bool AnyNeedBelow(float threshold)
        {
            return Food < threshold || Water < threshold || Warmth < threshold || Safety < threshold;
        }

        public Mood CurrentMood(float relationship)
        {
            if (Faith < 25f || relationship < -30f) return Faith < 15f ? Mood.Defiant : Mood.Angry;
            if (AnyNeedBelow(30f)) return Mood.Worried;
            if (Faith > 70f || relationship > 30f) return Mood.Grateful;
            return Mood.Content;
        }

        /// <summary>Apply one weather tick. riverLevel/fireRisk 0..100, passed by ref from island.</summary>
        public void ApplyWeather(WeatherState weather, float deltaDays, ref float riverLevel, ref float fireRisk)
        {
            float i = weather.Intensity;
            switch (weather.Type)
            {
                case WeatherType.Sun:
                    Food += 10f * i * deltaDays;
                    Water -= 6f * i * deltaDays;
                    Warmth += 15f * i * deltaDays;
                    Safety += 2f * i * deltaDays;
                    fireRisk += 8f * i * deltaDays;
                    riverLevel -= 6f * i * deltaDays;
                    break;
                case WeatherType.Rain:
                    Water += 22f * i * deltaDays;
                    Warmth -= 10f * i * deltaDays;
                    riverLevel += 14f * i * deltaDays;
                    if (i > 0.75f || riverLevel > 85f)
                    {
                        // Flood: hurts huts and farms.
                        Safety -= 15f * i * deltaDays;
                        Food -= 10f * i * deltaDays;
                    }
                    else
                    {
                        Food += 8f * i * deltaDays;
                        Safety -= 3f * i * deltaDays;
                    }
                    fireRisk -= 12f * i * deltaDays;
                    break;
                case WeatherType.Wind:
                    Water -= 3f * i * deltaDays;
                    Warmth -= 4f * i * deltaDays;
                    Food -= 2f * i * deltaDays;
                    Safety -= 6f * i * deltaDays;
                    riverLevel -= 2f * i * deltaDays;
                    break;
                case WeatherType.Calm:
                default:
                    Warmth += 2f * deltaDays;
                    Safety += 5f * deltaDays;
                    Food += 2f * deltaDays;
                    riverLevel -= 4f * deltaDays;
                    break;
                case WeatherType.Lightning:
                    // Stretch era power: dramatic, dangerous.
                    Safety -= 20f * i * deltaDays;
                    Warmth += 5f * i * deltaDays;
                    fireRisk += 12f * i * deltaDays;
                    break;
                case WeatherType.Snow:
                    Warmth -= 15f * i * deltaDays;
                    Water += 6f * i * deltaDays;
                    Safety -= 4f * i * deltaDays;
                    break;
                case WeatherType.Rainbow:
                    Faith += 10f * i * deltaDays;
                    break;
            }

            ApplyPassiveDrift(deltaDays);

            Food = Clamp(Food); Water = Clamp(Water); Warmth = Clamp(Warmth);
            Safety = Clamp(Safety); Faith = Clamp(Faith);
            riverLevel = Clamp(riverLevel); fireRisk = Clamp(fireRisk);

            // Secondary conditions (drought / cold / fire aftermath).
            if (Water < 20f) { Food -= 5f * deltaDays; Faith -= 2f * deltaDays; }
            if (Warmth < 20f) { Safety -= 5f * deltaDays; Faith -= 2f * deltaDays; }
            if (fireRisk > 85f) { Safety -= 6f * deltaDays; Food -= 4f * deltaDays; }

            Food = Clamp(Food); Water = Clamp(Water); Warmth = Clamp(Warmth);
            Safety = Clamp(Safety); Faith = Clamp(Faith);
        }

        /// <summary>
        /// Island-aware weather tick: applies need deltas WITHOUT mutating the
        /// island (GameSession ticks IslandState once, then calls this per villager).
        /// Flood is decided by the authoritative islandRiver + intensity.
        /// </summary>
        public void ApplyWeatherWithIsland(WeatherState weather, float deltaDays, float islandRiver, float islandFire)
        {
            float i = weather.Intensity;
            switch (weather.Type)
            {
                case WeatherType.Sun:
                    Food += 10f * i * deltaDays;
                    Water -= 6f * i * deltaDays;
                    Warmth += 15f * i * deltaDays;
                    Safety += 2f * i * deltaDays;
                    break;
                case WeatherType.Rain:
                    Water += 22f * i * deltaDays;
                    Warmth -= 10f * i * deltaDays;
                    if (i > 0.75f || islandRiver > 85f)
                    {
                        Safety -= 15f * i * deltaDays;
                        Food -= 10f * i * deltaDays;
                    }
                    else
                    {
                        Food += 8f * i * deltaDays;
                        Safety -= 3f * i * deltaDays;
                    }
                    break;
                case WeatherType.Wind:
                    Water -= 3f * i * deltaDays;
                    Warmth -= 4f * i * deltaDays;
                    Food -= 2f * i * deltaDays;
                    Safety -= 6f * i * deltaDays;
                    break;
                case WeatherType.Calm:
                default:
                    Warmth += 2f * deltaDays;
                    Safety += 5f * deltaDays;
                    Food += 2f * deltaDays;
                    break;
                case WeatherType.Lightning:
                    Safety -= 20f * i * deltaDays;
                    Warmth += 5f * i * deltaDays;
                    break;
                case WeatherType.Snow:
                    Warmth -= 15f * i * deltaDays;
                    Water += 6f * i * deltaDays;
                    Safety -= 4f * i * deltaDays;
                    break;
                case WeatherType.Rainbow:
                    Faith += 10f * i * deltaDays;
                    break;
            }

            ApplyPassiveDrift(deltaDays);

            Food = Clamp(Food); Water = Clamp(Water); Warmth = Clamp(Warmth);
            Safety = Clamp(Safety); Faith = Clamp(Faith);

            if (Water < 20f) { Food -= 5f * deltaDays; Faith -= 2f * deltaDays; }
            if (Warmth < 20f) { Safety -= 5f * deltaDays; Faith -= 2f * deltaDays; }
            if (islandFire > 85f) { Safety -= 6f * deltaDays; Food -= 4f * deltaDays; }

            Food = Clamp(Food); Water = Clamp(Water); Warmth = Clamp(Warmth);
            Safety = Clamp(Safety); Faith = Clamp(Faith);
        }

        public void ApplyPassiveDrift(float deltaDays)
        {
            Food -= 2f * deltaDays;
            Water -= 2f * deltaDays;
            // Warmth/Safety drift toward comfortable baselines.
            Warmth += (50f - Warmth) * 0.08f * deltaDays;
            Safety += (70f - Safety) * 0.08f * deltaDays;
            // Faith homeostasis: content villages recover, suffering villages doubt.
            if (LowestNeed() > 50f) Faith += 1f * deltaDays;
            else if (AnyNeedBelow(30f)) Faith -= 1.5f * deltaDays;
            else Faith += (50f - Faith) * 0.02f * deltaDays;
        }

        public void ApplyActionOutcome(VillagerAction action)
        {
            switch (action)
            {
                case VillagerAction.Work: Food += 9f; Water += 1f; break;
                case VillagerAction.SeekShelter: Safety += 10f; Warmth += 5f; break;
                case VillagerAction.Pray: Faith += 4f; break;
                case VillagerAction.Complain: Faith -= 0.5f; break;
                case VillagerAction.BuildShrine: Faith += 10f; break;
                case VillagerAction.Protest: Faith -= 1f; break;
                case VillagerAction.Rebel: Faith -= 2f; break;
            }
            Food = Clamp(Food); Water = Clamp(Water); Warmth = Clamp(Warmth);
            Safety = Clamp(Safety); Faith = Clamp(Faith);
        }

        public static float Clamp(float v)
        {
            if (v < NeedsTuning.Min) return NeedsTuning.Min;
            if (v > NeedsTuning.Max) return NeedsTuning.Max;
            return v;
        }
    }
}
