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
using System.Collections;

namespace DaydreamElements.Teleport {
  /// Teleport detector designed for arc-like raycasting from above player.
  public class ArcTeleportDetector : BaseTeleportDetector {
    [Tooltip("Height above the controller to raycast from")]
    public float heightAboveController = 4.0f;

    // Maxium y offset the controller can be from final hit point.
    [Tooltip("Maxium vertical change when teleporting.")]
    public float maxVerticalChange = 10.0f;

    [Tooltip("Minimum controller tilt angle")]
    public float minControllerAngle = 60;

    [Tooltip("Maximum controller tilt angle")]
    public float maxControllerAngle = 90;

#if UNITY_EDITOR
    // Debug helper to show raycasting lines in the editor for discovering height angle issues.
    [Tooltip("Show debug lines for raycasting angles in scene editor")]
    public bool debugRaycasting;
#endif

    // True if we've ever hit a position, across any sessions.
    private bool lastHitPositionValid;

    // Last position we hit, across all teleport sessions (makes for good estimates later)
    private Vector3 lastHitPosition;

    // Players height from the ground, useful for estimating arc with no hits.
    private float playerHeight = 2.0f;
    private bool didCachePlayerHeight;

    // Start teleport selection.
    public override void StartSelection(Transform controller) {
      playerHeight = DetectPlayersHeight(controller.position);
    }

    /// End teleport selection.
    public override void EndSelection() {
    }

    // Detect if there's a valid selection by raycasting from a distance above the controller.
    public override Result DetectSelection(Transform controller) {
      Result result = new Result();
      result.maxDistance = maxDistance;

      Vector3 currentGroundPosition = GroundPosition(controller);
      Vector3 raycastPosition = RaycastPosition(controller, currentGroundPosition);
      float raycastHeightFromGround = (raycastPosition - currentGroundPosition).magnitude;
      Vector3 raycastDirection = RaycastDirection(controller, raycastHeightFromGround);

      // Find the hypotenuse length so we know roughly the max distance to raycast.
      float raycastDistance = Mathf.Sqrt(Mathf.Pow(raycastHeightFromGround, 2)
        + Mathf.Pow(maxDistance, 2));

#if UNITY_EDITOR
      // Draw a debug line showing where the raycast happens from and it's current angle downwards.
      if (debugRaycasting) {
        Debug.DrawRay(raycastPosition, raycastDirection, Color.white);
      }
#endif

      // Intentionally double the raycast, so visualizers can show the invalid selection.
      RaycastHit hit;
      if (Physics.Raycast(raycastPosition,
                          raycastDirection,
                          out hit,
                          raycastDistance * 2,
                          raycastMask) == false) {
        // Return our best guess point for where the arc should be.
        result.selection = BestGuessLandingLocation(lastHitPosition,
            raycastPosition, raycastDirection,
            Vector3.Angle(Vector3.down, raycastDirection),
            controller.transform.position);
        return result;
      }

      lastHitPosition = hit.point;
      lastHitPositionValid = true;

      result.selection = hit.point;
      result.selectionNormal = hit.normal;
      result.selectionObject = hit.collider.gameObject;

      // Limit the amount of vertical change if elevation varies.
      Vector3 selectionDelta = currentGroundPosition - result.selection;
      if (Mathf.Abs(selectionDelta.y) >= maxVerticalChange) {
        return result;
      }

      // Validate that we hit a layer that's valid for teleporting.
      if ((validTeleportLayers.value & (1 << hit.collider.gameObject.layer)) == 0) {
        return result;
      }

      // Validate the angle relative to global up, so users don't teleport into walls etc.
      float angle = Vector3.Angle(Vector3.up, hit.normal);
      if (angle > maxSurfaceAngle) {
        return result;
      }

      result.selectionIsValid = true;

      return result;
    }

    private float DetectPlayersHeight(Vector3 controllerPosition) {
      RaycastHit hit;
      if (Physics.Raycast(controllerPosition, Vector3.down, out hit)) {
        return hit.distance;
      }
      else{
        // Log error, and default to something sensible.
        Debug.LogError("Failed to detect players height by raycasting downwards in arc");
        return 1;
      }
    }

    public Vector3 BestGuessLandingLocation(
        Vector3 lastValidHit,
        Vector3 raycastPosition,
        Vector3 raycastDirection,
        float raycastAngle,
        Vector3 controllerPosition) {
      // We use the player height if there's no previous hit to use.
      Vector3 bestHit = lastHitPosition;
      if (lastHitPositionValid == false) {
        bestHit = controllerPosition - new Vector3(0, playerHeight, 0);
      }

      float rayHeight = Mathf.Abs(bestHit.y - raycastPosition.y);
      float rayLength = rayHeight / Mathf.Cos(raycastAngle * Mathf.Deg2Rad);
      return raycastPosition + (raycastDirection.normalized * rayLength);
    }

    public Vector3 RaycastDirection(Transform controller, float raycastHeightFromGround) {
      float controllerAngle = ControllerAngleFromGround(controller);

      controllerAngle = Mathf.Clamp(controllerAngle, minControllerAngle, maxControllerAngle);
      float arcPercentage =
        (controllerAngle - minControllerAngle) / (maxControllerAngle - minControllerAngle);

      // Clamp the raycast angle so we're only raycasting the max distance configured.
      float minAngle = 0.0f;
      float maxAngle = (Mathf.Atan(maxDistance  / raycastHeightFromGround) * Mathf.Rad2Deg);
      float raycastAngle = -1 * Mathf.Lerp(minAngle, maxAngle, arcPercentage);

      Vector3 raycastDirection = Quaternion.AngleAxis(raycastAngle, controller.transform.right)
        * Vector3.down;

      raycastDirection.Normalize();

      return raycastDirection;
    }

    // Position relative from ground above player we'll raycast from.
    public Vector3 RaycastPosition(Transform controller, Vector3 groundPosition) {
      float yOffset = (controller.position - groundPosition).y + heightAboveController;
      return groundPosition + new Vector3(0, yOffset, 0);
    }

    public Vector3 GroundPosition(Transform controller) {
      RaycastHit hit;
      if (Physics.Raycast(controller.position,
                          Vector3.down,
                          out hit,
                          maxDistance,
                          raycastMask) == false) {
        // We didn't find the ground below us, log error and default to something sensible.
        Debug.LogError("Failed to located the ground, check layermask and max distance");
        return controller.position - new Vector3(0, heightAboveController, 0);
      }

      return hit.point;
    }

    // Vector pointing in same direction as controller along the ground.
    public Vector3 ControllerGroundDirection(Transform controller) {
      Vector3 controllerGroundVect = controller.forward;
      controllerGroundVect.y = 0;
      controllerGroundVect.Normalize();
      return controllerGroundVect;
    }

    // Angle between the controller and the ground.
    public float ControllerAngleFromGround(Transform controller) {
      return Vector3.Angle(Vector3.down, controller.forward);
    }
  }

}
