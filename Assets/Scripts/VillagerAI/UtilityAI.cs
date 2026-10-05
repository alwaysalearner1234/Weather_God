using System;

namespace TabletopWeatherGod.VillagerAI
{
    /// <summary>
    /// Layer 3: decisions (on-device utility AI). Every few seconds each villager
    /// scores possible actions and picks the best. Scores 0..100, argmax wins.
    /// Tuned so: needs fine -&gt; work; storm/cold -&gt; shelter; low need + high
    /// faith -&gt; pray; low need + falling faith -&gt; complain; high faith +
    /// high relationship -&gt; shrine (once); sustained low faith -&gt; protest/rebel.
    /// </summary>
    public static class UtilityAI
    {
        public static VillagerAction ChooseAction(Villager v, WeatherState weather,
            float villageFaith, int lowFaithDays)
        {
            float best = float.NegativeInfinity;
            VillagerAction action = VillagerAction.Idle;

            float s;
            s = ScoreWork(v);            if (s > best) { best = s; action = VillagerAction.Work; }
            s = ScoreShelter(v, weather);if (s > best) { best = s; action = VillagerAction.SeekShelter; }
            s = ScorePray(v);            if (s > best) { best = s; action = VillagerAction.Pray; }
            s = ScoreComplain(v);        if (s > best) { best = s; action = VillagerAction.Complain; }
            s = ScoreShrine(v);          if (s > best) { best = s; action = VillagerAction.BuildShrine; }
            s = ScoreProtest(v, villageFaith, lowFaithDays);
                                         if (s > best) { best = s; action = VillagerAction.Protest; }
            s = ScoreRebel(v, lowFaithDays);
                                         if (s > best) { best = s; action = VillagerAction.Rebel; }
            return action;
        }

        public static float ScoreWork(Villager v)
        {
            // Default when needs are fine, urgent when hungry/thirsty (work before prayer).
            float minNeed = v.Needs.LowestNeed();
            float score = 30f + (minNeed - 50f) * 0.6f;
            if (minNeed < 30f) score += (30f - minNeed) * 2.0f;
            if (v.Needs.Safety < 30f) score -= 40f;
            if (v.Needs.Warmth < 20f) score -= 30f;
            return score;
        }

        public static float ScoreShelter(Villager v, WeatherState w)
        {
            float score = (70f - v.Needs.Safety) * 1.2f;
            if (v.Needs.Warmth < 25f) score += (25f - v.Needs.Warmth) * 1.5f;
            bool storm = (w.Type == WeatherType.Rain && w.Intensity > 0.7f)
                      || (w.Type == WeatherType.Wind && w.Intensity > 0.7f)
                      || w.Type == WeatherType.Lightning
                      || w.Type == WeatherType.Snow;
            if (storm) score += 35f;
            return score;
        }

        public static float ScorePray(Villager v)
        {
            if (!v.Needs.AnyNeedBelow(40f)) return -10f;
            float score = (v.Needs.Faith - 40f) * 1.5f + (40f - v.Needs.LowestNeed()) * 0.8f;
            // Starving/thirsty villagers work first; cap prayer so work can win.
            if (v.Needs.Food < 25f || v.Needs.Water < 25f) score = Math.Min(score, 25f);
            return score;
        }

        public static float ScoreComplain(Villager v)
        {
            if (!v.Needs.AnyNeedBelow(40f)) return -10f;
            // Complains when a need is low AND faith is falling/low.
            float score = (40f - v.Needs.LowestNeed()) * 1.2f + (50f - v.Needs.Faith) * 0.8f;
            if (v.Relationship < -10f) score += 15f;
            return score;
        }

        public static float ScoreShrine(Villager v)
        {
            if (v.HasBuiltShrine) return -100f;
            if (v.Needs.Faith < 60f || v.Relationship < 25f) return -10f;
            return 40f + (v.Needs.Faith - 60f) * 1.5f + (v.Relationship - 25f);
        }

        public static float ScoreProtest(Villager v, float villageFaith, int lowFaithDays)
        {
            if (villageFaith > 35f || lowFaithDays < 2) return -100f;
            return 45f + (35f - villageFaith) + lowFaithDays * 8f - v.Needs.Faith * 0.2f;
        }

        public static float ScoreRebel(Villager v, int lowFaithDays)
        {
            if (v.Needs.Faith > 20f || lowFaithDays < 4) return -100f;
            return 50f + (20f - v.Needs.Faith) * 1.5f + lowFaithDays * 10f;
        }
    }
}
