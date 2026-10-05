using System;

namespace TabletopWeatherGod.VillagerAI
{
    /// <summary>
    /// Where a villager wants to go. Plain data (no Unity deps): game dev maps
    /// each SlotIndex to a world anchor (field/fishing hut/shelter/shrine/square).
    /// Villagers wander and work via these intents; AI ticks every few seconds.
    /// </summary>
    public enum IntentTarget
    {
        Wander = 0,
        WorkSite = 1,
        Shelter = 2,
        Shrine = 3,
        ProtestSquare = 4
    }

    [Serializable]
    public struct MovementIntent
    {
        public IntentTarget Target;
        public int SlotIndex; // deterministic slot so villagers spread out
        public float Speed;   // 0.5 walk, 1.0 hurry (shelter/storm)

        public MovementIntent(IntentTarget target, int slot, float speed)
        {
            Target = target; SlotIndex = slot; Speed = speed;
        }
    }

    public static class VillagerDirector
    {
        /// <summary>Map current action/mood to a movement intent. Pure + deterministic.</summary>
        public static MovementIntent NextIntent(Villager v, int villagerIndex, int tickIndex)
        {
            int slots = 6;
            int slot = (villagerIndex + tickIndex) % slots;
            switch (v.LastAction)
            {
                case VillagerAction.SeekShelter:
                    return new MovementIntent(IntentTarget.Shelter, villagerIndex % 3, 1.0f);
                case VillagerAction.Work:
                    return new MovementIntent(IntentTarget.WorkSite, WorkSlot(v), 0.5f);
                case VillagerAction.Pray:
                case VillagerAction.BuildShrine:
                    return new MovementIntent(IntentTarget.Shrine, 0, 0.5f);
                case VillagerAction.Protest:
                case VillagerAction.Rebel:
                    return new MovementIntent(IntentTarget.ProtestSquare, 0, 0.7f);
                case VillagerAction.Complain:
                    // Complainers seek each other: pair up by index.
                    return new MovementIntent(IntentTarget.Wander, (villagerIndex % 3), 0.7f);
                default:
                    return new MovementIntent(IntentTarget.Wander, slot, 0.5f);
            }
        }

        private static int WorkSlot(Villager v)
        {
            // Farmers to fields (0-1), fishers to shore (2-3), builders to huts (4-5).
            int h = 0;
            if (v.Personality.Job != null)
                foreach (char c in v.Personality.Job) h = (h * 31 + c) % 997;
            switch (v.Personality.Job)
            {
                case "farmer": return h % 2;
                case "fisher": return 2 + (h % 2);
                case "builder": return 4 + (h % 2);
                default: return h % 6;
            }
        }
    }
}
