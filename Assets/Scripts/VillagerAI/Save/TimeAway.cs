using System;
using System.Collections.Generic;

namespace TabletopWeatherGod.VillagerAI.Save
{
    /// <summary>
    /// Time-away simulation: on resume, fast-forward capped at 3 in-game days
    /// with gentle weather, then show a "while you were away" summary so the
    /// island always feels alive. Operates on live objects (no JSON needed).
    /// </summary>
    public static class TimeAway
    {
        public static List<string> Simulate(VillageState village, IslandState island, int awayDays, int seed)
        {
            var lines = new List<string>();
            if (village == null || island == null) return lines;
            if (awayDays <= 0) return lines;
            if (awayDays > 3) awayDays = 3;

            float faithBefore = village.Faith();
            float cropsBefore = island.Crops;
            int shrinesBefore = village.Shrines;
            var rnd = new Random(seed);

            WeatherType[] gentle = { WeatherType.Calm, WeatherType.Sun, WeatherType.Rain, WeatherType.Calm };
            for (int d = 0; d < awayDays; d++)
            {
                var w = new WeatherState(gentle[rnd.Next(gentle.Length)], 0.3f + (float)rnd.NextDouble() * 0.3f);
                island.Tick(w, 1f);
                float faith = village.Faith();
                foreach (var v in village.Villagers)
                    v.TickWithIsland(w, 1f, village.Day, island.River, island.FireRisk, faith, village.LowFaithDays);
                foreach (var v in village.Villagers)
                {
                    if (v.LastAction == VillagerAction.BuildShrine && !v.HasBuiltShrine)
                    {
                        v.HasBuiltShrine = true;
                        village.Shrines++;
                    }
                    if (v.LastAction == VillagerAction.Protest) village.Protests++;
                }
                village.TickDay();
            }
            island.TickGrowth(village.Faith(), village.Shrines, village.Protests);

            lines.Add("While you were away (" + awayDays + " day" + (awayDays > 1 ? "s" : "") + "):");
            float dc = island.Crops - cropsBefore;
            lines.Add(dc >= 0 ? "Crops grew (+" + dc.ToString("F0") + ")." : "Crops withered (" + dc.ToString("F0") + ").");
            if (island.Flooded) lines.Add("The river flooded a field.");
            if (village.Shrines > shrinesBefore) lines.Add("A new shrine stands on the hill.");
            if (village.Protests > 0) lines.Add("Some villagers grumble at the sky.");
            float df = village.Faith() - faithBefore;
            if (df > 3f) lines.Add("Faith burns brighter.");
            else if (df < -3f) lines.Add("Doubt creeps between the huts.");
            else lines.Add("The village endures.");
            return lines;
        }
    }
}
