// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

// Simplified VertexLit shader, optimized for high-poly meshes. Differences from regular VertexLit one:
// - less per-vertex work compared with Mobile-VertexLit
// - supports only DIRECTIONAL lights and ambient term, saves some vertex processing power
// - no per-material color
// - no specular
// - no emission

Shader "Custom/MobileTanksLit (Only Directional Lights)" {
    Properties {
       _Color ("Color", Color) = (1,1,1,1) 
       // _MainTex ("Base (RGB)", 2D) = "white" {}
    }
    SubShader {
        Blend SrcAlpha OneMinusSrcAlpha
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        LOD 80

    Pass {
        Name "FORWARD"
        Tags { "LightMode" = "ForwardBase" }
CGPROGRAM
#pragma vertex vert_surf
#pragma fragment frag_surf alpha 
#pragma target 2.0
#pragma multi_compile_fwdbase
#pragma multi_compile_fog
#include "HLSLSupport.cginc"
#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "AutoLight.cginc"

        uniform float4 detecting_sources_position[10];
        uniform int detecting_sources_count;
        uniform float fade_distance;
        uniform float visibility_range;

        inline float3 LightingLambertVS (float3 normal, float3 lightDir)
        {
            fixed diff = max (0, dot (normal, lightDir));
            return _LightColor0.rgb * diff;
        }

        UNITY_INSTANCING_BUFFER_START(Props)
           UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
        UNITY_INSTANCING_BUFFER_END(Props)

        struct v2f_surf {
  float4 pos : SV_POSITION;
  float2 pack0 : TEXCOORD0;
  #ifndef LIGHTMAP_ON
  fixed3 normal : TEXCOORD1;
  #endif
  #ifdef LIGHTMAP_ON
  float2 lmap : TEXCOORD2;
  #endif
  #ifndef LIGHTMAP_ON
  fixed3 vlight : TEXCOORD2;
  #endif
  LIGHTING_COORDS(3,4)
  UNITY_FOG_COORDS(5)
  UNITY_VERTEX_OUTPUT_STEREO
float fade : TEXCOORD6;
};
float4 _MainTex_ST;
v2f_surf vert_surf (appdata_full v)
{
    v2f_surf o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.pos = UnityObjectToClipPos(v.vertex);
    o.pack0.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
    #ifdef LIGHTMAP_ON
    o.lmap.xy = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
    #endif
    float3 worldN = UnityObjectToWorldNormal(v.normal);
    #ifndef LIGHTMAP_ON
    o.normal = worldN;
    #endif
    #ifndef LIGHTMAP_ON

    o.vlight = ShadeSH9 (float4(worldN,1.0));
    o.vlight += LightingLambertVS (worldN, _WorldSpaceLightPos0.xyz);

    #endif
    TRANSFER_VERTEX_TO_FRAGMENT(o);
    UNITY_TRANSFER_FOG(o,o.pos);

    
    float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
    o.fade = 0.0;

    // assuming were still in editor
    if(detecting_sources_count == 0)
    {
        o.fade = 1.0;
    } else
    {
        for(int i = 0; i < detecting_sources_count; i++)
        {
            const float3 forward = detecting_sources_position[i] - worldPos;
            const float d = forward.x * forward.x + forward.z * forward.z;
            float i_fade = 0.f;
            if(d <= visibility_range * visibility_range)
            {
                i_fade = 1.0;
            } else if(d <= visibility_range * visibility_range + fade_distance * fade_distance)
            {
                i_fade = 1.0 - clamp(1.0f / (fade_distance * fade_distance) * (d - visibility_range * visibility_range), 0.0f, 1.0f);
            }
            o.fade = max(o.fade, i_fade);
        }
    }
    
    return o;
}
fixed4 frag_surf (v2f_surf IN) : SV_Target
{
    SurfaceOutput o;
    o.Albedo = _Color.rgb;
    o.Alpha = _Color.a;
    o.Emission = 0.0;
    o.Specular = 0.0;
    o.Gloss = 0.0;
    
    #ifndef LIGHTMAP_ON
    o.Normal = IN.normal;
    #else
    o.Normal = 0;
    #endif
    
    fixed atten = LIGHT_ATTENUATION(IN);
    fixed4 c = 0;
    #ifndef LIGHTMAP_ON
    c.rgb = o.Albedo * IN.vlight * atten;
    #endif
    #ifdef LIGHTMAP_ON
    fixed3 lm = DecodeLightmap (UNITY_SAMPLE_TEX2D(unity_Lightmap, IN.lmap.xy));
    #ifdef SHADOWS_SCREEN
    c.rgb += o.Albedo * min(lm, atten*2);
    #else
    c.rgb += o.Albedo * lm;
    #endif
    c.a = o.Alpha;
    #endif

    UNITY_APPLY_FOG(IN.fogCoord, c);
    
    c.a = IN.fade;
    return c;
}

ENDCG
    }
}

FallBack "Mobile/VertexLit"
}
