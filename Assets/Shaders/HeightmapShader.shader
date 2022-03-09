Shader "HeightmapShader" {
    Properties
    {
        _MainTex ("Texture", 2D) = "black" {}
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        Pass {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            uniform sampler2D _MainTex;
            uniform float4 _MainTex_ST;
            sampler2D _CameraDepthNormalsTexture;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex: SV_POSITION;
            };

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);;
                return o;
            }

            half4 frag(v2f i) : SV_Target {
                float4 NormalDepth;
                DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, i.uv), NormalDepth.w, NormalDepth.xyz);

                if(NormalDepth.w < 0.9) return tex2D(_MainTex, i.uv);
                return float4(NormalDepth.w, NormalDepth.w, NormalDepth.w, 1.0);
            }
            ENDCG
        }
    }
}