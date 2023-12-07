Shader "Custom/BezierTest/Test2"
{
    Properties
    {
        _Color("Main Tint",Color)=(1,1,1,1)

        _start("start",Vector)=(0,0,0,0)
        _control1("control1",Vector)=(0,0,0,0)
        _control2("control2",Vector)=(0,0,0,0)
        _end("end",Vector)=(0,0,0,0)

        _Width("width",Range(0.1,100))=10
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
                float4 screenPos : TEXCOORD1;
            };
            fixed4 _Color;
            float2 _start;
            float2 _control1;
            float2 _control2;
            float2 _end;

            fixed _Width;
            float2 BezierCurve(float t){
                return (1 - t) * (1 - t) * (1 - t) * _start + 3 * t * (1 - t) * (1 - t) * _control1 + 3 * t * t * (1 - t) * _control2 + t * t * t * _end;
            }
            float PointToSegDist(float x, float y, float x1, float y1, float x2, float y2)
            {
	            float cross = (x2 - x1) * (x - x1) + (y2 - y1) * (y - y1);
	            if (cross <= 0) return sqrt((x - x1) * (x - x1) + (y - y1) * (y - y1));
	  
	            float d2 = (x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1);
	            if (cross >= d2) return sqrt((x - x2) * (x - x2) + (y - y2) * (y - y2));
	  
	            float r = cross / d2;
	            float px = x1 + (x2 - x1) * r;
	            float py = y1 + (y2 - y1) * r;
	            return sqrt((x - px) * (x - px) + (y - py) * (y - py));
            }
            v2f vert (appdata_full v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv=v.texcoord.xy;
                o.screenPos = ComputeScreenPos (o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                //float scrPos=i.pos.xy;

                float2 screenPos = i.screenPos.xy/i.screenPos.w;
                screenPos.xy *= _ScreenParams.xy;


                float dist=10000;
                float segements=30;

                for(int i=0;i<=segements;i++){
                    float2 P1=BezierCurve((i-1)/segements);
                    float2 P2=BezierCurve(i/segements);
                    dist=min(dist,PointToSegDist(screenPos.x,screenPos.y,P1.x,P1.y,P2.x,P2.y));
                }

                clip(_Width-dist);

                float4 finalColor=_Color;
                return finalColor;
            }
            ENDCG
        }
    }
}
