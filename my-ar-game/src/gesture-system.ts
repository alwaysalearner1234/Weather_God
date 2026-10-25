/**
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

import {
  createSystem,
  Vector3,
  Quaternion,
  Matrix4,
  XRHandVisualAdapter,
  XRInputManager,
  Entity,
} from '@iwsdk/core';
import { GestureState, HandGestureData, WeatherGesture } from './gesture-component.js';

// Gesture detection thresholds
const GESTURE_COOLDOWN = 0.8; // seconds between gestures
const SUN_MAX_PALM_DISTANCE = 0.08; // 8cm
const SUN_MIN_DURATION = 0.5; // seconds
const RAIN_MIN_PINCH_DURATION = 0.1;
const RAIN_MAX_PINCH_DURATION = 2.0;
const WIND_MIN_DURATION = 0.5;
const CALM_MIN_DURATION = 0.5;
const CALM_PALM_DOWN_THRESHOLD = -0.3; // palm forward y < -0.3
const WIND_LATERAL_VELOCITY_THRESHOLD = 0.3; // m/s
const SUN_OSCILLATION_THRESHOLD = 0.0001; // distance squared change

// SUN gesture state per session
interface SunGestureState {
  active: boolean;
  startTime: number;
  lastDistanceSq: number;
}

// RAIN gesture state per session
interface RainGestureState {
  active: boolean;
  pinchStartTime: number;
  pinchHeld: boolean;
}

// WIND gesture state per session
interface WindGestureState {
  active: boolean;
  startTime: number;
  lastPalmPos: Vector3;
}

// CALM gesture state per session
interface CalmGestureState {
  active: boolean;
  startTime: number;
}

function getFirstEntity(entities: Set<Entity>): Entity | undefined {
  for (const entity of entities) {
    return entity;
  }
  return undefined;
}

function getVec3(entity: Entity, component: any, key: string): Vector3 {
  const val = entity.getValue(component, key);
  if (!val) return new Vector3();
  return new Vector3().fromArray(val as [number, number, number]);
}

function getNumber(entity: Entity, component: any, key: string): number {
  const val = entity.getValue(component, key) as number | null;
  return val ?? 0;
}

function getBoolean(entity: Entity, component: any, key: string): boolean {
  const val = entity.getValue(component, key) as boolean | null;
  return val ?? false;
}

export class WeatherGestureSystem extends createSystem({
  gestureState: { required: [GestureState] },
  leftHand: { required: [HandGestureData] },
  rightHand: { required: [HandGestureData] },
}) {
  private sunState: SunGestureState = { active: false, startTime: 0, lastDistanceSq: 0 };
  private rainState: RainGestureState = { active: false, pinchStartTime: 0, pinchHeld: false };
  private windState: WindGestureState = { active: false, startTime: 0, lastPalmPos: new Vector3() };
  private calmState: CalmGestureState = { active: false, startTime: 0 };
  
  private leftHandAdapter: XRHandVisualAdapter | undefined;
  private rightHandAdapter: XRHandVisualAdapter | undefined;
  private xrInputManager: XRInputManager | undefined;
  
  // Callback for gesture events
  private onGestureCallback: ((gesture: WeatherGesture, intensity: number) => void) | undefined;

  init(): void {
    // Get access to XR input manager for hand tracking data
    this.xrInputManager = this.world.input.xr;
    
    // Subscribe to hand visual adapters to get joint data
    this.world.input.xr.visualAdapters.left.subscribe((adapter) => {
      if (adapter instanceof XRHandVisualAdapter) {
        this.leftHandAdapter = adapter;
      }
    });
    
    this.world.input.xr.visualAdapters.right.subscribe((adapter) => {
      if (adapter instanceof XRHandVisualAdapter) {
        this.rightHandAdapter = adapter;
      }
    });

    // Initialize hand gesture data components
    const leftEntity = getFirstEntity(this.queries.leftHand.entities);
    const rightEntity = getFirstEntity(this.queries.rightHand.entities);
    
    if (leftEntity) {
      leftEntity.setValue(HandGestureData, 'handedness', 'left');
    }
    if (rightEntity) {
      rightEntity.setValue(HandGestureData, 'handedness', 'right');
    }
  }

  /** Register a callback for gesture events */
  onGesture(callback: (gesture: WeatherGesture, intensity: number) => void): () => void {
    this.onGestureCallback = callback;
    return () => { this.onGestureCallback = undefined; };
  }

  update(delta: number, time: number): void {
    // Update cooldown timer
    const gestureState = getFirstEntity(this.queries.gestureState.entities);
    if (gestureState) {
      const cooldownTimer = gestureState.getValue(GestureState, 'cooldownTimer') ?? 0;
      if (cooldownTimer > 0) {
        gestureState.setValue(GestureState, 'cooldownTimer', Math.max(0, cooldownTimer - delta));
      }
      
      const timeSinceLast = gestureState.getValue(GestureState, 'timeSinceLastGesture') ?? 999;
      gestureState.setValue(GestureState, 'timeSinceLastGesture', timeSinceLast + delta);
    }

    // Update hand tracking data
    this.updateHandData('left', this.leftHandAdapter, delta);
    this.updateHandData('right', this.rightHandAdapter, delta);

    // Detect gestures if not in cooldown
    const cooldownTimer = gestureState?.getValue(GestureState, 'cooldownTimer') ?? 0;
    if (cooldownTimer <= 0) {
      this.detectSunGesture();
      this.detectRainGesture();
      this.detectWindGesture();
      this.detectCalmGesture();
    }
  }

  private updateHandData(
    handedness: 'left' | 'right',
    adapter: XRHandVisualAdapter | undefined,
    delta: number
  ): void {
    if (!adapter) return;

    const query = handedness === 'left' ? this.queries.leftHand : this.queries.rightHand;
    const entity = getFirstEntity(query.entities);
    if (!entity) return;

    // Get pinch strength
    const pinchStrength = adapter.getPinchStrength();
    const prevPinchStrength = entity.getValue(HandGestureData, 'pinchStrength') ?? 0;
    entity.setValue(HandGestureData, 'prevPinchStrength', prevPinchStrength);
    entity.setValue(HandGestureData, 'pinchStrength', pinchStrength);

    // Get palm pose from grip space
    if (adapter.gripSpace) {
      const palmPos = new Vector3();
      const palmQuat = new Quaternion();
      adapter.gripSpace.getWorldPosition(palmPos);
      adapter.gripSpace.getWorldQuaternion(palmQuat);
      
      const prevPalmPos = getVec3(entity, HandGestureData, 'palmPosition');
      entity.setValue(HandGestureData, 'prevPalmPosition', [prevPalmPos.x, prevPalmPos.y, prevPalmPos.z]);
      entity.setValue(HandGestureData, 'palmPosition', [palmPos.x, palmPos.y, palmPos.z]);

      // Calculate palm velocity
      const velocity = new Vector3(
        palmPos.x - prevPalmPos.x,
        palmPos.y - prevPalmPos.y,
        palmPos.z - prevPalmPos.z
      ).multiplyScalar(1 / delta);
      entity.setValue(HandGestureData, 'palmVelocity', [velocity.x, velocity.y, velocity.z]);

      // Palm forward direction (-Z in local space = forward)
      const forward = new Vector3(0, 0, -1).applyQuaternion(palmQuat);
      entity.setValue(HandGestureData, 'palmForward', [forward.x, forward.y, forward.z]);

      // Palm up direction (Y in local space = up)
      const up = new Vector3(0, 1, 0).applyQuaternion(palmQuat);
      entity.setValue(HandGestureData, 'palmUp', [up.x, up.y, up.z]);

      // Check if palm is facing down (for calm gesture)
      const palmFacingDown = forward.y < CALM_PALM_DOWN_THRESHOLD;
      entity.setValue(HandGestureData, 'palmFacingDown', palmFacingDown);

      // Check if fingers are extended (for wind gesture)
      // Approximate: if pinch strength is low, fingers are likely extended
      const fingersExtended = pinchStrength < 0.3;
      entity.setValue(HandGestureData, 'fingersExtended', fingersExtended);
    }

    // Get index finger tip position using public method
    const indexTipSpace = adapter.getIndexTipSpace();
    if (indexTipSpace && this.world.xrFrame && this.world.xrReferenceSpace) {
      const pose = this.world.xrFrame.getPose(indexTipSpace, this.world.xrReferenceSpace);
      if (pose) {
        const indexTipPos = new Vector3();
        const matrix = new Matrix4().fromArray(pose.transform.matrix as unknown as number[]);
        indexTipPos.setFromMatrixPosition(matrix);
        entity.setValue(HandGestureData, 'indexTipPosition', [indexTipPos.x, indexTipPos.y, indexTipPos.z]);
      }
    }
  }

  private detectSunGesture(): void {
    const leftHand = getFirstEntity(this.queries.leftHand.entities);
    const rightHand = getFirstEntity(this.queries.rightHand.entities);
    if (!leftHand || !rightHand) return;

    const leftPos = getVec3(leftHand, HandGestureData, 'palmPosition');
    const rightPos = getVec3(rightHand, HandGestureData, 'palmPosition');
    const distance = leftPos.distanceTo(rightPos);
    const distanceSq = distance * distance;

    // Check palms are facing each other
    const leftForward = getVec3(leftHand, HandGestureData, 'palmForward');
    const rightForward = getVec3(rightHand, HandGestureData, 'palmForward');
    const toRight = new Vector3().subVectors(rightPos, leftPos).normalize();
    
    const leftFacing = leftForward.dot(toRight) > 0.5;
    const rightFacing = rightForward.dot(toRight.negate()) > 0.5;
    const palmsFacing = leftFacing && rightFacing;
    const closeEnough = distance <= SUN_MAX_PALM_DISTANCE;

    if (palmsFacing && closeEnough && !this.sunState.active) {
      // Start sun gesture
      this.sunState = { active: true, startTime: performance.now() / 1000, lastDistanceSq: distanceSq };
      this.setDetecting(true, WeatherGesture.Sun);
    } else if (!palmsFacing || !closeEnough) {
      // Cancel sun gesture
      this.sunState.active = false;
      this.setDetecting(false, WeatherGesture.Sun);
    }

    if (this.sunState.active) {
      const elapsed = (performance.now() / 1000) - this.sunState.startTime;
      const oscillated = Math.abs(distanceSq - this.sunState.lastDistanceSq) > SUN_OSCILLATION_THRESHOLD;
      this.sunState.lastDistanceSq = distanceSq;

      if (elapsed >= SUN_MIN_DURATION && oscillated) {
        this.triggerGesture(WeatherGesture.Sun, 0.6);
        this.sunState.active = false;
      }
    }
  }

  private detectRainGesture(): void {
    // Rain: Pinch held, then fast release with downward fingertip velocity
    const leftHand = getFirstEntity(this.queries.leftHand.entities);
    const rightHand = getFirstEntity(this.queries.rightHand.entities);
    if (!leftHand || !rightHand) return;

    const leftPinch = getNumber(leftHand, HandGestureData, 'pinchStrength');
    const rightPinch = getNumber(rightHand, HandGestureData, 'pinchStrength');
    const leftPrevPinch = getNumber(leftHand, HandGestureData, 'prevPinchStrength');
    const rightPrevPinch = getNumber(rightHand, HandGestureData, 'prevPinchStrength');
    
    const pinching = leftPinch > 0.8 || rightPinch > 0.8;
    const wasPinching = leftPrevPinch > 0.8 || rightPrevPinch > 0.8;
    const justReleased = wasPinching && !pinching;

    if (pinching && !this.rainState.active) {
      this.rainState = { active: true, pinchStartTime: performance.now() / 1000, pinchHeld: true };
    } else if (justReleased && this.rainState.active && this.rainState.pinchHeld) {
      const elapsed = (performance.now() / 1000) - this.rainState.pinchStartTime;
      if (elapsed >= RAIN_MIN_PINCH_DURATION && elapsed <= RAIN_MAX_PINCH_DURATION) {
        // Check for downward flick - use palm velocity as approximation
        const leftPalmVel = getVec3(leftHand, HandGestureData, 'palmVelocity');
        const rightPalmVel = getVec3(rightHand, HandGestureData, 'palmVelocity');
        const downwardFlick = leftPalmVel.y < -0.5 || rightPalmVel.y < -0.5;
        
        if (downwardFlick) {
          this.triggerGesture(WeatherGesture.Rain, 0.5);
        }
      }
      this.rainState = { active: false, pinchStartTime: 0, pinchHeld: false };
    } else if (this.rainState.active && (performance.now() / 1000) - this.rainState.pinchStartTime > RAIN_MAX_PINCH_DURATION) {
      this.rainState = { active: false, pinchStartTime: 0, pinchHeld: false };
    }
  }

  private detectWindGesture(): void {
    // Wind: Sweep flat hand laterally
    const leftHand = getFirstEntity(this.queries.leftHand.entities);
    const rightHand = getFirstEntity(this.queries.rightHand.entities);
    if (!leftHand || !rightHand) return;

    const leftFingersExtended = getBoolean(leftHand, HandGestureData, 'fingersExtended');
    const rightFingersExtended = getBoolean(rightHand, HandGestureData, 'fingersExtended');
    const leftPalmVel = getVec3(leftHand, HandGestureData, 'palmVelocity');
    const rightPalmVel = getVec3(rightHand, HandGestureData, 'palmVelocity');
    
    // Check lateral velocity (X or Z axis)
    const leftLateralSpeed = Math.sqrt(leftPalmVel.x * leftPalmVel.x + leftPalmVel.z * leftPalmVel.z);
    const rightLateralSpeed = Math.sqrt(rightPalmVel.x * rightPalmVel.x + rightPalmVel.z * rightPalmVel.z);
    const hasLateralMovement = leftLateralSpeed > WIND_LATERAL_VELOCITY_THRESHOLD || rightLateralSpeed > WIND_LATERAL_VELOCITY_THRESHOLD;

    const palmsFlat = leftFingersExtended || rightFingersExtended;

    if (!this.windState.active) {
      if (palmsFlat && hasLateralMovement) {
        this.windState = { active: true, startTime: performance.now() / 1000, lastPalmPos: new Vector3() };
      }
    } else {
      const elapsed = (performance.now() / 1000) - this.windState.startTime;
      if (elapsed >= WIND_MIN_DURATION) {
        this.triggerGesture(WeatherGesture.Wind, 0.5);
        this.windState.active = false;
      } else if (!hasLateralMovement) {
        this.windState.active = false;
      }
    }
  }

  private detectCalmGesture(): void {
    // Calm: Palm facing down, slow downward motion
    const leftHand = getFirstEntity(this.queries.leftHand.entities);
    const rightHand = getFirstEntity(this.queries.rightHand.entities);
    if (!leftHand || !rightHand) return;

    const leftPalmDown = getBoolean(leftHand, HandGestureData, 'palmFacingDown');
    const rightPalmDown = getBoolean(rightHand, HandGestureData, 'palmFacingDown');
    const anyPalmDown = leftPalmDown || rightPalmDown;

    const leftPalmVel = getVec3(leftHand, HandGestureData, 'palmVelocity');
    const rightPalmVel = getVec3(rightHand, HandGestureData, 'palmVelocity');
    const slowDownward = (leftPalmVel.y < 0 && leftPalmVel.y > -0.3) || (rightPalmVel.y < 0 && rightPalmVel.y > -0.3);

    if (anyPalmDown && slowDownward && !this.calmState.active) {
      this.calmState = { active: true, startTime: performance.now() / 1000 };
      this.setDetecting(true, WeatherGesture.Calm);
    } else if (this.calmState.active) {
      const elapsed = (performance.now() / 1000) - this.calmState.startTime;
      if (elapsed >= CALM_MIN_DURATION) {
        this.triggerGesture(WeatherGesture.Calm, 0.3);
        this.calmState.active = false;
      } else if (!anyPalmDown || !slowDownward) {
        this.calmState.active = false;
        this.setDetecting(false, WeatherGesture.Calm);
      }
    }
  }

  private setDetecting(detecting: boolean, gesture: WeatherGesture): void {
    const gestureState = getFirstEntity(this.queries.gestureState.entities);
    if (gestureState) {
      gestureState.setValue(GestureState, 'isDetecting', detecting);
      if (detecting) {
        gestureState.setValue(GestureState, 'currentGesture', gesture);
      }
    }
  }

  private triggerGesture(gesture: WeatherGesture, intensity: number): void {
    const gestureState = getFirstEntity(this.queries.gestureState.entities);
    if (!gestureState) return;

    gestureState.setValue(GestureState, 'currentGesture', gesture);
    gestureState.setValue(GestureState, 'intensity', intensity);
    gestureState.setValue(GestureState, 'lastGesture', gesture);
    gestureState.setValue(GestureState, 'timeSinceLastGesture', 0);
    gestureState.setValue(GestureState, 'cooldownTimer', GESTURE_COOLDOWN);
    gestureState.setValue(GestureState, 'isDetecting', false);

    console.log(`[WeatherGesture] ${gesture.toUpperCase()} triggered at intensity ${intensity}`);
    
    // Call registered callback
    if (this.onGestureCallback) {
      this.onGestureCallback(gesture, intensity);
    }
  }
}

// Component to create hand gesture data entities
export function createHandGestureEntities(world: any): void {
  // Create left hand entity
  const leftEntity = world.createEntity();
  leftEntity.addComponent(HandGestureData);
  leftEntity.setValue(HandGestureData, 'handedness', 'left');

  // Create right hand entity
  const rightEntity = world.createEntity();
  rightEntity.addComponent(HandGestureData);
  rightEntity.setValue(HandGestureData, 'handedness', 'right');

  // Create gesture state entity
  const gestureEntity = world.createEntity();
  gestureEntity.addComponent(GestureState);
}