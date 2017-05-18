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

Shader "DaydreamElements/Demo/ShadowRender"
{
  Properties
  {
  }
  SubShader
  {
    Tags { "RenderType"="Opaque" }
    LOD 100

    Pass
    {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #include "UnityCG.cginc"

      struct appdata
      {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
      };

      struct v2f
      {
        float2 uv : TEXCOORD0;
        float3 shadowPosition : TEXCOORD3;
        float4 vertex : SV_POSITION;
      };

      /// Full Shadow Projection Matrix
      float4x4 _ShadowMatrix;

      // Shadow Camera Position / Rotation Matrix
      float4x4 _ShadowCameraMatrix;

      // Scalar values
      float4 _ShadowData;

      // Bias to adjust shadow depth
      float _ShadowBias;

      v2f vert (appdata v)
      {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        float4 worldPosition = mul(unity_ObjectToWorld, v.vertex);
        o.uv = v.uv;
        float4 shadowPosition = mul(_ShadowMatrix, worldPosition);

        float2 shadowTexturePosition = .5 + .5*shadowPosition.xy/abs(shadowPosition.w);
        float shadowDepth = -_ShadowData.y * abs(_ShadowBias + mul(_ShadowCameraMatrix, worldPosition).z)/_ShadowData.x;
        o.shadowPosition = float3( shadowTexturePosition.x, shadowTexturePosition.y , shadowDepth);

        return o;
      }

      sampler2D _ShadowTexture;
      fixed4 frag (v2f i) : SV_Target
      {
        /// Read the shadow texture
        half4 compressedShadow = tex2D(_ShadowTexture, i.shadowPosition.xy  );

        /// Convert the 4*8 bytes into 32 bit float, and scale to the correct exponential size
        float shadowDepth = _ShadowData.z*DecodeFloatRGBA(compressedShadow);

        /// Convert this pixel's shadow depth to exponential form
        float depth = exp(i.shadowPosition.z);

        half2 shadowClipping = max(0,10*(abs(2 * i.shadowPosition.xy - 1) -0.9));
        half shadowExtent = max(shadowClipping.x,shadowClipping.y);

        /// Perform the exponential comparison by multiplying, and then adjust smoothstep to alter falloff
        half shadow = shadowExtent +saturate(smoothstep(.8,1,depth*shadowDepth));

        return shadow;
      }
      ENDCG
    }
  }
}
