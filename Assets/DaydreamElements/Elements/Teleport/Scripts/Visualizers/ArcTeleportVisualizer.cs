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

namespace DaydreamElements.Teleport {
  /// This class works with the TeleportController to render
  /// a bezier arc for showing your teleport destination. This class
  /// will generate geometry used for the arc, and calculates the 3
  /// required verticies for the bezier arc curve start/end/control.
  /// These 3 verticies are passed to the bezier arc shader which
  /// uses a vertex shader to reposition the arc geometry into place.
  [RequireComponent(typeof(MeshFilter))]
  [RequireComponent(typeof(MeshRenderer))]
  public class ArcTeleportVisualizer : BaseTeleportVisualizer {
    /// Smoothness of the arc.
    [Tooltip("Number of steps in arc geometry.")]
    [Range(3, 200)]
    public int arcSmoothness = 100;

    /// Controller angle from ground to start bending arc.
    [Tooltip("Controller angle from ground to start arc bending")]
    [Range(0, 180)]
    public float startBendingAngle = 90;

    /// Amount of bending to apply to the arc.
    [Range(0, .1f)]
    [Tooltip("Amount of bending in the arc")]
    public float arcBendingStrength = .04f;

    /// Width of the selection line.
    [Tooltip("Width of the selection line")]
    public float lineWidth = .08f;

    /// Offset for line so it doesn't overlap controller.
    [Tooltip("Offset from controller")]
    public float lineStartOffset = .1f;

    /// Offset for line so we don't overlap the target.
    [Tooltip("Offset from selection target")]
    public float lineEndOffset = .2f;

    /// Material for line when selection is valid.
    [Tooltip("Valid selection material for line")]
    public Material validSelectionMat;

    /// Material for line when selection is invalid.
    [Tooltip("Invalid selection material for line")]
    public Material invalidSelectionMat;

    /// Prefab for object to place at final teleport location.
    [Tooltip("Optional target to place at end of line for valid selections")]
    public GameObject targetPrefab;

    /// Multiplier for growing line width over distance.
    public float distanceLineScaler = 0.01f;

    /// Instance of the target prefab.
    private GameObject target;

    /// Property block for material values.
    private MaterialPropertyBlock propertyBlock;

    /// Reference to the mesh for destroying it later.
    private Mesh mesh;

    void Awake() {
      propertyBlock = new MaterialPropertyBlock();
      GetComponent<MeshRenderer>().enabled = false;
      GenerateMesh();
    }

    void OnDestroy() {
      if (mesh != null) {
        Destroy(mesh);
      }
    }

    // Generate a flat mesh in the 0-1 z+ direction for BezierArcShader to manipulate.
    private void GenerateMesh() {
      if (arcSmoothness == 0) {
        Debug.LogError ("Can't build line mesh with 0 arcSteps");
        return;
      }

      // Smoothness is used as the number of vertex rows in the arc.
      int vertexRowCount = arcSmoothness;
      float increment = 1 / (float)vertexRowCount;

      int vertexCount = vertexRowCount * 2;
      Vector3[] verticies = new Vector3[vertexCount];
      Vector2[] uvs = new Vector2[vertexCount];
      int[] triangles = new int[(vertexRowCount - 1) * 6];

      // The mesh has a width of 2 (from x -1 to 1), shader update to final width.
      float width = 1;

      // Generate verticies first with z-position from 0-1.
      for (int i = 0; i < vertexRowCount; i++) {
        float zOffset = i * increment;
        int vertOffset = i * 2;
        verticies[vertOffset] = new Vector3(width, 0, zOffset); // Right vertex.
        verticies[vertOffset + 1] = new Vector3(-width, 0, zOffset); // Left vertex.

        uvs[vertOffset] = new Vector2(0, zOffset);
        uvs[vertOffset + 1] = new Vector2(1, zOffset);
      }

      // Create triangles by connecting verticies in step ahead of it.
      for (int i = 0; i < (vertexRowCount - 1); i++) {
        // Index of verticies for triangles.
        int vertexOffset = i * 2; // 2 verticies per row, so skip over previous.
        int backRight = vertexOffset;
        int backLeft = vertexOffset + 1;
        int frontRight = vertexOffset + 2;
        int frontLeft = vertexOffset + 3;

        // Right triangle.
        int triangleOffset = i * 6; // we create 2 triangles for each row.
        triangles[triangleOffset] = frontRight;
        triangles[triangleOffset + 1] = backRight;
        triangles[triangleOffset + 2] = frontLeft;

        // Left triangle.
        triangles[triangleOffset + 3] = backRight;
        triangles[triangleOffset + 4] = backLeft;
        triangles[triangleOffset + 5] = frontLeft;
      }

      // We hold onto the mesh since unity doesn't deallocated meshes automatically.
      mesh = new Mesh ();
      GetComponent<MeshFilter>().mesh = mesh;
      mesh.vertices = verticies;
      mesh.uv = uvs;
      mesh.triangles = triangles;

      // Force the mesh to always have a visible bounds.
      float boundValue = 10000;
      mesh.bounds = new Bounds(transform.position,
        new Vector3(boundValue, boundValue, boundValue));
    }

    // Start teleport selection.
    public override void StartSelection(Transform controller) {
      GetComponent<MeshRenderer>().enabled = true;
      ShowTarget();
    }

    // End teleport selection.
    public override void EndSelection() {
      GetComponent<MeshRenderer>().enabled = false;
      HideTarget();
    }

    // Visualize the current selection.
    public override void UpdateSelection(
        Transform controllerTransform,
        BaseTeleportDetector.Result selectionResult) {
      // Calculate verticies that define our bezier arc.
      Vector3 start;
      Vector3 end;
      Vector3 control;

      // Calculate the position for the 3 verticies.
      UpdateLine(controllerTransform, selectionResult,
        out start, out end, out control);

      // Update the material on the line.
      UpdateLineMaterial(selectionResult.selectionIsValid,
        start, end, control);

      // Update the target objects position at end of line.
      UpdateTarget(selectionResult);
    }

    private void UpdateLine(
        Transform controllerTransform,
        BaseTeleportDetector.Result selectionResult,
        out Vector3 start,
        out Vector3 end,
        out Vector3 control) {
      // Start point of line.
      start = controllerTransform.position
        + (controllerTransform.forward * lineStartOffset);

      // End line at selection or max distance.
      end = selectionResult.selection;

      // We can only offset if the line is long enough for it.
      float lineLength = (end - start).magnitude;
      float clampedEndOffset = Mathf.Clamp(this.lineEndOffset, 0, lineLength);

      Vector3 lineEndVector = end - start
        - (controllerTransform.forward * clampedEndOffset);
      end = start + lineEndVector;

      // Get control point used to bend the line into an arc.
      control = ControlPointForLine(
        start,
        end,
        selectionResult.maxDistance,
        controllerTransform);
    }

    private void ShowTarget() {
      if (targetPrefab == null) {
        return;
      }

      if (target == null) {
        target = Instantiate(targetPrefab, transform) as GameObject;
      }

      target.SetActive(true);
    }

    private void HideTarget() {
      if (target == null) {
        return;
      }

      target.SetActive(false);
    }

    protected void UpdateTarget(BaseTeleportDetector.Result selectionResult) {
      if (target == null) {
        return;
      }

      if (selectionResult.selectionIsValid == false) {
        target.SetActive(false);
        return;
      }

      target.SetActive(true);
      target.transform.position = selectionResult.selection;
    }

    protected void UpdateLineMaterial(bool isValidSelection,
        Vector3 start,
        Vector3 end,
        Vector3 control) {
      MeshRenderer mr = GetComponent<MeshRenderer>();
      if (mr == null) {
        Debug.LogError("Can't update material without mesh renderer");
        return;
      }

      if (isValidSelection) {
        mr.material = validSelectionMat;
      }
      else{
        mr.material = invalidSelectionMat;
      }

      // Update the shader with the most recent bezier path info.
      propertyBlock.SetVector("_StartPosition", transform.InverseTransformPoint(start));
      propertyBlock.SetVector("_EndPosition", transform.InverseTransformPoint(end));
      propertyBlock.SetVector("_ControlPosition", transform.InverseTransformPoint(control));
      propertyBlock.SetFloat("_LineWidth", lineWidth);
      propertyBlock.SetFloat("_DistanceScale", distanceLineScaler);

      mr.SetPropertyBlock(propertyBlock);
    }

    private Vector3 ControlPointForLine(Vector3 start, Vector3 end, float maxDistance, Transform controller) {
      Vector3 halfVect = end - start;
      Vector3 controlPoint = start + (halfVect.normalized * (halfVect.magnitude / 2));

      float angleFromDown = Vector3.Angle(Vector3.down, controller.forward);

      if (angleFromDown <= startBendingAngle) {
        return controlPoint;
      }

      float bendAmount = (angleFromDown - startBendingAngle) * arcBendingStrength;

      controlPoint += new Vector3(0, bendAmount, 0);

      return controlPoint;
    }
  }
}

