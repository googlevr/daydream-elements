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

Shader "DaydreamElements/Teleport/Teleport Glow" {
  Properties {
    _MainTex ("Main Texture", 2D) = "white" {}
    _Color ("Color", Color) = (1, 1, 1, 1)
    _ColorFrequency ("Color Frequency", float) = 1
    _ColorAmplitude ("Color Amplitude", float) = 1
    _AlphaSpeed ("Alpha Speed", float) = 1
  }

  SubShader {
    Tags { "RenderType"="Transparent" "IgnoreProjector"="True" "Queue"="Transparent" }
    LOD 100
    Blend SrcAlpha OneMinusSrcAlpha
    ZWrite off
    Cull off

    Pass {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #include "UnityCG.cginc"

      sampler2D _MainTex;
      float4 _Color;
      float _ColorFrequency;
      float _ColorAmplitude;
      float _AlphaSpeed;

      struct appdata
      {
        float4 vertex : POSITION;
        float4 uv : TEXCOORD0;
      };

      struct v2f
      {
        float4 vertex : SV_POSITION;
        float4 uv : TEXCOORD0;
      };

      v2f vert (appdata v)
      {
        v2f o;
        UNITY_INITIALIZE_OUTPUT(v2f, o);

        o.vertex = UnityObjectToClipPos(v.vertex);
        o.uv = v.uv;

        return o;
      }

      half4 frag (v2f i) : SV_Target
      {
        // Color waves and bends the higher up the y axis.
        float2 colorUVs = i.uv;
        colorUVs.x += sin(_Time.y * _ColorFrequency) * (i.uv.y * _ColorAmplitude);
        float4 baseColor = tex2D(_MainTex, colorUVs);

        // Alpha scrolls across.
        float2 alphaUVs = i.uv;
        alphaUVs.x += _Time.y * _AlphaSpeed;
        float4 alphaColor = tex2D(_MainTex, alphaUVs);

        // Take the color and replace with scrolled alpha value.
        float4 processedColor = baseColor;
        processedColor.a = alphaColor.a;

        // Tint with color value.
        return processedColor * _Color;
      }
      ENDCG
    }
  }
}
