using System;

namespace TabletopWeatherGod.VillagerAI
{
    /// <summary>Shipped MVP weather powers. Lightning/Snow/Rainbow are era-locked stretch.</summary>
    public enum WeatherType
    {
        Sun = 0,
        Rain = 1,
        Wind = 2,
        Calm = 3,
        Lightning = 4, // era unlock (stretch)
        Snow = 5,      // era unlock (stretch)
        Rainbow = 6    // era unlock (stretch)
    }

    /// <summary>Utility-AI actions. Protest/Rebel are village-level consequences.</summary>
    public enum VillagerAction
    {
        Work = 0,
        SeekShelter = 1,
        Pray = 2,
        Complain = 3,
        BuildShrine = 4,
        Protest = 5,
        Rebel = 6,
        Idle = 7
    }

    public enum Mood
    {
        Grateful = 0,
        Content = 1,
        Worried = 2,
        Angry = 3,
        Defiant = 4
    }

    public enum Era
    {
        Camp = 0,   // sessions ~1-3
        Hamlet = 1, // sessions ~4-7
        Town = 2    // sessions ~8+
    }

    /// <summary>Lightweight weather snapshot passed to Villager.Tick. Intensity 0..1.</summary>
    [Serializable]
    public struct WeatherState
    {
        public WeatherType Type;
        public float Intensity; // 0..1

        public WeatherState(WeatherType type, float intensity)
        {
            Type = type;
            Intensity = intensity < 0f ? 0f : (intensity > 1f ? 1f : intensity);
        }

        public static WeatherState Calm { get { return new WeatherState(WeatherType.Calm, 0.3f); } }
    }

    public static class NeedsTuning
    {
        public const float Min = 0f;
        public const float Max = 100f;
        public const float StartNeed = 70f;
        public const float StartFaith = 55f;
    }
}
