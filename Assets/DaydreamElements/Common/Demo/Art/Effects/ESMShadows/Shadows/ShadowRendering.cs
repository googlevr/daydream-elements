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
using UnityEngine.Rendering;

public class ShadowRendering : MonoBehaviour {

  public float shadowExponent;
  public float shadowDistance;
  public int baseBlurIterations;
  public int dynamicBlurIterations;

  public Mesh quad;

  public Camera camera;
  public Shader depthShader;

  public RenderTexture shadowTexture;

  public RenderTexture shadowTextureA;
  public RenderTexture shadowTextureB;

  public RenderTexture shadowTextureDisplay;

  public Material blurMaterial;
  public Material combineBlurMaterial;
  public Material blitMat;

  const string shadowCameraMatrixName = "_ShadowCameraMatrix";
  const string shadowMatrixName = "_ShadowMatrix";
  const string shadowTextureName = "_ShadowTexture";
  const string shadowDataName = "_ShadowData";

  int shadowCameraMatrixId;
  int shadowMatrixId;
  int shadowTextureId;
  int shadowDataId;

  int renderRes = 1024;
  int texRes = 512;
  Vector4 shadowData = new Vector4();
  CommandBuffer initMips;
  CommandBuffer mips;

  bool initialized = false;

  void Start(){

    shadowTexture = new RenderTexture(renderRes,renderRes,16, RenderTextureFormat.ARGB32);
    shadowTextureA = new RenderTexture(texRes,texRes,0, RenderTextureFormat.ARGB32);
    shadowTextureB = new RenderTexture(texRes,texRes,0, RenderTextureFormat.ARGB32);

    shadowTextureDisplay = new RenderTexture(texRes,texRes,0, RenderTextureFormat.ARGB32);

    shadowTextureDisplay.useMipMap = true;
    shadowTextureDisplay.autoGenerateMips = false;
    shadowTextureDisplay.Create();

    shadowTexture.Create();
    shadowTextureA.Create();
    shadowTextureB.Create();

    shadowTextureDisplay.DiscardContents();
    shadowTexture.DiscardContents();
    shadowTextureA.DiscardContents();
    shadowTextureB.DiscardContents();

    camera.SetReplacementShader(depthShader,"RenderType");

    shadowCameraMatrixId = Shader.PropertyToID(shadowCameraMatrixName);
    shadowMatrixId = Shader.PropertyToID(shadowMatrixName);
    shadowTextureId = Shader.PropertyToID(shadowTextureName);
    shadowDataId = Shader.PropertyToID(shadowDataName);

    UpdateCameraParams();

    initialized = false;
  }

  void OnDisable(){
    shadowTexture.Release();
    shadowTextureA.Release();
    shadowTextureB.Release();
    shadowTextureDisplay.Release();
  }

  Color almostWhite = new Color(0.99f,0.99f,0.99f,0.99f);
  void Update(){

    if(!initialized){
      if(shadowTexture.IsCreated() &&
        shadowTextureA.IsCreated() &&
        shadowTextureB.IsCreated() &&
        shadowTextureDisplay.IsCreated() ) {

        RenderTexture current = RenderTexture.active;

        /// We cannot clear to white because the decode cannot hand
        RenderTexture.active = shadowTexture;
        GL.Clear(true, true, almostWhite, 1);

        RenderTexture.active = shadowTextureA;
        GL.Clear(true, false, almostWhite);

        RenderTexture.active = shadowTextureB;
        GL.Clear(true, false, almostWhite);

        RenderTexture.active = shadowTextureDisplay;
        GL.Clear(true, false, almostWhite);

        RenderTexture.active = current;

        Shader.SetGlobalTexture(shadowTextureId, shadowTextureDisplay);
        ClearMips();
        GenerateMips();
        Graphics.ExecuteCommandBuffer(initMips);

        camera.targetTexture = shadowTexture;
        camera.enabled = true;
        initialized = true;
      }
    }
    else{
      UpdateCameraParams();
    }
  }

  void OnPostRender(){
    BlurLastRender();
  }

  void UpdateCameraParams(){
    shadowData.x = shadowDistance;
    shadowData.y = shadowExponent;
    shadowData.z = Mathf.Exp(shadowExponent);
    shadowData.w = Mathf.Exp(-shadowExponent);
    Shader.SetGlobalVector(shadowDataId,shadowData);
    Shader.SetGlobalMatrix(shadowCameraMatrixId,camera.worldToCameraMatrix);
    Shader.SetGlobalMatrix(shadowMatrixId,camera.projectionMatrix * camera.worldToCameraMatrix);
  }

  void BlurLastRender(){
    shadowTextureA.DiscardContents();
    shadowTextureB.DiscardContents();
    shadowTextureDisplay.DiscardContents();

    Graphics.Blit(shadowTexture, shadowTextureA, blurMaterial,0);

    RenderTexture current = shadowTextureA;
    RenderTexture target = shadowTextureB;

    for(int i=0; i<dynamicBlurIterations; i++){
      Graphics.Blit(current, target, blurMaterial,0);
      RenderTexture tmp = current;
      current = target;
      target = tmp;
    }

    Graphics.Blit(current, shadowTextureDisplay, blurMaterial,0);

    blitMat.mainTexture = current;
    Graphics.ExecuteCommandBuffer(mips);
  }

  void GenerateMips(){
    mips = new CommandBuffer();

    Matrix4x4 mat = Matrix4x4.identity;

    //128
    mips.SetRenderTarget(shadowTextureDisplay, 1);
    mips.DrawMesh(quad, mat, blitMat,0, 0);

    //64
    mips.SetRenderTarget(shadowTextureDisplay, 2);
    mips.DrawMesh(quad, mat, blitMat,0, 0);

    //32
    mips.SetRenderTarget(shadowTextureDisplay, 3);
    mips.DrawMesh(quad, mat, blitMat,0, 0);

    //16
    mips.SetRenderTarget(shadowTextureDisplay, 4);
    mips.DrawMesh(quad, mat, blitMat,0, 0);
  }


  void ClearMips(){
    initMips = new CommandBuffer();

    Matrix4x4 mat = Matrix4x4.identity;

    //128
    initMips.SetRenderTarget(shadowTextureDisplay, 1);
    initMips.ClearRenderTarget(false, true, Color.white);

    initMips.SetRenderTarget(shadowTextureDisplay, 2);
    initMips.ClearRenderTarget(false, true, Color.white);

    initMips.SetRenderTarget(shadowTextureDisplay, 3);
    initMips.ClearRenderTarget(false, true, Color.white);

    initMips.SetRenderTarget(shadowTextureDisplay, 4);
    initMips.ClearRenderTarget(false, true, Color.white);

    initMips.SetRenderTarget(shadowTextureDisplay, 5);
    initMips.ClearRenderTarget(false, true, Color.white);

    initMips.SetRenderTarget(shadowTextureDisplay, 6);
    initMips.ClearRenderTarget(false, true, Color.white);

    initMips.SetRenderTarget(shadowTextureDisplay, 7);
    initMips.ClearRenderTarget(false, true, Color.white);

    initMips.SetRenderTarget(shadowTextureDisplay, 8);
    initMips.ClearRenderTarget(false, true, Color.white);
  }
}
