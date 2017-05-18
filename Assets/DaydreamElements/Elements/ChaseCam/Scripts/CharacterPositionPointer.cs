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

namespace DaydreamElements.Chase {

  /// This class shows a laser pointer with an optional target
  /// prefab positioned at the end of the laser where it hit something. When
  /// The trigger is activated, and we're pointed at a valid position, this
  /// class will ask the positioned character to move to that location. You
  /// can customize what is considered a valid location by subclassing and
  /// overriding IsPointedAtValidMovePosition(), or by ignoring the position
  /// inside your positioned character subclass.
  [RequireComponent(typeof(LineRenderer))]
  public class CharacterPositionPointer : MonoBehaviour {
    /// Color of the line pointer.
    [Tooltip("Color of the laser pointer")]
    public Color laserColor = new Color(1.0f, 1.0f, 1.0f, 0.25f);

    /// Width of the line used for the laser pointer.
    public float lineWidth = .1f;

    /// Trigger used to confirm a selection and move to point.
    [Tooltip("Trigger for selecting new position to move to")]
    public BaseActionTrigger moveTrigger;

    /// Character we'll integrate to pass new move requests.
    [Tooltip("Positioned character this pointer is controlling")]
    public BasePositionedCharacter character;

    /// Target prefab is positioned at end of the laser pointer.
    [Tooltip("Prefab to place at the end of the laser pointer")]
    public GameObject targetPrefab;
    private GameObject target;

    [Tooltip("Maximum surface angle for valid selections")]
    [Range(0, 180)]
    public float maxHitAngleDegrees = 30.0f;

    /// Implementation of the pointer logic since it's not a monobehaviour.
    private CharacterPositionPointerImpl pointer;
    public CharacterPositionPointerImpl Pointer {
      get {
        return pointer;
      }
    }

    // Use this for initialization
    void Start () {
      if (targetPrefab == null) {
        Debug.LogError("Character position pointer must have target prefab!");
        return;
      }

      pointer = new CharacterPositionPointerImpl();
      pointer.line = GetComponent<LineRenderer>();
      pointer.PointerTransform = transform;
      pointer.OnStart();

      target = Instantiate(targetPrefab, transform);
      pointer.target = target;
    }

    void Update() {
      pointer.laserColor = laserColor;
      pointer.lineWidth = lineWidth;
      pointer.OnUpdate();

      // We hide the pointer target if there's no valid selection.
      bool isValid = IsPointedAtValidMovePosition() && IsPointedAtValidAngle();
      pointer.target.SetActive(isValid);

      if (!isValid) {
        return;
      }

      if (character == null) {
        Debug.LogError("Can't move null positioned character");
        return;
      }

      if (moveTrigger == null) {
        Debug.LogError("Can't position character without a move trigger to check");
        return;
      }

      if (moveTrigger.TriggerActive()) {
        // Ask the character to move to pointed at position.
        character.SetTargetPosition(pointer.hitPosition);
        return;
      }
    }

    /// Override this method for custom logic for character movement
    /// if you want to do filtering based on tags, layers, etc.
    protected virtual bool IsPointedAtValidMovePosition() {
      if (pointer == null) {
        return false;
      }
      return pointer.IsPointedAtObject;
    }

    /// Ignore steep surfaces, like walls.
    protected virtual bool IsPointedAtValidAngle() {
      if (pointer == null || pointer.IsPointedAtObject == false) {
        return false;
      }

      float angle = Vector3.Angle(Vector3.up, pointer.HitRaycastResult.worldNormal);
      return angle <= maxHitAngleDegrees;
    }
  }
}
