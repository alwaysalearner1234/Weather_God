using System;
using System.Collections.Generic;
using System.Text;

namespace TabletopWeatherGod.VillagerAI.Dialogue
{
    /// <summary>
    /// AI proxy contract. The APK NEVER holds an LLM API key: Unity sends a
    /// filled prompt (see Dialogue/Prompts/*.txt) to our Lambda proxy, which
    /// adds the key server-side. Timeout 1500 ms, then fallback lines.
    /// Lambda contract (to implement in week 5):
    ///   POST {proxyUrl}/dialogue  {"prompt": "...", "maxWords": 25}
    ///   -&gt; {"text": "...", "model": "amazon.nova-micro-v1:0"}
    ///   POST {proxyUrl}/chronicle {"prompt": "...", "maxWords": 60}
    /// </summary>
    [Serializable]
    public class DialogueRequest
    {
        public string VillagerName;
        public string Job;
        public string Trait;
        public float Food, Water, Warmth, Safety, Faith;
        public string Mood;
        public float Relationship;
        public List<string> Memories = new List<string>();
        public string Weather;
        public float Intensity;
        public int MaxWords = 25;
    }

    [Serializable]
    public class DialogueResponse
    {
        public string Text;
        public string Model; // e.g. "fallback" | "amazon.nova-micro-v1:0"
        public bool FromFallback;
        public int LatencyMs;
    }

    [Serializable]
    public class ChronicleRequest
    {
        public int Day;
        public string Weather;
        public List<string> Events = new List<string>();
        public float FaithStart;
        public float FaithEnd;
        public int MaxWords = 60;
    }

    public interface IDialogueService
    {
        DialogueResponse GetLine(DialogueRequest req, int seed);
        string GetChronicle(ChronicleRequest req);
    }

    /// <summary>
    /// Offline service: instant, always available. Game must never wait on net.
    /// Unity upgrades to LlmProxyService (async, timeout + ContentFilter) later;
    /// this stays as the guaranteed path and the Wi-Fi-off path.
    /// </summary>
    public class FallbackDialogueService : IDialogueService
    {
        public DialogueResponse GetLine(DialogueRequest req, int seed)
        {
            Mood mood = Mood.Content;
            try { mood = (Mood)Enum.Parse(typeof(Mood), req.Mood, true); }
            catch { mood = Mood.Content; }
            string topic = TopicFor(req.Weather);
            string text = FallbackLines.Pick(mood, topic, seed);
            return new DialogueResponse { Text = text, Model = "fallback", FromFallback = true, LatencyMs = 0 };
        }

        public string GetChronicle(ChronicleRequest req)
        {
            WeatherType w = WeatherType.Calm;
            try { w = (WeatherType)Enum.Parse(typeof(WeatherType), req.Weather, true); }
            catch { w = WeatherType.Calm; }
            return ChronicleGenerator.Generate(req.Day, w, req.Events, req.FaithStart, req.FaithEnd);
        }

        private static string TopicFor(string weather)
        {
            if (string.IsNullOrEmpty(weather)) return "work";
            string w = weather.ToLowerInvariant();
            if (w.Contains("sun")) return "sun";
            if (w.Contains("rain")) return "rain";
            if (w.Contains("calm")) return "calm";
            if (w.Contains("cold") || w.Contains("snow")) return "cold";
            if (w.Contains("flood")) return "flood";
            if (w.Contains("storm") || w.Contains("wind")) return "storm";
            if (w.Contains("pray")) return "pray";
            return "work";
        }
    }

    /// <summary>Prompt filler: builds the exact string POSTed to the proxy.</summary>
    public static class PromptFiller
    {
        public static string FillDialogue(string template, DialogueRequest req)
        {
            var sb = new StringBuilder(template);
            sb.Replace("{name}", req.VillagerName);
            sb.Replace("{job}", req.Job);
            sb.Replace("{trait}", req.Trait);
            sb.Replace("{food}", req.Food.ToString("F0"));
            sb.Replace("{water}", req.Water.ToString("F0"));
            sb.Replace("{warmth}", req.Warmth.ToString("F0"));
            sb.Replace("{safety}", req.Safety.ToString("F0"));
            sb.Replace("{faith}", req.Faith.ToString("F0"));
            sb.Replace("{mood}", req.Mood);
            sb.Replace("{relationship}", req.Relationship.ToString("F0"));
            for (int i = 0; i < 3; i++)
                sb.Replace("{memory_" + (i + 1) + "}",
                    i < req.Memories.Count ? req.Memories[i] : "-");
            sb.Replace("{weather}", req.Weather);
            sb.Replace("{intensity}", req.Intensity.ToString("F2"));
            return sb.ToString();
        }

        public static string FillChronicle(string template, ChronicleRequest req)
        {
            var sb = new StringBuilder(template);
            sb.Replace("{day}", req.Day.ToString());
            sb.Replace("{weather}", req.Weather);
            for (int i = 0; i < 4; i++)
                sb.Replace("{event_" + (i + 1) + "}",
                    i < req.Events.Count ? req.Events[i] : "-");
            sb.Replace("{faith_start}", req.FaithStart.ToString("F0"));
            sb.Replace("{faith_end}", req.FaithEnd.ToString("F0"));
            return sb.ToString();
        }
    }
}
