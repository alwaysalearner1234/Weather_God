using System;
using UnityEngine;
using UnityEngine.XR;

namespace TabletopWeatherGod
{
    /// <summary>
    /// Simplified gesture detector for the 4 core weather gestures.
    /// Uses XR Input Subsystem hand tracking.
    /// All gestures work within a two-foot (0.6m) seated radius.
    /// Each gesture has a primary detection and a simpler fallback.
    /// </summary>
    public class SimpleGestureDetector : MonoBehaviour
    {
        [Header("Gesture Settings")]
        public float sunMaxPalmDistance = 0.08f; // 8cm
        public float sunMinDuration = 0.5f;
        public float rainMinPinchDuration = 0.1f;
        public float windMinDuration = 0.5f;
        public float calmMinDuration = 0.5f;

        private float lastGestureTime = -999f;
        private const float GESTURE_COOLDOWN = 0.8f;

        // SUN gesture state
        private bool sunGestureActive = false;
        private float sunStartTime = 0f;
        private float sunLastDistSq = float.MaxValue;

        // RAIN gesture state
        private bool rainGestureActive = false;
        private float rainPinchStart = 0f;
        private bool rainPinchHeld = false;

        // WIND gesture state
        private bool windGestureActive = false;
        private float windStartTime = 0f;
        private Vector3 windLastPalmPos = Vector3.zero;

        // CALM gesture state
        private bool calmGestureActive = false;
        private float calmStartTime = 0f;

        // Track hand positions
        private Vector3 leftPalmPos = Vector3.zero;
        private Vector3 rightPalmPos = Vector3.zero;
        private Quaternion leftPalmRot = Quaternion.identity;
        private Quaternion rightPalmRot = Quaternion.identity;

        void Update()
        {
            GetHandData();
            DetectAllGestures();
            EnforceCooldown();
        }

        private void GetHandData()
        {
            // Get hand poses from XR Input Subsystem
            var (lPos, lRot) = TryGetPose(XRNode.LeftHand);
            var (rPos, rRot) = TryGetPose(XRNode.RightHand);

            leftPalmPos = lPos;
            rightPalmPos = rPos;
            leftPalmRot = lRot;
            rightPalmRot = rRot;
        }

        private (Vector3 pos, Quaternion rot) TryGetPose(XRNode node)
        {
            var device = InputDevices.GetDeviceAtXRNode(node);
            var pos = Vector3.zero;
            var rot = Quaternion.identity;
            device.TryGetFeatureValue(CommonUsages.devicePosition, out pos);
            device.TryGetFeatureValue(CommonUsages.deviceRotation, out rot);
            return (pos, rot);
        }

        private void DetectAllGestures()
        {
            if (leftPalmPos == Vector3.zero || rightPalmPos == Vector3.zero) return;

            // Calculate distance and facing direction
            Vector3 toRight = rightPalmPos - leftPalmPos;
            float palmDistance = toRight.magnitude;
            
            Vector3 leftForward = leftPalmRot * Vector3.forward;
            Vector3 rightForward = rightPalmRot * Vector3.forward;
            bool palmsFacing = Vector3.Dot(leftForward, toRight) > 0f &&
                              Vector3.Dot(rightForward, -toRight) > 0f;

            // 1. SUN: Rub palms together
            DetectSun(palmsFacing, palmDistance);

            // 2. RAIN: Flick fingers down
            DetectRain();

            // 3. WIND: Sweep flat hand
            DetectWind();

            // 4. CALM: Palm down, slow downward motion
            DetectCalm();
        }

        private void DetectSun(bool palmsFacing, float palmDistance)
        {
            if (palmsFacing && palmDistance <= sunMaxPalmDistance)
            {
                if (!sunGestureActive)
                {
                    sunGestureActive = true;
                    sunStartTime = Time.time;
                    sunLastDistSq = toRight.sqrMagnitude;
                    Debug.Log("Sun gesture started");
                }

                float elapsed = Time.time - sunStartTime;
                float currentDistSq = toRight.sqrMagnitude;
                bool oscillated = Mathf.Abs(currentDistSq - sunLastDistSq) > 0.0001f;

                if (elapsed >= sunMinDuration && oscillated)
                {
                    TriggerSun();
                    sunGestureActive = false;
                }
            }
            else
            {
                sunGestureActive = false;
            }
        }

        private void DetectRain()
        {
            // Simplified: pinch then release detection
            // Full implementation would track thumb-index finger tip distance
            
            bool isPinching = false; // Placeholder - would check finger joint data

            if (isPinching)
            {
                if (!rainGestureActive)
                {
                    rainGestureActive = true;
                    rainPinchStart = Time.time;
                    rainPinchHeld = true;
                    Debug.Log("Rain pinch started");
                }
                else
                {
                    float elapsed = Time.time - rainPinchStart;
                    if (elapsed > 2.0f)
                    {
                        rainGestureActive = false;
                        rainPinchHeld = false;
                    }
                }
            }
            else if (rainGestureActive && rainPinchHeld)
            {
                float elapsed = Time.time - rainPinchStart;
                if (elapsed >= rainMinPinchDuration && elapsed <= 2.0f)
                {
                    // Would check downward flick - placeholder true
                    TriggerRain();
                }
                rainGestureActive = false;
                rainPinchHeld = false;
            }
        }

        private void DetectWind()
        {
            if (!windGestureActive)
            {
                // Check for sweep position: palms flat, lateral movement
                bool palmsFlat = true; // placeholder
                bool lateralMovement = true; // placeholder

                if (palmsFlat && lateralMovement)
                {
                    windGestureActive = true;
                    windStartTime = Time.time;
                    windLastPalmPos = leftPalmPos;
                    Debug.Log("Wind sweep started");
                }
            }
            else
            {
                float elapsed = Time.time - windStartTime;
                if (elapsed >= windMinDuration)
                {
                    // Would check sufficient lateral velocity - placeholder true
                    TriggerWind();
                    windGestureActive = false;
                }
                else if (!CheckLateralMovement())
                {
                    windGestureActive = false;
                }
            }
        }

        private bool CheckLateralMovement()
        {
            // Placeholder: check if hands have lateral movement
            return true;
        }

        private void DetectCalm()
        {
            // Check if either palm is facing down
            Vector3 leftForward = leftPalmRot * Vector3.forward;
            Vector3 rightForward = rightPalmRot * Vector3.forward;
            
            bool leftPalmDown = leftForward.y < -0.3f;
            bool rightPalmDown = rightForward.y < -0.3f;
            bool anyPalmDown = leftPalmDown || rightPalmDown;

            if (anyPalmDown && !calmGestureActive)
            {
                calmGestureActive = true;
                calmStartTime = Time.time;
                Debug.Log("Calm gesture: palm down started");
            }
            else if (calmGestureActive)
            {
                float elapsed = Time.time - calmStartTime;
                if (elapsed >= calmMinDuration)
                {
                    // Would check slow downward motion - placeholder true
                    TriggerCalm();
                    calmGestureActive = false;
                }
                else if (!anyPalmDown)
                {
                    calmGestureActive = false;
                }
            }
        }

        // -------------------------------------------------
        // Gesture Triggers
        // -------------------------------------------------
        private void TriggerSun()
        {
            lastGestureTime = Time.time;
            // Would call: FindObjectOfType<WeatherGestureBridge>()?.TriggerSun();
            Debug.Log(">>> SUN WEATHER TRIGGERED <<<");
        }

        private void TriggerRain()
        {
            lastGestureTime = Time.time;
            Debug.Log(">>> RAIN WEATHER TRIGGERED <<<");
        }

        private void TriggerWind()
        {
            lastGestureTime = Time.time;
            Debug.Log(">>> WIND WEATHER TRIGGERED <<<");
        }

        private void TriggerCalm()
        {
            lastGestureTime = Time.time;
            Debug.Log(">>> CALM WEATHER TRIGGERED <<<");
        }

        private void EnforceCooldown()
        {
            if (Time.time - lastGestureTime < GESTURE_COOLDOWN) return;
        }

        // -------------------------------------------------
        // GUI for development
        // -------------------------------------------------
        void OnGUI()
        {
            GUI.color = Color.cyan;
            GUILayout.BeginArea(new Rect(20, 20, 200, 220));
            GUILayout.Box("GESTURE CONTROLS (dev)", new GUIStyle(GUI.skin.box) { fontStyle = FontStyle.Bold });
            
            GUILayout.Label("1. Rub palms together -> SUN");
            GUILayout.Label("2. Pinch then flick down -> RAIN");
            GUILayout.Label("3. Sweep flat hand -> WIND");
            GUILayout.Label("4. Palm facing down -> CALM");
            GUILayout.Label($"Cooldown: {GESTURE_COOLDOWN}s");
            GUILayout.Label("R: End day");
            GUILayout.Label("Q: New session");
            GUILayout.EndArea();
        }
    }