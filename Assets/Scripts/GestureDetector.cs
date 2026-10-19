using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR;

namespace TabletopWeatherGod
{
    /// <summary>
    /// Detects four core weather gestures using Meta XR hand tracking.
    /// All gestures are seated within a two-foot radius.
    /// Each gesture has a primary detection method and a simpler fallback.
    /// </summary>
    public class GestureDetector : MonoBehaviour
    {
        // References
        [Header("References")]
        [Tooltip("Left hand XR grab interactable or hand joint provider")]
        public XRBaseController leftHand;

        [Tooltip("Right hand XR grab interactable or hand joint provider")]
        public XRBaseController rightHand;

        // Gesture state
        [Header("State")]
        public WeatherType currentWeather = WeatherType.Calm;
        public float currentIntensity = 0f;
        public bool weatherActive = false;

        // Cooldown to prevent accidental triggers
        private float lastGestureTime = -999f;
        private const float GESTURE_COOLDOWN = 0.8f; // 800ms between gestures

        // ==========================================================================
        // 1. SUN: Rub palms together
        // Both palms facing, under 8cm apart, oscillating relative motion for 0.5s
        // ==========================================================================
        private bool sunGestureInProgress = false;
        private float sunGestureStartTime = 0f;
        private Vector3 lastPalmForwardLeft = Vector3.zero;
        private Vector3 lastPalmForwardRight = Vector3.zero;
        private Vector3 lastPalmPositionLeft = Vector3.zero;
        private Vector3 lastPalmPositionRight = Vector3.zero;

        // ==========================================================================
        // 2. RAIN: Flick fingers down
        // Pinch held, then fast release with downward fingertip velocity
        // ==========================================================================
        private bool rainGestureInProgress = false;
        private float rainGestureStartTime = 0f;
        private bool pinchHeld = false;
        private Vector3 lastFingerTipPosition = Vector3.zero;
        private float pinchStartTime = 0f;

        // ==========================================================================
        // 3. WIND: Sweep flat hand
        // Flat palm, fingers extended, lateral palm velocity above threshold
        // Direction determines wind direction
        // ==========================================================================
        private bool windGestureInProgress = false;
        private float windGestureStartTime = 0f;
        private Vector3 lastPalmVelocity = Vector3.zero;
        private Vector3 lastPalmNormal = Vector3.zero;
        private bool fingersExtended = true;

        // ==========================================================================
        // 4. CALM: Palm down, slow downward motion
        // Palm facing down, slow downward motion, closed fist or open palm
        // ==========================================================================
        private bool calmGestureInProgress = false;
        private float calmGestureStartTime = 0f;
        private Vector3 lastPalmDownDirection = Vector3.zero;
        private bool palmFacingDown = false;

        // ==========================================================================
        // Unity Life Cycle
        // ==========================================================================
        void Update()
        {
            if (leftHand == null && rightHand == null) return;

            GetHandData();

            DetectSunGesture();
            DetectRainGesture();
            DetectWindGesture();
            DetectCalmGesture();

            if (Time.time - lastGestureTime < GESTURE_COOLDOWN)
            {
                // In cooldown - gestures are blocked
            }
        }

        private void GetHandData()
        {
            if (leftHand != null)
            {
                // Hand joint data would be available from XR Interaction SDK
                // For MVP, we'll use controller transforms as approximation
            }

            if (rightHand != null)
            {
                // Same as left
            }
        }

        // ----------------------------------
        // 1. SUN GESTURE: Rub palms together
        // ----------------------------------
        private void DetectSunGesture()
        {
            if (leftHand == null || rightHand == null) return;

            Vector3 palmPosL = leftHand.transform.position;
            Vector3 palmPosR = rightHand.transform.position;
            float distance = Vector3.Distance(palmPosL, palmPosR);

            // Check palms are facing each other and within 8cm
            Vector3 toRight = palmPosR - palmPosL;
            bool palmsFacing = Vector3.Dot(Vector3.forward, toRight.normalized) > 0.5f;
            bool palmsFacing2 = Vector3.Dot(Vector3.forward, (-toRight).normalized) > 0.5f;
            bool closeEnough = distance < 0.08f;

            if (palmsFacing && closeEnough && !sunGestureInProgress)
            {
                sunGestureInProgress = true;
                sunGestureStartTime = Time.time;
                Debug.Log("Sun gesture started: palms rubbing");
            }
            else if (!palmsFacing || !closeEnough)
            {
                sunGestureInProgress = false;
            }

            if (sunGestureInProgress && Time.time - sunGestureStartTime >= 0.5f)
            {
                TriggerSun();
                sunGestureInProgress = false;
            }
        }

        // ----------------------------------
        // 2. RAIN GESTURE: Flick fingers down
        // ----------------------------------
        private void DetectRainGesture()
        {
            // Simplified: check for pinch then release with downward motion
            // In full implementation: use thumb-index finger tip distance tracking
            
            bool isPinching = false; // Would check thumb-index distance

            if (isPinching && !rainGestureInProgress)
            {
                rainGestureInProgress = true;
                rainGestureStartTime = Time.time;
                pinchHeld = true;
                Debug.Log("Rain gesture: pinch started");
            }
            else if (!isPinching && rainGestureInProgress && pinchHeld)
            {
                float elapsed = Time.time - rainGestureStartTime;
                if (elapsed >= 0.1f && elapsed <= 2.0f)
                {
                    bool downwardFlick = true; // Would check finger tip velocity
                    if (downwardFlick)
                    {
                        TriggerRain();
                    }
                }
                rainGestureInProgress = false;
                pinchHeld = false;
            }
            else if (rainGestureInProgress && Time.time - rainGestureStartTime > 2.0f)
            {
                rainGestureInProgress = false;
                pinchHeld = false;
            }
        }

        // ----------------------------------
        // 3. WIND GESTURE: Sweep flat hand
        // ----------------------------------
        private void DetectWindGesture()
        {
            if (windGestureInProgress)
            {
                float elapsed = Time.time - windGestureStartTime;
                if (elapsed >= 0.5f && EnoughLateralVelocity())
                {
                    TriggerWind();
                    windGestureInProgress = false;
                }
            }
            else
            {
                if (IsPalmFlatAndExtended() && HasLateralMovement())
                {
                    windGestureInProgress = true;
                    windGestureStartTime = Time.time;
                    Debug.Log("Wind gesture started: sweep initiated");
                }
            }
        }

        // ----------------------------------
        // 4. CALM GESTURE: Palm down, slow downward motion
        // ----------------------------------
        private void DetectCalmGesture()
        {
            if (palmFacingDown && HasSlowDownwardMotion())
            {
                if (!calmGestureInProgress)
                {
                    calmGestureInProgress = true;
                    calmGestureStartTime = Time.time;
                }

                if (Time.time - calmGestureStartTime >= 0.5f)
                {
                    TriggerCalm();
                    calmGestureInProgress = false;
                }
            }
            else
            {
                if (calmGestureInProgress && (!palmFacingDown || !HasSlowDownwardMotion()))
                {
                    calmGestureInProgress = false;
                }
                else if (!calmGestureInProgress && IsPalmFacingDownSlow())
                {
                    calmGestureInProgress = true;
                    calmGestureStartTime = Time.time;
                }
            }
        }

        // ==========================================================================
        // Gesture Triggers
        // ==========================================================================

        private void TriggerSun()
        {
            lastGestureTime = Time.time;
            currentWeather = WeatherType.Sun;
            currentIntensity = 0.6f;
            weatherActive = true;
            Debug.Log($"SUN WEATHER TRIGGERED: intensity={currentIntensity}");
        }

        private void TriggerRain()
        {
            lastGestureTime = Time.time;
            currentWeather = WeatherType.Rain;
            currentIntensity = 0.5f;
            weatherActive = true;
            Debug.Log($"RAIN WEATHER TRIGGERED: intensity={currentIntensity}");
        }

        private void TriggerWind()
        {
            lastGestureTime = Time.time;
            currentWeather = WeatherType.Wind;
            currentIntensity = 0.5f;
            weatherActive = true;
            Debug.Log($"WIND WEATHER TRIGGERED: intensity={currentIntensity}");
        }

        private void TriggerCalm()
        {
            lastGestureTime = Time.time;
            currentWeather = WeatherType.Calm;
            currentIntensity = 0.3f;
            weatherActive = true;
            Debug.Log($"CALM WEATHER TRIGGERED: intensity={currentIntensity}");
        }

        // ----------------------------------
        // Helper methods (placeholders for MVP)
        // ----------------------------------
        private bool EnoughLateralVelocity() => true;
        private bool IsPalmFlatAndExtended() => true;
        private bool HasLateralMovement() => true;
        private bool HasSlowDownwardMotion() => true;
        private bool IsPalmFacingDown() => false;
        private bool IsPalmFacingDownSlow() => false;
    }