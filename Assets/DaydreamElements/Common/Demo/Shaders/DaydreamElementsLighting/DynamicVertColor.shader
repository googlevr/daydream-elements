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

Shader "DaydreamElements/Demo/Dynamic Vertex Color" {
  Properties 
  {
     _Color ("Grayscale Tint", Color) = (1,1,1,1)
  }
  SubShader {
    Tags { "Queue"="Geometry" "RenderType"="Geometry"}

    Pass {
      Cull Off
      ZWrite On
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      // Unity fog
      // #pragma multi_compile_fog
      // Custom fog
      #include "DaydreamElementsLighting.cginc"
      #include "UnityCG.cginc"

      struct appdata {
        float4 vertex : POSITION;
        half4 color : COLOR;
      };

      struct v2f {
        float4 vertex : SV_POSITION;
        //UNITY_FOG_COORDS(1)
        half4 color : COLOR;
      };

      // Properties set by GlobalDynamicWindColor
      half _GlobalEffectScale;
      half _GlobalColorEffectScale;

      half _WindSpeed;
      half _WindMagnitude;
      half _WindTurbulence;

      half _WindDirectionX;
      half _WindDirectionZ;

      half _RadialEffectorInfluence;
      
      half _SaturationMin;

      half4 _EffectorA;
      half4 _EffectorB;
      half4 _EffectorC;

      // Properties set by DynamicWindColorEffector
      half _PerObjectEffectScale;

      half3 _Color;

      // Set by shader
      half saturation;

      half effectorAMask;
      half effectorBMask;
      half effectorCMask;

      v2f vert(appdata v) {
        v2f o;

        o.vertex = UnityObjectToClipPos(v.vertex);

        // Object effect scale limited by global effect scale
        _PerObjectEffectScale *= _GlobalEffectScale;
        
        // Get vertex world position
        float4 worldPos = mul(unity_ObjectToWorld,v.vertex);
        // Calculations for custom fog
        float3 direction = _WorldSpaceCameraPos - worldPos.xyz;
        float distance = length(direction);
        float3 viewDir = direction / distance;
        half4 fogVal = simplefog(worldPos, -viewDir, distance);

        /// Animate vertex color  

        // Unpack vertex color channels 
        half3 col = v.color.rgb;
        half grayscale = v.color.a;

        // Calculate influence from radial effectors
        half dstToEffectorA = length(_EffectorA.xyz - worldPos.xyz);
        half effectorAScale = _EffectorA.w;
        effectorAMask = 1 - (dstToEffectorA / _EffectorA.w);
        effectorAMask *= effectorAScale;
        effectorAMask = saturate(effectorAMask);

        half dstToEffectorB = length(_EffectorB.xyz - worldPos.xyz);
        half effectorBScale = _EffectorB.w;
        effectorBMask = 1 - (dstToEffectorB / _EffectorB.w);
        effectorBMask *= effectorBScale;
        effectorBMask = saturate(effectorBMask);

        half dstToEffectorC = length(_EffectorC.xyz - worldPos.xyz);
        half effectorCScale = _EffectorC.w;
        effectorCMask = 1 - (dstToEffectorC / _EffectorC.w);
        effectorCMask *= effectorCScale;
        effectorCMask = saturate(effectorCMask);

        // Update saturation value
        saturation = _SaturationMin;
        saturation += _PerObjectEffectScale;
        saturation += (effectorAMask + effectorBMask + effectorCMask) * _RadialEffectorInfluence;
        saturation = clamp(saturation,0,0.25)*4;
        saturation *= _GlobalColorEffectScale;

        float t = _Time.y;

        // Create world space gusts
        float gustMaskX = sin(t * 0.5 * _WindSpeed + worldPos.x * 0.1);
        gustMaskX = (gustMaskX + 1) * 0.5; 
        _WindDirectionX *= gustMaskX;

        float gustMaskZ = sin(t * 0.5 * _WindSpeed + worldPos.z * 0.1);
        gustMaskZ = (gustMaskZ + 1) * 0.5; 
        _WindDirectionZ *= gustMaskZ; 

        // Animate hue shift 
        col.r += sin(t) * gustMaskX * gustMaskZ * 0.05;
        col.g += cos(t) * gustMaskX * gustMaskZ * 0.05;

        // Tint grayscale
        half3 tint = _Color;

        // Get final albedo 
        col = col * saturation + (grayscale + tint) * (1-saturation);
        // Apply custom fog
        col = fogVal.a * fogVal.rgb + (1-fogVal.a) * col;

        o.color = half4(col,fogVal.a); 

        // UNITY_TRANSFER_FOG(o,o.vertex);

        return o;      
      }

      half4 frag(v2f i) : SV_TARGET { 
        half4 col = i.color;
        // UNITY_APPLY_FOG(i.fogCoord, col);
        return col;      
      }      
      ENDCG    
    }
  }
}
