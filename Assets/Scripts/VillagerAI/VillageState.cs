using System;
using System.Collections.Generic;

namespace TabletopWeatherGod.VillagerAI
{
    /// <summary>
    /// Village-level state: faith (main score), era growth, shrine/protest counts.
    /// High faith grows the village (new huts, temple). Low faith for several
    /// days -&gt; protests, then rebellion. Era advances roughly over ~10 sessions.
    /// </summary>
    [Serializable]
    public class VillageState
    {
        public List<Villager> Villagers = new List<Villager>();
        public int Day = 1;
        public int SessionCount = 1;
        public float RiverLevel = 40f;
        public float FireRisk = 20f;
        public int Shrines = 0;
        public int Protests = 0;
        public int LowFaithDays = 0;

        public float Faith()
        {
            if (Villagers.Count == 0) return 50f;
            float sum = 0f;
            foreach (var v in Villagers) sum += v.Needs.Faith;
            return sum / Villagers.Count;
        }

        public Era Era()
        {
            if (SessionCount >= 8 || Shrines >= 3) return Era.Town;
            if (SessionCount >= 4 || Shrines >= 1) return Era.Hamlet;
            return Era.Camp;
        }

        public string EraPower()
        {
            switch (Era())
            {
                case Era.Hamlet: return "lightning";
                case Era.Town: return "snow";
                default: return "none";
            }
        }

        /// <summary>Advance one in-game day: decay memories, track low-faith streak.</summary>
        public void TickDay()
        {
            foreach (var v in Villagers) v.EndOfDay(Day);
            if (Faith() < 30f) LowFaithDays++;
            else LowFaithDays = 0;
            Day++;
        }

        public static VillageState CreateDefault(int count)
        {
            string[] names = { "Asha", "Bram", "Chiku", "Dev", "Esha", "Farid", "Gita", "Hari", "Ira", "Jai" };
            string[] jobs = { "farmer", "fisher", "builder", "elder", "child", "farmer", "fisher", "builder", "elder", "child" };
            string[] traits = { "optimistic", "gruff", "anxious", "devout", "mischievous", "optimistic", "gruff", "anxious", "devout", "mischievous" };
            var village = new VillageState();
            for (int i = 0; i < count && i < names.Length; i++)
                village.Villagers.Add(new Villager(names[i], jobs[i], traits[i]));
            return village;
        }
    }
}
