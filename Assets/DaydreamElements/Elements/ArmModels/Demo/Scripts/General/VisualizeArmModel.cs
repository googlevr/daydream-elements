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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DaydreamElements.ArmModels {

  /// Early version of script for visualizing an arm model.
  public class VisualizeArmModel : MonoBehaviour {
    public enum Joint {
      SHOULDER,
      BICEP,
      ELBOW,
      FOREARM,
      WRIST,
      CONTROLLER,
      LASER
    }

    [SerializeField]
    private Transform shoulderJoint;

    [SerializeField]
    private Transform bicepLimb;

    [SerializeField]
    private Transform elbowJoint;

    [SerializeField]
    private Transform forearmLimb;

    [SerializeField]
    private Transform wristJoint;

    [SerializeField]
    private Transform controller;

    [SerializeField]
    private LineRenderer laser;

    [SerializeField]
    private Color laserDefaultColor;

    [SerializeField]
    private Color laserHighlightColor;

    [SerializeField]
    private float wristOffset = -0.05f;

    public GvrBaseArmModel armModel;

    private MeshRenderer shoulderJointRenderer;
    private MeshRenderer bicepLimbRenderer;
    private MeshRenderer elbowJointRenderer;
    private MeshRenderer forearmLimbRenderer;
    private MeshRenderer wristJointRenderer;
    private MeshRenderer controllerRenderer;
    private bool isLaserHighlighted;

    private const float BICEP_SCALE_FACTOR = 4.4f;
    private const float FOREARM_SCALE_FACTOR = 3.6f;

    private const string OUTLINE_KEYWORD = "OUTLINE_ENABLED";

    public void SetOutlineEnabled(Joint joint, bool enabled) {
      switch (joint) {
        case Joint.SHOULDER:
          SetOutlineEnabledForMesh(shoulderJointRenderer, enabled);
          break;
        case Joint.BICEP:
          SetOutlineEnabledForMesh(bicepLimbRenderer, enabled);
          break;
        case Joint.ELBOW:
          SetOutlineEnabledForMesh(elbowJointRenderer, enabled);
          break;
        case Joint.FOREARM:
          SetOutlineEnabledForMesh(forearmLimbRenderer, enabled);
          break;
        case Joint.WRIST:
          SetOutlineEnabledForMesh(wristJointRenderer, enabled);
          break;
        case Joint.CONTROLLER:
          SetOutlineEnabledForMesh(controllerRenderer, enabled);
          break;
        case Joint.LASER:
          isLaserHighlighted = enabled;
          break;
      }
    }

    public void SetAllOutlinesEnabled(bool enabled) {
      SetOutlineEnabledForMesh(shoulderJointRenderer, enabled);
      SetOutlineEnabledForMesh(bicepLimbRenderer, enabled);
      SetOutlineEnabledForMesh(elbowJointRenderer, enabled);
      SetOutlineEnabledForMesh(forearmLimbRenderer, enabled);
      SetOutlineEnabledForMesh(wristJointRenderer, enabled);
      SetOutlineEnabledForMesh(controllerRenderer, enabled);
      SetOutlineEnabled(Joint.LASER, enabled);
    }

    void Awake() {
      shoulderJointRenderer = shoulderJoint.GetComponentInChildren<MeshRenderer>();
      bicepLimbRenderer = bicepLimb.GetComponentInChildren<MeshRenderer>();
      elbowJointRenderer = elbowJoint.GetComponentInChildren<MeshRenderer>();
      forearmLimbRenderer = forearmLimb.GetComponentInChildren<MeshRenderer>();
      wristJointRenderer = wristJoint.GetComponentInChildren<MeshRenderer>();

      if (controller != null) {
        controllerRenderer = controller.GetComponentInChildren<MeshRenderer>();
      }

      SetAllOutlinesEnabled(false);
    }

    void OnEnable() {
      GvrControllerInput.OnPostControllerInputUpdated += OnPostControllerUpdated;
      OnPostControllerUpdated();
    }

    void OnDisable() {
      GvrControllerInput.OnPostControllerInputUpdated -= OnPostControllerUpdated;
    }

    private void OnPostControllerUpdated() {
      IArmModelVisualProvider armModelVisualProvider = armModel as IArmModelVisualProvider;

      if (armModelVisualProvider == null) {
        return;
      }

      // Shoulder Joint.
      shoulderJoint.localPosition = armModelVisualProvider.ShoulderPosition;
      shoulderJoint.localRotation = armModelVisualProvider.ShoulderRotation;

      // Elbow Joint.
      elbowJoint.localPosition = armModelVisualProvider.ElbowPosition;
      elbowJoint.localRotation = armModelVisualProvider.ElbowRotation;

      // Bicep Limb.
      Vector3 elbowShoulderDiff = elbowJoint.localPosition - shoulderJoint.localPosition;
      Vector3 bicepPosition = shoulderJoint.localPosition + (elbowShoulderDiff * 0.5f);
      bicepLimb.localPosition = bicepPosition;
      bicepLimb.LookAt(shoulderJoint, elbowJoint.forward);

      bicepLimb.localScale = new Vector3(1.0f, 1.0f, elbowShoulderDiff.magnitude * BICEP_SCALE_FACTOR);

      // Wrist Joint.
      wristJoint.localPosition = armModelVisualProvider.WristPosition;
      wristJoint.localRotation = armModelVisualProvider.WristRotation;
      Vector3 wristDir = armModelVisualProvider.WristRotation * Vector3.forward;
      wristJoint.localPosition = wristJoint.localPosition + (wristDir * wristOffset);

      // Forearm Limb.
      Vector3 wristElbowDiff = wristJoint.localPosition - elbowJoint.localPosition;
      Vector3 forearmPosition = elbowJoint.localPosition + (wristElbowDiff * 0.5f);
      forearmLimb.localPosition = forearmPosition;
      forearmLimb.LookAt(elbowJoint, wristJoint.up);
      forearmLimb.localScale = new Vector3(1.0f, 1.0f, wristElbowDiff.magnitude * FOREARM_SCALE_FACTOR);

      if (laser != null) {
        if (isLaserHighlighted) {
          laser.startColor = laserHighlightColor;
          laser.enabled = true;
        } else {
          laser.startColor = laserDefaultColor;
        }

        Color color = laser.startColor;
        color.a *= armModel.PreferredAlpha;
        laser.startColor = color;
        laser.endColor = Color.clear;

      }
    }

    private void SetOutlineEnabledForMesh(MeshRenderer meshRenderer, bool enabled) {
      if (meshRenderer == null) {
        return;
      }

      if (enabled) {
        meshRenderer.enabled = true;
        meshRenderer.sharedMaterial.EnableKeyword(OUTLINE_KEYWORD);
      } else {
        meshRenderer.sharedMaterial.DisableKeyword(OUTLINE_KEYWORD);
      }
    }
  }
}