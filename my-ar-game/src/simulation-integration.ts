/**
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

import { createSystem, Entity } from '@iwsdk/core';
import { GestureState, WeatherGesture } from './gesture-component.js';
import { WeatherGestureSystem } from './gesture-system.js';

// Import sim-core functions (these would be available when bundled)
interface SimCoreModule {
  createSession: (seed: number) => any;
  playerAction: (session: any, action: string, region: string, intensity: number) => any;
  tick: (session: any, n: number) => void;
  endDay: (session: any) => any;
  villageFaithOf: (state: any) => number;
}

let simCore: SimCoreModule | null = null;

// Dynamic import for sim-core (works when bundled)
async function loadSimCore(): Promise<SimCoreModule | null> {
  if (simCore) return simCore;
  try {
    // In production, this would be a direct import
    // For now, we'll use a mock implementation
    console.warn('[SimCoreIntegration] Using mock sim-core implementation');
    return null;
  } catch (e) {
    console.error('[SimCoreIntegration] Failed to load sim-core:', e);
    return null;
  }
}

interface SimulationState {
  session: any;
  seed: number;
  day: number;
  lastActionTime: number;
}

export class SimulationIntegrationSystem extends createSystem({
  gestureState: { required: [GestureState] },
}) {
  private simState: SimulationState | null = null;
  private gestureSystem: WeatherGestureSystem | undefined;
  private initialized = false;

  async init(): Promise<void> {
    // Get reference to gesture system
    this.gestureSystem = this.world.getSystem(WeatherGestureSystem);
    
    // Initialize simulation
    await this.initializeSimulation();
    
    // Subscribe to gesture events
    if (this.gestureSystem) {
      this.gestureSystem.onGesture((gesture, intensity) => {
        this.onWeatherGesture(gesture, intensity);
      });
    }
  }

  private async initializeSimulation(): Promise<void> {
    // Load sim-core
    await loadSimCore();
    
    // For now, create a mock session
    // In production, this would use the real sim-core
    this.simState = {
      session: null, // Would be created by simCore.createSession(seed)
      seed: Date.now(),
      day: 1,
      lastActionTime: 0,
    };
    
    this.initialized = true;
    console.log('[SimulationIntegration] Initialized (mock mode)');
  }

  private onWeatherGesture(gesture: WeatherGesture, intensity: number): void {
    if (!this.initialized || !this.simState) return;
    
    // Map gesture to sim-core action
    const action = this.mapGestureToAction(gesture);
    if (!action) return;
    
    const now = performance.now() / 1000;
    if (now - this.simState.lastActionTime < 1.0) {
      // Rate limit actions
      return;
    }
    
    this.simState.lastActionTime = now;
    
    console.log(`[SimulationIntegration] Applying weather action: ${action} at intensity ${intensity}`);
    
    // In production, this would call:
    // simCore.playerAction(this.simState.session, action, 'field', intensity);
    // simCore.tick(this.simState.session, 6); // Advance 6 ticks (1/4 day)
    
    // For now, just log
    this.logSimulationState();
  }

  private mapGestureToAction(gesture: WeatherGesture): string | null {
    switch (gesture) {
      case WeatherGesture.Sun: return 'sun';
      case WeatherGesture.Rain: return 'rain';
      case WeatherGesture.Wind: return 'wind';
      case WeatherGesture.Calm: return 'calm';
      default: return null;
    }
  }

  private logSimulationState(): void {
    if (!this.simState) return;
    console.log(`[SimulationIntegration] Day ${this.simState.day}, Seed: ${this.simState.seed}`);
  }

  update(): void {
    // Periodic simulation updates could go here
    // e.g., advance time, check for day end, etc.
  }
}