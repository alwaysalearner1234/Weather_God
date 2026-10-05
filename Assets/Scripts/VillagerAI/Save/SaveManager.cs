using System;
using System.IO;

namespace TabletopWeatherGod.VillagerAI.Save
{
    /// <summary>
    /// File store abstraction so save logic is testable without Unity.
    /// Unity passes Application.persistentDataPath as baseDir via FileStore.
    /// JSON conversion is injected: Unity side uses JsonUtility.ToJson/FromJson,
    /// tests can use any serializer. No behaviour change to VillageSave DTOs.
    /// </summary>
    public interface IFileStore
    {
        void Write(string path, string contents);
        string Read(string path);
        bool Exists(string path);
    }

    public class FileStore : IFileStore
    {
        private readonly string baseDir;
        public FileStore(string baseDir) { this.baseDir = baseDir; }

        private string Full(string path) { return Path.Combine(baseDir, path); }

        public void Write(string path, string contents)
        {
            string full = Full(path);
            string dir = Path.GetDirectoryName(full);
            if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(full, contents);
        }

        public string Read(string path) { return File.ReadAllText(Full(path)); }
        public bool Exists(string path) { return File.Exists(Full(path)); }
    }

    /// <summary>In-memory store for tests.</summary>
    public class MemoryStore : IFileStore
    {
        private readonly System.Collections.Generic.Dictionary<string, string> files =
            new System.Collections.Generic.Dictionary<string, string>();
        public void Write(string path, string contents) { files[path] = contents; }
        public string Read(string path) { return files[path]; }
        public bool Exists(string path) { return files.ContainsKey(path); }
    }

    public class SaveManager
    {
        public const string SlotPath = "saves/village.json";
        private readonly IFileStore store;
        private readonly Func<VillageSave, string> serialize;
        private readonly Func<string, VillageSave> deserialize;

        // Unity wiring example:
        //   new SaveManager(new FileStore(Application.persistentDataPath),
        //       JsonUtility.ToJson, j => JsonUtility.FromJson<VillageSave>(j));
        public SaveManager(IFileStore store,
            Func<VillageSave, string> serialize,
            Func<string, VillageSave> deserialize)
        {
            this.store = store;
            this.serialize = serialize;
            this.deserialize = deserialize;
        }

        public void Save(VillageState village, IslandState island)
        {
            var save = VillageSave.FromVillage(village);
            save.RiverLevel = island != null ? island.River : save.RiverLevel;
            save.FireRisk = island != null ? island.FireRisk : save.FireRisk;
            store.Write(SlotPath, serialize(save));
        }

        public bool HasSave() { return store.Exists(SlotPath); }

        /// <returns>Null when no save exists.</returns>
        public VillageSave Load()
        {
            if (!store.Exists(SlotPath)) return null;
            return deserialize(store.Read(SlotPath));
        }

        /// <summary>Restore live state from a save (villagers + island + counters).</summary>
        public static void Restore(VillageSave save, VillageState village, IslandState island)
        {
            if (save == null || village == null) return;
            village.Day = save.Day;
            village.SessionCount = save.SessionCount;
            village.Shrines = save.Shrines;
            village.Protests = save.Protests;
            village.LowFaithDays = save.LowFaithDays;
            village.RiverLevel = save.RiverLevel;
            village.FireRisk = save.FireRisk;
            village.Villagers.Clear();
            foreach (var vs in save.Villagers)
            {
                var v = new Villager(vs.Name, vs.Job, vs.Trait);
                v.Needs.Food = vs.Food; v.Needs.Water = vs.Water;
                v.Needs.Warmth = vs.Warmth; v.Needs.Safety = vs.Safety;
                v.Needs.Faith = vs.Faith;
                v.HasBuiltShrine = vs.HasBuiltShrine;
                v.Memory.Events.Clear();
                foreach (var m in vs.Memories)
                {
                    var e = new MemoryEvent(m.Day, m.Description, m.Valence, m.InitialSalience, m.Cause);
                    e.Salience = m.Salience;
                    v.Memory.Events.Add(e);
                }
                village.Villagers.Add(v);
            }
            if (island != null)
            {
                island.River = save.RiverLevel;
                island.FireRisk = save.FireRisk;
            }
        }
    }
}
