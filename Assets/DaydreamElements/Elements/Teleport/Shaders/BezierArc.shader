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

// Shader draws a bezier line that bends smoothly with control point.
Shader "DaydreamElements/Teleport/BezierArc" {
  Properties {
    _Color ("Color", Color) = (1, 1, 1, 1)
    _MainTex ("Texture", 2D) = "white" {}
    _StartPosition ("Start", Vector) = (0, 0, 0, 0)
    _EndPosition ("End", Vector) = (0, 0, 0, 0)
    _ControlPosition ("Control", Vector) = (0, 0, 0, 0)
    _LineWidth ("Line Width", float) = .01
    _DistanceScale ("Distance Scale", float) = .005
  }

  SubShader {
    Tags { "RenderType"="Transparent" "IgnoreProjector"="True" "Queue"="Transparent" }
    LOD 100
    Blend SrcAlpha OneMinusSrcAlpha
    Cull Off

    Pass {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag

      #include "UnityCG.cginc"

      sampler2D _MainTex;
      float4 _Color;
      float4 _StartPosition;
      float4 _EndPosition;
      float4 _ControlPosition;
      float _LineWidth;
      float _DistanceScale;

      struct appdata {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
      };

      struct v2f {
        float2 uv : TEXCOORD0;
        float4 vertex : SV_POSITION;
      };

      // Calculate a quadratic bezier curve point for a given time (0-1).
      float BezierValueForTime(
        float start,
        float end,
        float control,
        float t) {
      return (pow(1 - t, 2) * start)
        + (2 * (1 - t) * t * control)
        + (pow(t, 2) * end);
      }

      // Move the verticies to make a smooth bezier curve.
      v2f vert (appdata v) {
        v2f o;
        UNITY_INITIALIZE_OUTPUT(v2f, o); // d3d11 requires initialization

        // The input mesh uses z values 0-1 to indicate it's position along the curve.
        float percent = v.vertex.z;

        float4 adjustedVertex = v.vertex;

        // Calcuate new x, y, z values given the start/end/control positions and line width.
        adjustedVertex.x = BezierValueForTime(_StartPosition.x, _EndPosition.x, _ControlPosition.x, percent);
        adjustedVertex.y = BezierValueForTime(_StartPosition.y, _EndPosition.y, _ControlPosition.y, percent);
        adjustedVertex.z = BezierValueForTime(_StartPosition.z, _EndPosition.z, _ControlPosition.z, percent);

        // Width is scaled the farther you are from the start position.
        float vertexToStart = distance(adjustedVertex, _StartPosition);
        float width = _LineWidth + (vertexToStart * _DistanceScale);

        // Vertex x is always -1 or 1 in mesh so multiplity to flip sides for thickness offset.
        adjustedVertex.x += (v.vertex.x * width);

        o.vertex = UnityObjectToClipPos(adjustedVertex);
        o.uv = v.uv;

        return o;
      }

      fixed4 frag (v2f i) : SV_Target {
        // The image is horizontal, so we need to flip our x/y UVs.
        float2 reversedUV = (i.uv.y, i.uv.x);
        return tex2D(_MainTex, reversedUV) * _Color;
      }

      ENDCG
    }
  }
}
