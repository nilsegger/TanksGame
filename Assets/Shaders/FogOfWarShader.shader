Shader "Unlit/FogOfWarShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "black" {}
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

            #define POINTS 20 

            uniform sampler2D _MainTex;
            uniform float4 _MainTex_ST;
            
            uniform fixed _FOGFadeDistance;

            uniform fixed4 center;
            uniform fixed4 points[POINTS];

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

            fixed signTriangle(fixed2 p1, fixed2 p2, fixed2 p3)
            {
                return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
            }

            bool pointInTriangle(fixed2 p, fixed2 c1, fixed2 c2, fixed2 c3)
            {
                fixed d1 = signTriangle(p, c1, c2);
                fixed d2 = signTriangle(p, c2, c3);
                fixed d3 = signTriangle(p, c3, c1);

                bool has_neg = (d1 < 0) || (d2 < 0) || (d3 < 0);
                bool has_pos = (d1 > 0) || (d2 > 0) || (d3 > 0);

                return !(has_neg && has_pos);
            }

            // https://stackoverflow.com/a/6853926
            fixed distanceToLineSegmentSquared(fixed2 p, fixed2 start, fixed2 end)
            {
                fixed A = p.x - start.x;
                fixed B = p.y - start.y;
                fixed C = end.x - start.x;
                fixed D = end.y - start.y;

                fixed d = A * C + B * D;
                fixed len_eq = C * C + D * D;
                fixed param = -1;
                if(len_eq != 0) {
                    param = d / len_eq;
                }

                fixed xx, yy;
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

                fixed dx = p.x - xx;
                fixed dy = p.y - yy;
                return dx * dx + dy * dy;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                if(debug == 1)
                {
                    fixed2 screenRatio = fixed2(1.0, _ScreenParams.y / _ScreenParams.x);
                    if(_ScreenParams.y > _ScreenParams.x) screenRatio = fixed2(_ScreenParams.x / _ScreenParams.y, 1.0);

                    fixed dist = distance(i.fragScreenPos.xy * screenRatio, center.xy * screenRatio);
                    if(dist < 0.005)
                    {
                        return fixed4(1.0, 0.0, 0.0, 1.0);
                    }

                    for(int j = 0; j < POINTS; j++)
                    {
                        dist = distance(i.fragScreenPos.xy * screenRatio, points[j].xy * screenRatio);
                        if(dist < 0.005)
                        {
                            return fixed4(1.0, 0.0, 0.0, 1.0);
                        }
                    }
                }

                fixed closestDistance = distanceToLineSegmentSquared(i.fragScreenPos, center.xy, points[0].xy);
                fixed lastToCenterDist = distanceToLineSegmentSquared(i.fragScreenPos, points[POINTS - 1].xy, center.xy);
                if(lastToCenterDist < closestDistance) closestDistance = lastToCenterDist;

                bool inView = false;

                fixed fadeDistance = _FOGFadeDistance / 1000.0f;
                if(i.fragScreenPos.x < points[0].x - fadeDistance || i.fragScreenPos.x > points[POINTS - 1].x + fadeDistance)
                {
                        closestDistance = fadeDistance * fadeDistance;
                } else
                {
                    for(int k = 0; k < POINTS - 1; k++)
                    {
                        if(pointInTriangle(i.fragScreenPos.xy, center.xy, points[k].xy, points[k + 1].xy))
                        {
                            inView = true;
                            break;
                        }

                        fixed dist = distanceToLineSegmentSquared(i.fragScreenPos, points[k].xy, points[k + 1].xy);
                        if(dist < closestDistance)
                        {
                            closestDistance = dist;
                        }
                    
                    }
                }

                fixed4 col = tex2D(_MainTex, i.uv);
                if(inView) return col;

                if(closestDistance <= fadeDistance * fadeDistance)
                {
                    fixed fadeWeight = 1.0f - 1.0f / (fadeDistance * fadeDistance) * closestDistance;
                    return lerp(0, col, fadeWeight);    
                }

                if(debug == 1) return col;
                return 0; 

            }
            ENDCG
        }
    }
}
