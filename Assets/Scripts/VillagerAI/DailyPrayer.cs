using System;

namespace TabletopWeatherGod.VillagerAI
{
    /// <summary>
    /// Daily prayer: each new session opens with one villager's specific request
    /// (short goal, e.g. "my field is dry — grant sun"). Pure data + picker so
    /// Unity can show it in the arrival moment. Reward is faith on fulfilment.
    /// </summary>
    [Serializable]
    public class DailyPrayer
    {
        public string VillagerName;
        public string Need;        // food | water | warmth | safety
        public float TargetValue;  // e.g. water above 50
        public string RequestText;
        public float RewardFaith = 8f;

        public bool IsFulfilled(Villager v)
        {
            switch (Need)
            {
                case "food": return v.Needs.Food >= TargetValue;
                case "water": return v.Needs.Water >= TargetValue;
                case "warmth": return v.Needs.Warmth >= TargetValue;
                case "safety": return v.Needs.Safety >= TargetValue;
                default: return false;
            }
        }

        /// <summary>Pick the neediest villager's lowest need as today's prayer.</summary>
        public static DailyPrayer Pick(VillageState village)
        {
            var prayer = new DailyPrayer { VillagerName = "Asha", Need = "water", TargetValue = 50f };
            if (village == null || village.Villagers.Count == 0)
            {
                prayer.RequestText = "Grant us water, great one.";
                return prayer;
            }
            Villager worst = village.Villagers[0];
            foreach (var v in village.Villagers)
                if (v.Needs.LowestNeed() < worst.Needs.LowestNeed()) worst = v;

            string need = "water";
            float cur = worst.Needs.Water;
            if (worst.Needs.Food <= cur) { need = "food"; cur = worst.Needs.Food; }
            if (worst.Needs.Warmth < cur && worst.Needs.Warmth < 50f) { need = "warmth"; cur = worst.Needs.Warmth; }
            if (worst.Needs.Safety < cur && worst.Needs.Safety < 50f) { need = "safety"; cur = worst.Needs.Safety; }

            prayer.VillagerName = worst.Personality.Name;
            prayer.Need = need;
            prayer.TargetValue = 50f;
            prayer.RequestText = RequestLine(worst.Personality.Name, need);
            return prayer;
        }

        private static string RequestLine(string name, string need)
        {
            switch (need)
            {
                case "food": return name + " asks: my jar is empty — grant sun for the crops.";
                case "warmth": return name + " asks: the little one shivers — grant us warmth.";
                case "safety": return name + " asks: the river climbs — grant us calm.";
                default: return name + " asks: my field is dry — grant rain.";
            }
        }
    }
}
