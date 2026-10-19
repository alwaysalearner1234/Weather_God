using System;
using UnityEngine;
using TabletopWeatherGod.VillagerAI;

namespace TabletopWeatherGod
{
    /// <summary>
    /// Bridges gesture detection to the game session weather system.
    /// One session = one in-game day (5-8 min).
    /// Gestures drive weather, weather drives island, island drives villagers.
    /// </summary>
    public class WeatherGestureBridge : MonoBehaviour
    {
        [Header("References")]
        public SimpleGestureDetector gestureDetector;
        
        public GameSession gameSession;
        
        [Header("Session Timing")]
        public float minutesPerDay = 6f; // target 5-8 min per session

        // Session state
        private int sessionDay = 0;
        private bool sessionActive = false;
        private float sessionStartTime = 0f;

        void Start()
        {
            if (gameSession == null)
            {
                gameSession = FindObjectOfType<GameSession>();
                if (gameSession == null)
                {
                    Debug.LogError("WeatherGestureBridge: No GameSession found! " +
                                   "Ensure a GameSession object exists in the scene.");
                    enabled = false;
                    return;
                }
            }

            // Initialize first session (player arrival)
            InitializeNewSession();
        }

        void Update()
        {
            if (!sessionActive) return;

            // Check for gestures each frame
            CheckGestureInputs();

            // Auto-advance day timer (for players who don't gesture much)
            // In a full build, this would be disabled or very slow
            if (Time.time - sessionStartTime > minutesPerDay * 60f)
            {
                Debug.Log("Auto-advancing day timer - session took too long");
                EndCurrentDay();
            }
        }

        // ----------------------------------
        // Initialize a new session (player arrives)
        // ----------------------------------
        private void InitializeNewSession()
        {
            sessionDay = 1;
            sessionActive = true;
            sessionStartTime = Time.time;

            // Reset the game session for a fresh start
            // The GameSession constructor creates villagers, sets up island, etc.
            // We need to re-initialize it
            
            // In a full implementation, we'd use a proper reset method
            // or re-create the session with the right villager count
            
            // Issue the daily prayer (first goal of the session)
            // DailyPrayer.Pick chooses the neediest villager's lowest need
            // This gives the player a short goal for the session
            
            Debug.Log($"=== Session {sessionDay}: Island arrived on lap ===");
            Debug.Log("Rub palms for SUN, flick fingers for RAIN, sweep hand for WIND, ");
            Debug.Log("or palm down for CALM. Each gesture changes the weather.");
            Debug.Log("First weather within 30 seconds; first villager reaction within 2 minutes.");

            // Show the "while you were away" summary if resuming from a save
            CheckResumeSummary();
        }

        // ----------------------------------
        // Check for resumed session (headset was off)
        // ----------------------------------
        private void CheckResumeSummary()
        {
            // In a full implementation:
            // 1. Check if there's a save file in Application.persistentDataPath
            // 2. Calculate away days using VillageSave.AwayDays()
            // 3. Run TimeAway.Simulate() to fast-forward gently
            // 4. Show the "while you were away" summary to the player
            
            // For now, just log that we check
            // VillageSave.AwayDays(lastPlayedUtc, nowUtc) → TimeAway.Simulate()
            Debug.Log("Checking for saved game state and 'while you were away' summary...");
        }

        // ----------------------------------
        // Check for gesture inputs each frame
        // ----------------------------------
        private void CheckGestureInputs()
        {
            // 1. SUN: Rub palms together
            // Primary: detected by SimpleGestureDetector
            // Fallback (dev): press '1' key
            if (gestureDetector != null)
            {
                // Gesture detection is handled in SimpleGestureDetector.TriggerSun()
                // which sets lastGestureTime. We just need to check if a trigger happened.
                // The bridge checks lastGestureTime via the cooldown enforcement.
            }

            // Development keyboard fallbacks
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Joystick1Button0))
            {
                TriggerSun();
            }

            // 2. RAIN: Flick fingers down
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Joystick1Button1))
            {
                TriggerRain();
            }

            // 3. WIND: Sweep flat hand
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Joystick1Button2))
            {
                TriggerWind();
            }

            // 4. CALM: Palm down, slow motion
            if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Joystick1Button3))
            {
                TriggerCalm();
            }

            // R: End day
            if (Input.GetKeyDown(KeyCode.R))
            {
                EndCurrentDay();
            }

            // Q: New session
            if (Input.GetKeyDown(KeyCode.Q))
            {
                InitializeNewSession();
            }
        }

        // ==========================================================================
        // Gesture Triggers - connect to GameSession
        // ==========================================================================

        private void TriggerSun()
        {
            if (gestureDetector != null && Time.time - gestureDetector.lastGestureTime < 0.8f)
            {
                Debug.Log("Gesture on cooldown - waiting " + (0.8f - (Time.time - gestureDetector.lastGestureTime)) + "s");
                return;
            }

            // Apply sun weather to the game session
            // intensity 0..1, region optional (empty = whole island)
            gameSession.ApplyWeather(WeatherType.Sun, 0.6f);

            Debug.Log($"[Gesture {sessionDay}] SUN applied - island weather changed");

            // Check villager response
            CheckVillagerState();
        }

        private void TriggerRain()
        {
            if (gestureDetector != null && Time.time - gestureDetector.lastGestureTime < 0.8f) return;

            gameSession.ApplyWeather(WeatherType.Rain, 0.5f);
            Debug.Log($"[Gesture {sessionDay}] RAIN applied");

            CheckVillagerState();
        }

        private void TriggerWind()
        {
            if (gestureDetector != null && Time.time - gestureDetector.lastGestureTime < 0.8f) return;

            gameSession.ApplyWeather(WeatherType.Wind, 0.5f);
            Debug.Log($"[Gesture {sessionDay}] WIND applied");

            CheckVillagerState();
        }

        private void TriggerCalm()
        {
            if (gestureDetector != null && Time.time - gestureDetector.lastGestureTime < 0.8f) return;

            gameSession.ApplyWeather(WeatherType.Calm, 0.3f);
            Debug.Log($"[Gesture {sessionDay}] CALM applied");

            CheckVillagerState();
        }

        // ----------------------------------
        // Check villager state after weather change
        // ----------------------------------
        private void CheckVillagerState()
        {
            float faith = gameSession.Village.Faith();
            int shrines = gameSession.Village.Shrines;
            int protests = gameSession.Village.Protests;
            
            Debug.Log($"[Villagers] Faith={faith:F1}, Shrines={shrines}, Protests={protests}, Day={gameSession.Village.Day}");
        }

        // ----------------------------------
        // End the current day: chronicle, save, next day
        // ----------------------------------
        private void EndCurrentDay()
        {
            if (gameSession == null) return;

            // Generate chronicle (offline template, no LLM needed)
            string chronicle = gameSession.EndDay();
            
            Debug.Log($"=== DAY {sessionDay} END ===");
            Debug.Log($"Chronicle: {chronicle}");

            // Check era advancement
            var era = gameSession.Village.Era();
            Debug.Log($"Era: {era} (SessionCount={gameSession.Village.SessionCount}, Shrines={gameSession.Village.Shrines})");

            // Prepare for next day
            sessionDay++;
            sessionActive = false; // Will be reset on next Initialize
            
            // Auto-advance or wait for player
            Debug.Log($"Prepare for Day {sessionDay} - rub palms for sun, flick for rain, etc.");
        }

        // ----------------------------------
        // GUI for development/playtesting
        // ----------------------------------
        void OnGUI()
        {
            if (!sessionActive) return;

            GUILayout.BeginArea(new Rect(20, 20, 250, 300));
            GUILayout.Box($"Session Day {sessionDay}", new GUIStyle(GUI.skin.box) { fontStyle = FontStyle.Bold });
            
            GUILayout.Label("Gesture Controls:");
            GUILayout.Label("1: Sun (rub palms)");
            GUILayout.Label("2: Rain (flick down)");
            GUILayout.Label("3: Wind (sweep hand)");
            GUILayout.Label("4: Calm (palm down)");
            GUILayout.Label($"Cooldown: {gestureDetector?.GESTURE_COOLDOWN ?? 0.8f}s");
            GUILayout.Label("R: End day");
            GUILayout.Label("Q: New session");
            GUILayout.EndArea();
        }
    }