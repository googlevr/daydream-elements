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

Shader "DaydreamElements/Demo/Vertex Color Diffuse AO"
{
	Properties
	{
		_MainTex ("Main Texture (A)", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Transparent"}
		LOD 100

		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite On
			ZTest LEqual
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// Unity fog
			// #pragma multi_compile_fog	
			// Custom fog
			#include "DaydreamElementsLighting.cginc"
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				half4 color : COLOR;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				// UNITY_FOG_COORDS(1)
				half4 color : COLOR;
			};

			sampler2D _MainTex;
			half4 _MainTex_ST;

			
			v2f vert (appdata v)
			{
				v2f o;

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.color = v.color;

        // Calculations for custom fog
			  half4 worldPos = (mul(unity_ObjectToWorld, v.vertex));
				float3 direction = _WorldSpaceCameraPos - worldPos.xyz;
			  float distance = length(direction);
			  float3 viewDir = direction / distance;
			  half4 fogVal = simplefog(worldPos, -viewDir, distance);
        
        half3 col = o.color.rgb;

        // Apply custom fog
			  col = fogVal.a * fogVal.rgb + (1-fogVal.a) * col;

        o.color = half4(col,1);

				// UNITY_TRANSFER_FOG(o,o.vertex);

				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
			  half alpha = tex2D(_MainTex, i.uv).a;
			  half4 col = alpha * i.color;
			  col = half4(col.rgb, i.color.a);
				// UNITY_APPLY_FOG(i.fogCoord, col);
			  return col;
			}
			ENDCG
		}
	}
}
