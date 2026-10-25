/**
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

import { Types, createComponent } from '@iwsdk/core';

export enum WeatherGesture {
  None = 'none',
  Sun = 'sun',
  Rain = 'rain',
  Wind = 'wind',
  Calm = 'calm',
}

const WeatherGestureEnum = {
  none: 'none',
  sun: 'sun',
  rain: 'rain',
  wind: 'wind',
  calm: 'calm',
};

const HandednessEnum = {
  left: 'left',
  right: 'right',
};

export const GestureState = createComponent('GestureState', {
  /** Current detected gesture */
  currentGesture: { type: Types.Enum, enum: WeatherGestureEnum, default: WeatherGesture.None },
  /** Intensity of the gesture (0-1) */
  intensity: { type: Types.Float32, default: 0.5 },
  /** Whether a gesture is currently being detected */
  isDetecting: { type: Types.Boolean, default: false },
  /** Cooldown timer to prevent rapid re-triggering */
  cooldownTimer: { type: Types.Float32, default: 0 },
  /** Last detected gesture (for UI feedback) */
  lastGesture: { type: Types.Enum, enum: WeatherGestureEnum, default: WeatherGesture.None },
  /** Time since last gesture trigger */
  timeSinceLastGesture: { type: Types.Float32, default: 999 },
});

/** Per-hand gesture tracking state */
export const HandGestureData = createComponent('HandGestureData', {
  /** Handedness */
  handedness: { type: Types.Enum, enum: HandednessEnum, default: 'right' },
  /** Pinch strength 0-1 */
  pinchStrength: { type: Types.Float32, default: 0 },
  /** Previous pinch strength for edge detection */
  prevPinchStrength: { type: Types.Float32, default: 0 },
  /** Palm position in world space */
  palmPosition: { type: Types.Vec3, default: [0, 0, 0] },
  /** Palm forward direction */
  palmForward: { type: Types.Vec3, default: [0, 0, -1] },
  /** Palm up direction */
  palmUp: { type: Types.Vec3, default: [0, 1, 0] },
  /** Index finger tip position */
  indexTipPosition: { type: Types.Vec3, default: [0, 0, 0] },
  /** Thumb tip position */
  thumbTipPosition: { type: Types.Vec3, default: [0, 0, 0] },
  /** Wrist position */
  wristPosition: { type: Types.Vec3, default: [0, 0, 0] },
  /** Previous palm position for velocity calculation */
  prevPalmPosition: { type: Types.Vec3, default: [0, 0, 0] },
  /** Palm velocity */
  palmVelocity: { type: Types.Vec3, default: [0, 0, 0] },
  /** Whether palm is facing down (for calm gesture) */
  palmFacingDown: { type: Types.Boolean, default: false },
  /** Whether fingers are extended (for wind gesture) */
  fingersExtended: { type: Types.Boolean, default: false },
});