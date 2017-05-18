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
using System.Collections.Generic;
using DaydreamElements.Common;

namespace DaydreamElements.ClickMenu {

  /// On-screen game object associated with a menu item.
  [RequireComponent(typeof(SpriteRenderer))]
  [RequireComponent(typeof(MeshCollider))]
  public class ClickMenuIcon : MonoBehaviour,
                             IPointerEnterHandler,
                             IPointerExitHandler {
    private const int NUM_SIDES_CIRCLE_MESH = 48;

    /// Distance to pop out icon when hovered in meters.
    private const float HOVER_PUSH = 0.04f;

    /// Distance to push the menu in depth during fades.
    private const float FADE_DEPTH = 0.2f;

    /// Distance the background is pushed when idle in meters.
    private const float BACKGROUND_PUSH = 0.01f;

    /// Rate at which to push the icon during hover.
    private const float PUSH_RATE = 0.006f;

    /// Radius from center to menu item in units of scale.
    private const float ITEM_SPACING = 0.15f;

    /// The spacing for this many items is the minimum spacing.
    private const int MIN_ITEM_SPACE = 5;

    /// Time for the fading animation in seconds.
    private const float ANIMATION_TIME = 0.24f;

    /// Scaling factor for the tooltip text.
    private const float TOOLTIP_SCALE = 0.1f;

    /// Distance in meters the icon hovers above pie slices to prevent occlusion.
    private const float ICON_Z_OFFSET = 0.1f;

    private Vector3 startPosition;
    private Vector3 startScale;
    private Vector3 menuCenter;
    private Quaternion menuOrientation;
    private float menuScale;
    private ClickMenuIcon parentMenu;
    private List<ClickMenuIcon> childMenus;
    private GameObject background;
    private ClickMenuItem menuItem;
    private AssetTree.Node menuNode;
    public ClickMenuRoot menuRoot { private get; set; }
    private GameObject tooltip;
    private Vector3 localOffset;
    private MeshCollider meshCollider;
    private Mesh sharedMesh;
    private MaterialPropertyBlock propertyBlock;
    private SpriteRenderer spriteRenderer;
    private SpriteRenderer backgroundSpriteRenderer;
    private MeshRenderer tooltipRenderer;

    private GameObject pieBackground;
    private MeshRenderer pieMeshRenderer;
    private Color pieStartColor;

    private const float INNER_RADIUS = 0.6f;
    private const float OUTER_RADIUS = 1.6f;
    private float startAngle;
    private float endAngle;

    enum FadeType {
      NoFade,
      FadeInPush,
      FadeInPull,
      FadeOutPush,
      FadeOutPull
    }
    private FadeType fadeType;
    private bool selected;
    private float fadeStartTime;
    private bool destroyOnClose;
    private float pushDepth;
    private bool active;
    private bool isBackButton;

    public GameObject tooltipPrefab;

    void Awake() {
      sharedMesh = new Mesh();
      sharedMesh.name = "Pie Mesh";
      meshCollider = GetComponent<MeshCollider>();
      propertyBlock = new MaterialPropertyBlock();
      spriteRenderer = GetComponent<SpriteRenderer>();
      childMenus = new List<ClickMenuIcon>();
      active = false;
      selected = false;
      destroyOnClose = false;
      isBackButton = false;
      fadeType = FadeType.NoFade;
    }

    public void SetDummy() {
      active = false;
    }

    public void Initialize(ClickMenuRoot root, ClickMenuIcon _parentMenu, AssetTree.Node node,
                           Vector3 _menuCenter, float scale, Vector3 offset) {
      string name = (node == null ? "Back " : ((ClickMenuItem)node.value).toolTip);
      gameObject.name = name + " Item";
      parentMenu = _parentMenu;
      startPosition = transform.position;
      startScale = transform.localScale;
      menuRoot = root;
      menuNode = node;
      menuCenter = _menuCenter;
      menuOrientation = transform.rotation;
      menuScale = scale;
      localOffset = offset;
      background = null;
      active = true;
      if (node != null) {
        // Set foreground icon
        menuItem = (ClickMenuItem)node.value;
        spriteRenderer.sprite = menuItem.icon;

        // Set background icon
        if (menuItem.background) {
          background = new GameObject(name + " Item Background");
          background.transform.parent = transform.parent;
          background.transform.localPosition = transform.localPosition + transform.forward * BACKGROUND_PUSH;
          background.transform.localRotation = transform.localRotation;
          background.transform.localScale = transform.localScale;
          backgroundSpriteRenderer = background.AddComponent<SpriteRenderer>();
          backgroundSpriteRenderer.sprite = menuItem.background;
        }

        // Set tooltip text
        tooltip = Instantiate(tooltipPrefab);
        tooltip.name = name + " Tooltip";
        tooltip.transform.parent = transform.parent;
        tooltip.transform.localPosition = menuCenter;
        tooltip.transform.localRotation = menuOrientation;
        tooltip.transform.localScale = transform.localScale * TOOLTIP_SCALE;
        tooltip.GetComponent<TextMesh>().text = menuItem.toolTip.Replace('\\','\n');
        tooltipRenderer = tooltip.GetComponent<MeshRenderer>();
        SetTooltipAlpha(0.0f);
      } else {
        // This is a back button
        spriteRenderer.sprite = root.backIcon;
        isBackButton = true;
      }

      pieBackground = null;
      pieMeshRenderer = null;
      if (root.pieMaterial) {
        pieBackground = new GameObject(name + " Pie Background");
        pieBackground.transform.SetParent(transform.parent, false);
        pieBackground.transform.localPosition = transform.localPosition;
        pieBackground.transform.localRotation = transform.localRotation;
        pieBackground.transform.localScale = transform.localScale;
        pieBackground.AddComponent<MeshFilter>().sharedMesh = sharedMesh;
        pieMeshRenderer = pieBackground.AddComponent<MeshRenderer>();
        pieMeshRenderer.sharedMaterial = root.pieMaterial;
        pieStartColor = root.pieMaterial.GetColor("_Color");
      }

      parentMenu.childMenus.Add(this);
      StartFade(FadeType.FadeInPush);
    }

    private void MakeMeshColliderCircle() {
      Vector3[] vertices = new Vector3[NUM_SIDES_CIRCLE_MESH*2 + 1];
      int[] triangles = new int[NUM_SIDES_CIRCLE_MESH * 9];

      vertices[0] = new Vector3(0.0f, 0.0f, ICON_Z_OFFSET);
      float pushScaled = pushDepth / transform.localScale[0];
      for (int i = 0; i < NUM_SIDES_CIRCLE_MESH; i++) {
        float angle = i * 2.0f * Mathf.PI / NUM_SIDES_CIRCLE_MESH;
        float x = Mathf.Sin(angle) * INNER_RADIUS;
        float y = Mathf.Cos(angle) * INNER_RADIUS;
        vertices[i + 1] = new Vector3(x, y, ICON_Z_OFFSET);
        vertices[i + NUM_SIDES_CIRCLE_MESH + 1] = new Vector3(x, y, pushScaled + ICON_Z_OFFSET);
        int nextIx = (i == NUM_SIDES_CIRCLE_MESH - 1 ? 1 : i + 2);
        triangles[i * 9 + 0] = i + 1;
        triangles[i * 9 + 1] = nextIx;
        triangles[i * 9 + 2] = 0;
        triangles[i * 9 + 3] = i + 1;
        triangles[i * 9 + 4] = i + NUM_SIDES_CIRCLE_MESH + 1;
        triangles[i * 9 + 5] = nextIx;
        triangles[i * 9 + 6] = nextIx;
        triangles[i * 9 + 7] = i + NUM_SIDES_CIRCLE_MESH + 1;
        triangles[i * 9 + 8] = nextIx + NUM_SIDES_CIRCLE_MESH;
      }

      sharedMesh.vertices = vertices;
      sharedMesh.triangles = triangles;
    }

    private void MakeMeshCollider() {
      if (localOffset.sqrMagnitude <= Mathf.Epsilon) {
        MakeMeshColliderCircle();
        return;
      }

      int numSides = (int)((endAngle - startAngle) * 8.0f) + 1;
      Vector3[] vertices = new Vector3[numSides * 4 + 4];
      int[] triangles = new int[numSides * 18 + 12];

      float outerRadius = localOffset.magnitude + Mathf.Min(localOffset.magnitude - INNER_RADIUS, OUTER_RADIUS - 1.0f);
      float pushScaled = pushDepth / transform.localScale[0];
      float x = Mathf.Sin(startAngle);
      float y = Mathf.Cos(startAngle);
      vertices[0] = new Vector3(x * INNER_RADIUS, y * INNER_RADIUS, pushScaled + ICON_Z_OFFSET) - localOffset;
      vertices[1] = new Vector3(x * outerRadius, y * outerRadius, pushScaled + ICON_Z_OFFSET) - localOffset;
      vertices[2] = new Vector3(x * INNER_RADIUS, y * INNER_RADIUS, ICON_Z_OFFSET) - localOffset;
      vertices[3] = new Vector3(x * outerRadius, y * outerRadius, ICON_Z_OFFSET) - localOffset;

      triangles[0] = 0;
      triangles[1] = 1;
      triangles[2] = 2;
      triangles[3] = 3;
      triangles[4] = 2;
      triangles[5] = 1;

      for (int i = 0; i < numSides; i++) {
        float angle = startAngle + (i + 1) * (endAngle - startAngle) / numSides;
        x = Mathf.Sin(angle);
        y = Mathf.Cos(angle);
        vertices[i*4 + 4] = new Vector3(x * INNER_RADIUS, y * INNER_RADIUS, pushScaled + ICON_Z_OFFSET) - localOffset;
        vertices[i*4 + 5] = new Vector3(x * outerRadius, y * outerRadius, pushScaled + ICON_Z_OFFSET) - localOffset;
        vertices[i*4 + 6] = new Vector3(x * INNER_RADIUS, y * INNER_RADIUS, ICON_Z_OFFSET) - localOffset;
        vertices[i*4 + 7] = new Vector3(x * outerRadius, y * outerRadius, ICON_Z_OFFSET) - localOffset;

        triangles[i*18 + 6]  = i*4 + 0;
        triangles[i*18 + 7]  = i*4 + 2;
        triangles[i*18 + 8]  = i*4 + 4;
        triangles[i*18 + 9]  = i*4 + 6;
        triangles[i*18 + 10] = i*4 + 4;
        triangles[i*18 + 11] = i *4 + 2;

        triangles[i * 18 + 12] = i*4 + 2;
        triangles[i * 18 + 13] = i*4 + 3;
        triangles[i * 18 + 14] = i*4 + 6;
        triangles[i * 18 + 15] = i*4 + 7;
        triangles[i * 18 + 16] = i*4 + 6;
        triangles[i * 18 + 17] = i*4 + 3;

        triangles[i * 18 + 18] = i*4 + 3;
        triangles[i * 18 + 19] = i*4 + 1;
        triangles[i * 18 + 20] = i*4 + 7;
        triangles[i * 18 + 21] = i*4 + 5;
        triangles[i * 18 + 22] = i*4 + 7;
        triangles[i * 18 + 23] = i*4 + 1;
      }

      int lastTriangleIx = numSides * 18 + 6;
      int lastVertIx = numSides * 4;
      triangles[lastTriangleIx + 0] = lastVertIx + 2;
      triangles[lastTriangleIx + 1] = lastVertIx + 1;
      triangles[lastTriangleIx + 2] = lastVertIx + 0;
      triangles[lastTriangleIx + 3] = lastVertIx + 1;
      triangles[lastTriangleIx + 4] = lastVertIx + 2;
      triangles[lastTriangleIx + 5] = lastVertIx + 3;

      sharedMesh.vertices = vertices;
      sharedMesh.triangles = triangles;
    }

    /// Opens a new menu, returns true if a menu transition needs to occur.
    public static bool OpenMenu(ClickMenuRoot root, AssetTree.Node treeNode, ClickMenuIcon parent,
                                Vector3 center, Quaternion orientation, float scale) {
      // Determine how many children are in the sub-menu
      List<AssetTree.Node> childItems = treeNode.children;

      // If this is the end of a menu, invoke the action and return early
      if (childItems.Count == 0) {
        if (parent.menuItem.closeAfterSelected) {
          root.CloseAll();
        }
        return false;
      }

      // Radius needs to expand when there are more icons
      float radius = ITEM_SPACING * Mathf.Max(childItems.Count, MIN_ITEM_SPACE) / (2.0f * Mathf.PI);

      // Create and arrange the icons in a circle
      float arcAngle = 2.0f * Mathf.PI / childItems.Count;
      for (int i = 0; i < childItems.Count; ++i) {
        float angle = i * arcAngle;
        Vector3 posOffset = new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0.0f) * radius;

        ClickMenuIcon childMenu = (ClickMenuIcon)Instantiate(root.menuIconPrefab, root.transform);
        childMenu.transform.position = center + (orientation * posOffset);
        childMenu.transform.rotation = orientation;
        childMenu.transform.localScale = Vector3.one * scale;
        childMenu.startAngle = angle - arcAngle / 2;
        childMenu.endAngle = angle + arcAngle / 2;
        childMenu.Initialize(root, parent, childItems[i], center, scale, posOffset / scale);
      }

      // Also create a back button
      ClickMenuIcon backButton = (ClickMenuIcon)Instantiate(root.menuIconPrefab, root.transform);
      backButton.transform.position = center;
      backButton.transform.rotation = orientation;
      backButton.transform.localScale = Vector3.one * scale;
      backButton.Initialize(root, parent, null, center, scale, Vector3.zero);
      return true;
    }

    private bool OpenSubMenu() {
      return OpenMenu(menuRoot, menuNode, this, menuCenter, menuOrientation, menuScale);
    }

    public void CloseSubMenu() {
      if (!parentMenu) {
        menuRoot.CloseAll();
      } else {
        FadeChildren(FadeType.FadeOutPull, true);
        parentMenu.FadeChildren(FadeType.FadeInPull);
        childMenus.Clear();
      }
    }

    void Update() {
      // Update the push animation
      float pushRate = (selected ? PUSH_RATE : -PUSH_RATE);
      pushDepth = Mathf.Clamp(pushDepth + pushRate, 0.0f, HOVER_PUSH);
      SetTooltipAlpha(pushDepth / HOVER_PUSH);

      // Update back button transparency
      if (isBackButton) {
        SetButtonTransparency(pushDepth / HOVER_PUSH);
      }

      if (active) {
        // Draw the pie background with highlight
        if (pieMeshRenderer) {
          Color newColor = pieStartColor;
          newColor.a += (pushDepth / HOVER_PUSH) * (1.0f - newColor.a);
          pieMeshRenderer.GetPropertyBlock(propertyBlock);
          propertyBlock.SetColor("_Color", newColor);
          pieMeshRenderer.SetPropertyBlock(propertyBlock);
        }

        // Make button interactive and process clicks
        SetScaleT(0.0f);
        MakeMeshCollider();
        if (selected && (GvrController.ClickButtonDown)) {
          menuRoot.MakeSelection(menuItem ? menuItem.id : -1);
          if (isBackButton) {
            parentMenu.CloseSubMenu();
          } else {
            if (OpenSubMenu()) {
              parentMenu.FadeChildren(FadeType.FadeOutPush);
            }
          }
        }
      } else if (fadeType != FadeType.NoFade) {
        // Apply fading animations
        float t = Mathf.Min((Time.time - fadeStartTime) / ANIMATION_TIME, 1.0f);
        if (fadeType == FadeType.FadeInPush) {
          SetScaleT(1.0f - t);
          SetTransparency(t);
          MakeMeshCollider();
          if (t >= 1.0f) {
            FinishFadeIn();
          }
        } else if (fadeType == FadeType.FadeInPull) {
          SetScaleT(t - 1.0f);
          SetTransparency(t);
          MakeMeshCollider();
          if (t >= 1.0f) {
            FinishFadeIn();
          }
        } else if (fadeType == FadeType.FadeOutPush) {
          selected = false;
          SetScaleT(-t);
          SetTransparency(1.0f - t);
          if (t >= 1.0f) {
            FinishFadeOut();
          }
        } else if (fadeType == FadeType.FadeOutPull) {
          selected = false;
          SetScaleT(t);
          SetTransparency(1.0f - t);
          if (t >= 1.0f) {
            FinishFadeOut();
          }
        }
      }
    }

    private void SetScaleT(float t) {
      float scaleMult = Mathf.Max(1.0f - Mathf.Abs(t), 0.01f);
      Vector3 delta = (startPosition - menuCenter) * (1.0f - t);
      transform.position = menuCenter + delta - transform.forward * pushDepth;
      transform.localScale = startScale * scaleMult;
      if (background) {
        background.transform.position = menuCenter + transform.forward * BACKGROUND_PUSH + delta;
        background.transform.localScale = startScale * scaleMult;
      }
      if (pieBackground) {
        pieBackground.transform.position = startPosition - pieBackground.transform.forward * pushDepth;
        pieBackground.transform.localScale = startScale;
      }
    }

    private void SetButtonTransparency(float alpha) {
      Color alphaColor = new Color(1.0f, 1.0f, 1.0f, alpha);
      if (isBackButton) {
        alphaColor.a = Mathf.Min(pushDepth / HOVER_PUSH, alpha);
      }
      spriteRenderer.GetPropertyBlock(propertyBlock);
      propertyBlock.SetColor("_Color", alphaColor);
      spriteRenderer.SetPropertyBlock(propertyBlock);
      if (background) {
        backgroundSpriteRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_Color", alphaColor);
        backgroundSpriteRenderer.SetPropertyBlock(propertyBlock);
      }
    }

    private void SetTransparency(float alpha) {
      SetButtonTransparency(alpha);
      if (pieMeshRenderer) {
        Color pieColor = pieStartColor;
        pieColor.a *= alpha;
        pieMeshRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_Color", pieColor);
        pieMeshRenderer.SetPropertyBlock(propertyBlock);
      }
    }

    private void SetTooltipAlpha(float alpha) {
      if (tooltip) {
        Color alphaColor = new Color(1.0f, 1.0f, 1.0f, alpha);
        tooltipRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_Color", alphaColor);
        tooltipRenderer.SetPropertyBlock(propertyBlock);
      }
    }

    void OnDestroy() {
      Destroy(sharedMesh);
      if (pieBackground) {
        Destroy(pieBackground);
      }
      if (background) {
        Destroy(background);
      }
      if (tooltip) {
        Destroy(tooltip);
      }
    }

    private void FadeChildren(FadeType fade, bool destroy = false) {
      foreach (ClickMenuIcon child in childMenus) {
        child.destroyOnClose = destroy;
        child.StartFade(fade);
      }
    }

    private void StartFade(FadeType fade) {
      fadeType = fade;
      fadeStartTime = Time.time;
      active = false;
      if (fadeType == FadeType.FadeOutPush || fadeType == FadeType.FadeOutPull) {
        meshCollider.sharedMesh = null;
      } else if (fadeType == FadeType.FadeInPush || fadeType == FadeType.FadeInPull) {
        fadeStartTime = Time.time;
        pushDepth = 0.0f;
        SetTransparency(0.0f);
      }
    }

    private void FinishFadeOut() {
      fadeType = FadeType.NoFade;
      if (destroyOnClose) {
        Destroy(gameObject);
      }
    }

    private void FinishFadeIn() {
      meshCollider.sharedMesh = sharedMesh;
      fadeType = FadeType.NoFade;
      active = true;
    }

    public void CloseAll() {
      foreach (ClickMenuIcon child in childMenus) {
        child.CloseAll();
      }
      if (active) {
        destroyOnClose = true;
        StartFade(FadeType.FadeOutPull);
      } else {
        Destroy(gameObject);
      }
    }

    public ClickMenuIcon DeepestMenu() {
      foreach (ClickMenuIcon child in childMenus) {
        if (child.childMenus.Count > 0) {
          return child.DeepestMenu();
        }
      }
      return this;
    }

    public void OnPointerEnter(PointerEventData eventData) {
      if (!selected) {
        menuRoot.MakeHover(menuItem ? menuItem.id : -1);
      }
      selected = true;
    }

    public void OnPointerExit(PointerEventData eventData) {
      selected = false;
    }
  }
}
