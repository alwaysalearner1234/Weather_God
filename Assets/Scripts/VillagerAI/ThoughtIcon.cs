namespace TabletopWeatherGod.VillagerAI
{
    /// <summary>
    /// Thought icons over heads: the "read the village" UI. Pure function of
    /// villager + island state so game dev can map each value to a sprite.
    /// Priority: rebellion &gt; flood/storm &gt; thirst/hunger/cold &gt; prayer/joy.
    /// A new player must see a need icon within seconds of arrival.
    /// </summary>
    public enum ThoughtIcon
    {
        None = 0,
        DryField = 1,   // farmer thirsty: dry field
        Thirsty = 2,    // non-farmer thirsty
        Hungry = 3,     // dry field / empty jar
        Cold = 4,       // cold family
        FloodedHut = 5, // flooded hut
        Storm = 6,      // seek shelter
        Praying = 7,    // praying / offering
        Happy = 8,      // grateful
        Angry = 9,      // complain
        Rebel = 10,     // protest / rebel
    }

    public static class ThoughtIcons
    {
        public static ThoughtIcon GetIcon(Villager v, IslandState island, WeatherState weather)
        {
            if (v.LastAction == VillagerAction.Protest || v.LastAction == VillagerAction.Rebel)
                return ThoughtIcon.Rebel;
            if (v.Mood == Mood.Defiant)
                return ThoughtIcon.Rebel;
            if (v.LastAction == VillagerAction.Complain || v.Mood == Mood.Angry)
                return ThoughtIcon.Angry;
            bool storm = (weather.Type == WeatherType.Rain && weather.Intensity > 0.7f)
                      || (weather.Type == WeatherType.Wind && weather.Intensity > 0.7f)
                      || weather.Type == WeatherType.Lightning
                      || weather.Type == WeatherType.Snow;
            if (island != null && island.Flooded) return ThoughtIcon.FloodedHut;
            if (v.Needs.Safety < 25f) return ThoughtIcon.FloodedHut;
            if (storm && v.LastAction == VillagerAction.SeekShelter) return ThoughtIcon.Storm;
            if (v.Needs.Water < 30f)
                return v.Personality.Job == "farmer" ? ThoughtIcon.DryField : ThoughtIcon.Thirsty;
            if (v.Needs.Food < 30f) return ThoughtIcon.Hungry;
            if (v.Needs.Warmth < 30f) return ThoughtIcon.Cold;
            if (v.LastAction == VillagerAction.Pray) return ThoughtIcon.Praying;
            if (v.Mood == Mood.Grateful) return ThoughtIcon.Happy;
            return ThoughtIcon.None;
        }
    }
}
