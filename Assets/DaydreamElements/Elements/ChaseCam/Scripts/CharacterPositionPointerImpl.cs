// Copyright 2017 Google Inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using UnityEngine;
using UnityEngine.EventSystems;
using Gvr;

namespace DaydreamElements.Chase {
  /// This class positions a laser pointer and a target prefab
  /// at the end of the pointer.
  public class CharacterPositionPointerImpl : GvrBasePointer {

    /// Color of the laser pointer including alpha transparency
    public Color laserColor = new Color(1.0f, 1.0f, 1.0f, 0.25f);

    /// Width laser pointer line.
    public float lineWidth = .1f;

    /// Laser line.
    public LineRenderer line;

    /// Position of current pointer raycast hit.
    public Vector3 hitPosition;
    public GameObject hitGameObject;

    /// True if we're currently pointed at something.
    public bool IsPointedAtObject {
      get {
        return hitGameObject != null;
      }
    }

    /// Raycast hit result if pointed at object.
    private RaycastResult hitRaycastResult;
    public RaycastResult HitRaycastResult {
      get {
        return hitRaycastResult;
      }
    }

    private Vector3 lineEndPoint;
    public override Vector3 LineEndPoint {
      get {
        return lineEndPoint;
      }
    }

    public float maxPointerDistance = 20.0f;
    public override float MaxPointerDistance {
      get {
         return maxPointerDistance;
      }
    }

    /// Target object to position at hit point.
    public GameObject target;

    /// This is called when the 'BaseInputModule' system should be enabled.
    public override void OnInputModuleEnabled() {
    }

    /// This is called when the 'BaseInputModule' system should be disabled.
    public override void OnInputModuleDisabled() {
    }

    /// Called when the pointer is facing a valid GameObject. This can be a 3D
    /// or UI element.
    public override void OnPointerEnter(
        RaycastResult rayastResult,
        Ray ray,
        bool isInteractive) {
      hitRaycastResult = rayastResult;
      hitGameObject = rayastResult.gameObject;
      hitPosition = rayastResult.worldPosition;
      UpdateLaserPointer();
    }

    /// Called every frame the user is still pointing at a valid GameObject. This
    /// can be a 3D or UI element.
    public override void OnPointerHover(
        RaycastResult rayastResult,
        Ray ray,
        bool isInteractive) {
      hitRaycastResult = rayastResult;
      hitGameObject = rayastResult.gameObject;
      hitPosition = rayastResult.worldPosition;
      UpdateLaserPointer();
    }

    /// Called when the pointer no longer faces an object previously
    /// intersected with a ray projected from the camera.
    /// This is also called just before **OnInputModuleDisabled** and may have have any of
    /// the values set as **null**.
    public override void OnPointerExit(GameObject targetObject) {
      hitGameObject = null;
      hitPosition = Vector3.zero;
      UpdateLaserPointer();
    }

    /// Called when a click is initiated.
    public override void OnPointerClickDown() {
    }

    /// Called when click is finished.
    public override void OnPointerClickUp() {
    }

    public override void GetPointerRadius(out float enterRadius, out float exitRadius) {
      enterRadius = 0;
      exitRadius = 0;
    }

    private Vector3 CalculateLaserEndPoint() {
      if (hitGameObject != null) {
        return hitPosition;
      } else {
        return base.PointerTransform.position
          + (base.PointerTransform.forward * maxPointerDistance);
      }
    }

    private void UpdateLaserPointer() {
      Vector3 lineEndPoint = CalculateLaserEndPoint();
      line.SetPosition(0, base.PointerTransform.position);
      line.SetPosition(1, lineEndPoint);

      float preferredAlpha = GvrArmModel.Instance != null ?
        GvrArmModel.Instance.preferredAlpha : .5f;
      line.startColor = Color.Lerp(Color.clear, laserColor, preferredAlpha);
      line.endColor = Color.clear;
    }

    private void UpdateTargetPosition() {
      if (target == null) {
        return;
      }

      target.SetActive(hitGameObject != null);
      target.transform.position = hitPosition;
      target.transform.rotation = Quaternion.identity;
    }

    public void OnUpdate() {
      UpdateLaserPointer();
      UpdateTargetPosition();
    }
  }
}

