using System;
using System.Collections.Generic;
using System.Text;

namespace TabletopWeatherGod.VillagerAI.Dialogue
{
    /// <summary>
    /// Day-end chronicle (offline template version). At day's end the day's
    /// event log becomes a 3-4 line village story in the village's own words.
    /// LLM version (see Prompts/chronicle_prompt.txt) upgrades this async;
    /// this template is instant and always available.
    /// </summary>
    public static class ChronicleGenerator
    {
        public static string Generate(int day, WeatherType dominantWeather,
            List<string> events, float faithStart, float faithEnd)
        {
            var sb = new StringBuilder();
            sb.Append("Day " + day + ". ");
            sb.Append(WeatherLine(dominantWeather));
            sb.Append(" ");
            int n = Math.Min(2, events.Count);
            for (int i = 0; i < n; i++)
            {
                sb.Append(events[i].Trim());
                if (!events[i].TrimEnd().EndsWith(".")) sb.Append(".");
                sb.Append(" ");
            }
            float d = faithEnd - faithStart;
            if (d > 5f) sb.Append("Faith burns brighter.");
            else if (d < -5f) sb.Append("Doubt creeps between the huts.");
            else sb.Append("The village endures.");
            return sb.ToString().Trim();
        }

        private static string WeatherLine(WeatherType w)
        {
            switch (w)
            {
                case WeatherType.Sun: return "The sun lay warm on the roofs.";
                case WeatherType.Rain: return "Rain drummed the thatch.";
                case WeatherType.Wind: return "Wind worried the palms.";
                case WeatherType.Lightning: return "Lightning split the sky.";
                case WeatherType.Snow: return "Strange white cold fell.";
                case WeatherType.Rainbow: return "A rainbow arched over the bay.";
                default: return "The air lay still.";
            }
        }
    }
}
