// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/FogOfWarShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _FOGFadeDistance("FOG Fade Distance", float) = 50
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

            uniform sampler2D _MainTex;
            uniform float4 _MainTex_ST;
            
            uniform sampler2D _FOGNoise;
            uniform float4 _FOGNoise_ST;
            uniform float _FOGFadeDistance;

            uniform sampler2D heightmap;
            
            uniform float4 center;
            uniform float4 points[POINTS];

            uniform int debug;
            
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

            // https://stackoverflow.com/a/6853926
            float distanceToLineSegment(float2 p, float2 start, float2 end)
            {
                float A = p.x - start.x;
                float B = p.y - start.y;
                float C = end.x - start.x;
                float D = end.y - start.y;

                float d = A * C + B * D;
                float len_eq = C * C + D * D;
                float param = -1;
                if(len_eq != 0) {
                    param = d / len_eq;
                }

                float xx, yy;
                if(param < 0)
                {
                    xx = start.x;
                    yy = start.y;
                } else if(param > 1)
                {
                    xx = end.x;
                    yy = end.y;
                } else
                {
                    xx = start.x + param * C;
                    yy = start.y + param * D;
                }

                float dx = p.x - xx;
                float dy = p.y - yy;
                return sqrt(dx * dx + dy * dy);
            }

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

                /* TODO optimazation, get points of triangle which would stand if there was no interference, then check if points is inside of that one, if yes, check all triangles */
                bool inView = false;
                float closestDistance = distanceToLineSegment(i.fragScreenPos, center.xy, points[0].xy);
                float lastToCenterDist = distanceToLineSegment(i.fragScreenPos, points[POINTS - 1].xy, center.xy);
                if(lastToCenterDist < closestDistance) closestDistance = lastToCenterDist;
                
                for(int k = 0; k < POINTS - 1; k++)
                {
                    if(pointInTriangle(i.fragScreenPos.xy, center.xy, points[k].xy, points[k + 1].xy))
                    {
                        inView = true;
                        break;
                    }

                    float dist = distanceToLineSegment(i.fragScreenPos, points[k].xy, points[k + 1].xy);
                    if(dist < closestDistance)
                    {
                        closestDistance = dist;
                    }
                    
                }

                if(inView)
                {
                    fixed4 col = tex2D(_MainTex, i.uv);
                    return col;
                }
                
                float4 col = tex2D(_MainTex, i.uv);
                float fadeDistance = _FOGFadeDistance / 1000.0f;
                float fadeWeight = 1.0f - 1.0f / fadeDistance * closestDistance;

                float4 heightmapValue = 1.0f - tex2D(heightmap, i.uv);
                float4 fog_color = lerp(0.0, col, heightmapValue);
                
                if(closestDistance <= fadeDistance)
                {
                    return lerp(fog_color, col, fadeWeight);    
                }

                if(debug) return col;
                return fog_color; 

            }
            ENDCG
        }
    }
}
