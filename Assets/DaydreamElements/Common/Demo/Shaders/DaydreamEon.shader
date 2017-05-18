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

Shader "DaydreamElements/Demo/Daydream Eon"
{
	Properties
	{
		_BGColor ("Background Color", Color) = (1,1,1,1)
		_HighlightColor ("Highlight Color", Color) = (0.113,0.914,0.714,1)
		_Color ("Shadow Color", Color) = (0.161,0.475,1,1)
		_Alpha ("Alpha", Float) = 1
		_ScaleFactor ("Color Scale", Range(-1,1)) = 1
		_GradientDirection ("Gradient Direction", Range(-1,1)) = 1
	}
	SubShader
	{
		Tags { "Queue"="Transparent"}
		LOD 100

		//Z pre-pass
		Pass
		{
			Blend Off
			Cull Back
			ColorMask 0
			Zwrite On
			ZTest LEqual
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
			};
			
			v2f vert (appdata v)
			{
				v2f o;

				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}
			
			half4 frag (v2f i) : SV_Target
			{
				return 0;
			}
			
			ENDCG
		}

		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
      #pragma multi_compile_fog	
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				half4 color : COLOR;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				half4 color : COLOR;
			};

      half3 _BGColor;
			half3 _HighlightColor;
			half3 _Color;
			half _Alpha;
			half _ScaleFactor;
			half _GradientDirection;
			
			v2f vert (appdata v)
			{
				v2f o;

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.color = v.color;

				float3 objectOrigin = float3(unity_ObjectToWorld[0][3],
																unity_ObjectToWorld[1][3],
																unity_ObjectToWorld[2][3]);
				float3 worldPosition = mul(unity_ObjectToWorld, v.vertex).xyz;
        float3 dstToCenter = worldPosition - objectOrigin;
        dstToCenter *= _ScaleFactor;
 	      half maskHighlightColor = saturate((1-dstToCenter.x*_GradientDirection)*0.5 + dstToCenter.y);  
 	      half maskMainColor = 1-maskHighlightColor;

        o.color.rgb = (maskHighlightColor * _HighlightColor + maskMainColor * _Color)*o.color.a + _BGColor*(1-o.color.a);
				o.color.a *= _Alpha;
				o.color.a += o.color.a;

				UNITY_TRANSFER_FOG(o,o.vertex);

				return o;
			}

			half4 frag (v2f i) : SV_Target
			{
				half4 col = i.color;
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			
			ENDCG
		}
	}
}
