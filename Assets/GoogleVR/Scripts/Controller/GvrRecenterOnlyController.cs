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
using UnityEngine.VR;

/// Used to recenter only the controller, which is required for scenes that have no clear forward direction.
///
/// Offsets the orientation of the transform when a recenter event occurs to correct for the orientation
/// change from the recenter event.
///
/// Usage: Place on the parent of the camera that should have it's orientation corrected.
public class GvrRecenterOnlyController : MonoBehaviour {
  private Quaternion lastAppliedYawCorrection = Quaternion.identity;
  private Quaternion yawCorrection = Quaternion.identity;

  void Update() {
    if (GvrControllerInput.State != GvrConnectionState.Connected) {
      return;
    }

    // Daydream is loaded only on deivce, not in editor.
#if UNITY_ANDROID && !UNITY_EDITOR
        if (VRSettings.loadedDeviceName != "daydream")
        {
            return;
        }
#endif
    if (GvrControllerInput.Recentered) {
      transform.localRotation =
        transform.localRotation * Quaternion.Inverse(lastAppliedYawCorrection) * yawCorrection;
      lastAppliedYawCorrection = yawCorrection;
      return;
    }

#if UNITY_EDITOR
    // Compatibility for Instant Preview.
    if (Gvr.Internal.InstantPreview.Instance != null &&
      Gvr.Internal.InstantPreview.Instance.enabled &&
      (GvrControllerInput.HomeButtonDown || GvrControllerInput.HomeButtonState)) {
      return;
    }
#else  // !UNITY_EDITOR
    if (GvrControllerInput.HomeButtonDown || GvrControllerInput.HomeButtonState) {
      return;
    }
#endif  // UNITY_EDITOR

    yawCorrection = GetYawCorrection();
  }

  void OnDisable() {
    yawCorrection = Quaternion.identity;
    transform.localRotation = transform.localRotation * Quaternion.Inverse(lastAppliedYawCorrection);
    lastAppliedYawCorrection = Quaternion.identity;
  }

  private Quaternion GetYawCorrection() {
    Quaternion headRotation = GetHeadRotation();
    Vector3 euler = headRotation.eulerAngles;
    return lastAppliedYawCorrection * Quaternion.Euler(0.0f, euler.y, 0.0f);
  }

  private Quaternion GetHeadRotation() {
#if UNITY_EDITOR
    return GvrEditorEmulator.HeadRotation;
#else
    return InputTracking.GetLocalRotation(VRNode.Head);
#endif // UNITY_EDITOR
  }
}
