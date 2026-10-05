using System;
using System.Collections.Generic;

namespace TabletopWeatherGod.VillagerAI.Save
{
    /// <summary>
    /// JSON DTOs for persistence. Save to Application.persistentDataPath at
    /// end of each day + OnApplicationPause. On resume, fast-forward capped at
    /// 3 in-game days and show "while you were away".
    /// NOTE: Unity side serializes with JsonUtility; keep fields public + simple.
    /// </summary>
    [Serializable]
    public class VillagerSave
    {
        public string Name;
        public string Job;
        public string Trait;
        public float Food, Water, Warmth, Safety, Faith;
        public List<MemoryEventSave> Memories = new List<MemoryEventSave>();
        public bool HasBuiltShrine;
    }

    [Serializable]
    public class MemoryEventSave
    {
        public int Day;
        public string Description;
        public float Valence;
        public float Salience;
        public float InitialSalience;
        public string Cause;
    }

    [Serializable]
    public class VillageSave
    {
        public int Version = 1;
        public int Day;
        public int SessionCount;
        public float RiverLevel;
        public float FireRisk;
        public int Shrines;
        public int Protests;
        public int LowFaithDays;
        public string LastPlayedUtc;
        public List<VillagerSave> Villagers = new List<VillagerSave>();

        public static VillageSave FromVillage(VillageState village)
        {
            var save = new VillageSave
            {
                Day = village.Day,
                SessionCount = village.SessionCount,
                RiverLevel = village.RiverLevel,
                FireRisk = village.FireRisk,
                Shrines = village.Shrines,
                Protests = village.Protests,
                LowFaithDays = village.LowFaithDays,
                LastPlayedUtc = DateTime.UtcNow.ToString("o"),
            };
            foreach (var v in village.Villagers)
            {
                var vs = new VillagerSave
                {
                    Name = v.Personality.Name,
                    Job = v.Personality.Job,
                    Trait = v.Personality.Trait,
                    Food = v.Needs.Food,
                    Water = v.Needs.Water,
                    Warmth = v.Needs.Warmth,
                    Safety = v.Needs.Safety,
                    Faith = v.Needs.Faith,
                    HasBuiltShrine = v.HasBuiltShrine,
                };
                foreach (var m in v.Memory.Events)
                {
                    vs.Memories.Add(new MemoryEventSave
                    {
                        Day = m.Day, Description = m.Description, Valence = m.Valence,
                        Salience = m.Salience, InitialSalience = m.InitialSalience, Cause = m.Cause,
                    });
                }
                save.Villagers.Add(vs);
            }
            return save;
        }

        /// <summary>Days to fast-forward on resume: real days elapsed, capped 0..3.</summary>
        public static int AwayDays(string lastPlayedUtc, string nowUtc)
        {
            DateTime a, b;
            if (!DateTime.TryParse(lastPlayedUtc, out a)) return 0;
            if (!DateTime.TryParse(nowUtc, out b)) return 1;
            int d = (int)Math.Floor((b - a).TotalDays);
            if (d < 0) d = 0;
            if (d > 3) d = 3;
            return d;
        }
    }
}
