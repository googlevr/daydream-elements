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

Shader "DaydreamElements/Teleport/Unlit Vertex Color Transparent"
{
  Properties {
  }
  SubShader
  {
    Tags { "RenderType"="Transparent" "Queue"="Transparent" }
    LOD 100
    Blend SrcAlpha OneMinusSrcAlpha

    Pass
    {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #include "UnityCG.cginc"

      struct appdata
      {
        float4 vertex : POSITION;
        half4 color : COLOR;
      };

      struct v2f
      {
        float4 vertex : SV_POSITION;
        half4 color : COLOR;
      };

      v2f vert (appdata v)
      {
        v2f o;

        o.vertex = UnityObjectToClipPos(v.vertex);
        o.color = v.color;

        return o;
      }

      half4 frag (v2f i) : SV_Target
      {
        half4 col = i.color;
        return col;
      }
      ENDCG
    }
  }
}
