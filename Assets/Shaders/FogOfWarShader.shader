// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/FogOfWarShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            #define POINTS 40 

            sampler2D _MainTex;
            float4 _MainTex_ST;

            
            float4 center;
            float4 points[POINTS];

            int debug;
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float2 fragScreenPos : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.fragScreenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            float signTriangle(float2 p1, float2 p2, float2 p3)
            {
                return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
            }

            bool pointInTriangle(float2 p, float2 c1, float2 c2, float2 c3)
            {
                float d1 = signTriangle(p, c1, c2);
                float d2 = signTriangle(p, c2, c3);
                float d3 = signTriangle(p, c3, c1);

                bool has_neg = (d1 < 0) || (d2 < 0) || (d3 < 0);
                bool has_pos = (d1 > 0) || (d2 > 0) || (d3 > 0);

                return !(has_neg && has_pos);
            }

            // TODO sgliche mit de heightmap nomol versueche mit https://docs.unity3d.com/ScriptReference/Camera.ViewportToWorldPoint.html
            // alli 4 pünkt 0,0 - 1,0 - 0,1 - 1,1 umwandle in world pos und ahand vu dem in koords vude heightmap umwandle

            fixed4 frag (v2f i) : SV_Target
            {

                if(debug == 1)
                {
                    float2 screenRatio = float2(1.0, _ScreenParams.y / _ScreenParams.x);
                    if(_ScreenParams.y > _ScreenParams.x) screenRatio = float2(_ScreenParams.x / _ScreenParams.y, 1.0);

                    float dist = distance(i.fragScreenPos.xy * screenRatio, center.xy * screenRatio);
                    if(dist < 0.005)
                    {
                        return float4(1.0, 0.0, 0.0, 1.0);
                    }

                    for(int j = 0; j < POINTS; j++)
                    {
                        dist = distance(i.fragScreenPos.xy * screenRatio, points[j].xy * screenRatio);
                        if(dist < 0.005)
                        {
                            return float4(1.0, 0.0, 0.0, 1.0);
                        }
                    }
                }

                // TODO optimazation, get points of triangle which would stand if there was no interference, then check if points is inside of that one, if yes, check all triangles
                bool inView = false;
                for(int k = 0; k < POINTS - 1; k++)
                {
                    if(pointInTriangle(i.fragScreenPos.xy, center.xy, points[k].xy, points[k + 1].xy))
                    {
                        inView = true;
                        break;
                    }
                    
                }

                if(inView)
                {
                    fixed4 col = tex2D(_MainTex, i.uv);
                    return col;
                } else
                {
                    float4 col = tex2D(_MainTex, i.uv);
                    return col * 0.25f;
                }

            }
            ENDCG
        }
    }
}
