// Copyright 2016 Google Inc. All rights reserved.
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

namespace DaydreamElements.Tunneling {

  /// Provides functionality to render a "vignette" in front of the camera.
  /// The vignette is will reduce the player's field of view to reduce the experience of motion sickness.
  /// This should be used when the camera is moving.
  [ExecuteInEditMode]
  public class TunnelingVignette : MonoBehaviour {
    public const float MAX_FOV = 180.0f;

    private const float DEFAULT_FADE_FOV = 6.5f;
    private const float HALF_PI = 1.57079632679f;
    private const float CULL_IRIS_FOV = 175.0f;
    private const float FOV_SMOOTH_THRESHOLD = 0.01f;
    private const float DEPTH_CULLING_SAFETY_FACTOR = 3.0f;
    private const string SHADER_PROP_VIGNETTE_MIN_MAX = "_VignetteMinMax";
    private const string SHADER_PROP_VIGNETTE_CAGE_COLOR = "_VignetteCageColor";
    private const string SHADER_PROP_VIGNETTE_CAMERA_FORWARD = "_MainCameraForward";
    private const string SHADER_PROP_VIGNETTE_PIXEL = "_VignettePixel";
    private const string SHADER_PROP_VIGNETTE_ALPHA = "_VignetteAlpha";

    [Tooltip("The mesh used to render the iris.")]
    [SerializeField]
    private MeshRenderer iris;

    /// The radius of the sphere on which the iris is rendered.
    [Tooltip("Determines where the 'hole' will appear in z-space (meters).")]
    [SerializeField]
    private float irisDistance = 5.0f;

    /// Default color is a gentle light blue.
    [Tooltip("Determines the cage color of the vignette.")]
    [SerializeField]
    private Color cageColor = new Color(0.25f, 0.25f, 0.5f, 1.0f);

    [SerializeField]
    private Material irisDepthMaterial;

    [SerializeField]
    private Material irisColorMaterial;

    private int vignetteMinMaxId;
    private int camForwardId;
    private int vignetteCageColorId;
    private int vignettePSId;
    private int vignetteAlphaId;

    private bool fovNeedsUpdate = true;
    private Vector3 lastForwardVector;
    private Color lastCageColor;
    private Vector4 vignetteVector;
    private Vector4 vignettePixelShader;
    private float vignetteAlpha = 1.0f;

    public float CurrentFOV {
      get {
        return currentFOV;
      }
      set {
        if (currentFOV == value) {
          return;
        }

        currentFOV = value;
        currentFOV = Mathf.Clamp(currentFOV, 0.0f, MAX_FOV);
        fovNeedsUpdate = true;
      }
    }

    private float currentFOV = MAX_FOV;

    public float CurrentFadeFOV {
      get {
        return currentFadeFOV;
      }
      set {
        if (currentFadeFOV == value) {
          return;
        }

        currentFadeFOV = value;
        currentFadeFOV = Mathf.Clamp(currentFadeFOV, 0.0f, MAX_FOV);
        fovNeedsUpdate = true;
      }
    }

    private float currentFadeFOV = DEFAULT_FADE_FOV;

    public bool IsVignetteVisible {
      get {
        if (iris == null) {
          return false;
        }

        return iris.enabled;
      }
    }

    public float Alpha {
      get {
        return vignetteAlpha;
      }
      set {
        if (vignetteAlpha == value) {
          return;
        }

        vignetteAlpha = value;
        Shader.SetGlobalFloat(vignetteAlphaId, vignetteAlpha);

        if (Alpha == 1.0f) {
          if (iris.sharedMaterials.Length == 1) {
            iris.sharedMaterials = new Material[] { irisDepthMaterial, irisColorMaterial };
          }
        } else {
          if (iris.sharedMaterials.Length == 2) {
            iris.sharedMaterials = new Material[] { irisColorMaterial };
          }
        }
      }
    }

    void Awake() {
      vignetteMinMaxId = Shader.PropertyToID(SHADER_PROP_VIGNETTE_MIN_MAX);
      vignetteCageColorId = Shader.PropertyToID(SHADER_PROP_VIGNETTE_CAGE_COLOR);
      camForwardId = Shader.PropertyToID(SHADER_PROP_VIGNETTE_CAMERA_FORWARD);
      vignettePSId = Shader.PropertyToID(SHADER_PROP_VIGNETTE_PIXEL);
      vignetteAlphaId = Shader.PropertyToID(SHADER_PROP_VIGNETTE_ALPHA);

      Shader.SetGlobalFloat(vignetteAlphaId, vignetteAlpha);
      Shader.SetGlobalVector(vignetteCageColorId, cageColor);
      lastCageColor = cageColor;
    }

    void LateUpdate() {
      if (iris == null) {
        Debug.LogWarning("Iris is unassigned.");
        return;
      }

      if (Camera.main == null) {
        Debug.LogWarning("Main Camera does not exist.");
        return;
      }

      // Make sure the iris is always parented to the main camera.
      if (iris.transform.parent != Camera.main.transform) {
        iris.transform.SetParent(Camera.main.transform, false);
      }

      // We can minimize the number of times these are set, while still making
      // this work reliably in the editor by caching the previously set value.
      Vector3 forward = Camera.main.transform.forward;
      if (forward != lastForwardVector) {
        // We can assume this needs to be updated continuously, although maybe
        // we can find an existing shader variable for this.
        Shader.SetGlobalVector(camForwardId, forward);
        lastForwardVector = forward;
      }

      if (cageColor != lastCageColor) {
        Shader.SetGlobalVector(vignetteCageColorId, cageColor);
        lastCageColor = cageColor;
      }

      if (!fovNeedsUpdate) {
        return;
      }

      UpdateFOV();
      UpdateIrisEnabled();
    }

    private void UpdateIrisEnabled() {
      if (iris == null) {
        return;
      }

      if (currentFOV > CULL_IRIS_FOV) {
        if (iris.enabled) {
          iris.enabled = false;
        }
      } else if (!iris.enabled) {
        iris.enabled = true;
      }
    }

#if UNITY_EDITOR
    public void SetPreviewFOV(float newFOV) {
      if (!Application.isPlaying) {
        Shader.SetGlobalVector(vignetteCageColorId, cageColor);
        CurrentFOV = newFOV;
        UnityEditor.EditorUtility.SetDirty(this);
      }
    }

    public void SetPreviewAlpha(float newAlpha) {
      if (!Application.isPlaying) {
        Alpha = newAlpha;
      }
    }
#endif

    private void UpdateFOV() {
      // The following components are used for occluding the cage.
      vignettePixelShader.x = Mathf.Cos(Mathf.Deg2Rad * (currentFOV + 3 * currentFadeFOV) * 0.5f);
      vignettePixelShader.y = Mathf.Cos(Mathf.Deg2Rad * currentFOV * 0.5f);

      vignetteVector.y = (1.0f / (Mathf.Deg2Rad * currentFadeFOV));
      vignetteVector.x = Mathf.Clamp01(Mathf.Deg2Rad * currentFOV * 0.5f / HALF_PI);

      // The following components of vignetteVector are used for depth culling.
      vignetteVector.z = Mathf.Clamp01(
        Mathf.Deg2Rad
        * (currentFOV + DEPTH_CULLING_SAFETY_FACTOR * currentFadeFOV)
        * 0.5f
        / HALF_PI);

      vignetteVector.w = irisDistance;
      Shader.SetGlobalVector(vignetteMinMaxId, vignetteVector);

      Shader.SetGlobalVector(vignettePSId, vignettePixelShader);
      fovNeedsUpdate = false;
    }
  }
}
