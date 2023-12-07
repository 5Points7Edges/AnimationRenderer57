Shader "Custom/BezierTest/lineDraw"
{
    Properties
    {
        _Color("Main Tint",Color)=(1,1,1,1)
        _start("startX",Color)=(0,0,0,0)
        _control1("startX",Color)=(0,0,0,0)
    }
    SubShader

    {

        Pass{

            Tags {"LightMode" = "ForwardBase" }

            CGPROGRAM

            #pragma vertex vert

            #pragma fragment frag

            #include "Lighting.cginc"

            fixed4 _Color;

            struct a2v {

                float4 vertex : POSITION;

                //float3 normal : NORMAL;

            };

            struct v2f {

                float4 pos : SV_POSITION;
                float3 worldPos: TEXCOORD1;

            };

            v2f vert(a2v v)

            {

                v2f o;

                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos=mul(unity_ObjectToWorld,v.vertex);
                return o;
                    
            }

            fixed4 frag(v2f i) : SV_Target

            {
                float2 uv=i.pos.xy/i.pos.w;
                float lineWidth=0.2;

                //float threshold = step(lineWidth, abs(y - uv.y));

                fixed3 color=fixed3(uv.xy,0);
                return fixed4(color,1.0);


            }

            ENDCG

        }

    }
    FallBack "DIFFUSE"
}
