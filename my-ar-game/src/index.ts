/**
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
import { World } from '@iwsdk/core';
import projectOptions from 'virtual:iwsdk-project';
import { PanelSystem } from './panel.js';
import { RobotSystem } from './robot.js';
import { WeatherGestureSystem, createHandGestureEntities } from './gesture-system.js';
import { SimulationIntegrationSystem } from './simulation-integration.js';

World.create(
  document.getElementById('scene-container') as HTMLDivElement,
  projectOptions,
).then((world) => {
  world.registerSystem(RobotSystem);
  world.registerSystem(PanelSystem);
  world.registerSystem(WeatherGestureSystem);
  world.registerSystem(SimulationIntegrationSystem);
  
  // Create hand gesture tracking entities
  createHandGestureEntities(world);
});
