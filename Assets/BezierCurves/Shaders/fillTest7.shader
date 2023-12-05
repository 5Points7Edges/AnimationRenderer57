Shader "Custom/BezierTest/fillTest7"
{
    Properties
    {
        _Color("Main Tint",Color)=(1,1,1,1)
        _smoothEdge("smoothEdge",Range(0.1,100))=1
        _approximationStep("approximationStep",Range(1,100))=20
        
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        LOD 100


        Pass
        {
            Tags{"LightMode"="ForwardBase"}
            Cull Off

            ZWrite Off
            BlendOp Add , Add
            Blend SrcAlpha OneMinusSrcAlpha , OneMinusDstAlpha One

            HLSLPROGRAM

            #pragma target 5.0
            #include "UnityCG.cginc"

            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 pos : POSITION;
                uint id : SV_VERTEXID;
            };

            struct v2f
            {
                //float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;

                float3 start : TEXCOORD1;
                float3 C1 : TEXCOORD2;
                float3 C2 : TEXCOORD3;
                float3 end : TEXCOORD4;
                int orientation: TEXCOORD5;
                int fillAll : TEXCOORD6;

            };
            
            fixed4 _Color;

            fixed _smoothEdge;
            float _approximationStep;

            struct curveData
            {
                float4 start;
                float4 control1;
                float4 control2;
                float4 end;
                int orientation;

            };
            StructuredBuffer<curveData> curves;

            float2 BezierCurve(float t,float2 start, float2 control1, float2 control2, float2 end){
                return (1 - t) * (1 - t) * (1 - t) * start + 3 * t * (1 - t) * (1 - t) * control1 + 3 * t * t * (1 - t) * control2 + t * t * t * end;
            }
            bool LeftOfLine(float2 p1,float2 p2,float2 c){
                float tmp=(p1.x-c.x)*(p2.y-c.y)-(p1.y-c.y)*(p2.x-c.x);
                if(tmp<0)return true;
                return false;
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

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.pos);
                o.worldPos = mul(unity_ObjectToWorld, v.pos).xyz;

                int curveIndex=v.id/9;

                o.start=mul(unity_ObjectToWorld, curves[curveIndex].start).xyz;
                o.C1=mul(unity_ObjectToWorld, curves[curveIndex].control1).xyz;
                o.C2=mul(unity_ObjectToWorld, curves[curveIndex].control2).xyz;
                o.end=mul(unity_ObjectToWorld, curves[curveIndex].end).xyz;
                
                o.orientation=curves[curveIndex].orientation;
                o.fillAll=0;
                curveIndex=v.id%9;
                if (curveIndex==6 || curveIndex==7 ||curveIndex==8)
                {
                    o.fillAll=1;
                }
                return o;
            }
          

            fixed4 frag (v2f input) : SV_Target
            {
                float4 finalColor=_Color;
                finalColor.a*=0.95;
                if(input.orientation<0)
                {
                    finalColor.a = -finalColor.a / (1 - finalColor.a);
                }
                //return finalColor;
                if (input.fillAll==1)return finalColor;
                
                float2 start=input.start.xy;
                float2 control1=input.C1.xy;
                float2 control2=input.C2.xy;
                float2 end=input.end.xy;

                float2 pos= input.worldPos.xy;
                
                float dist=10000;
                int ifOutput=0;
                if(input.orientation>0)
                {
                    ifOutput=1;
                    for(int i=0;i<=_approximationStep;i++){
                        float2 P1=BezierCurve((i-1)/_approximationStep,start,control1,control2,end);
                        float2 P2=BezierCurve(i/_approximationStep,start,control1,control2,end);
                        dist=min(dist,PointToSegDist(pos.x,pos.y,P1.x,P1.y,P2.x,P2.y));
                        if(LeftOfLine(P1,P2,pos) ){
                            ifOutput=-1;
                        }
                    }
                }
                if(input.orientation<0)
                {
                    ifOutput=-1;
                    for(int i=0;i<=_approximationStep;i++){
                        float2 P1=BezierCurve((i-1)/_approximationStep,start,control1,control2,end);
                        float2 P2=BezierCurve(i/_approximationStep,start,control1,control2,end);
                        dist=min(dist,PointToSegDist(pos.x,pos.y,P1.x,P1.y,P2.x,P2.y));
                        if(!LeftOfLine(P1,P2,pos) ){
                            ifOutput=1;
                        }
                    }
                }
                
                
                if(input.orientation<0){
                    ifOutput*=-1;
                }
                
                if(ifOutput<0)discard;
                return finalColor;
            }
            ENDHLSL
        }

    }
    //FallBack "Diffuse"
    Fallback "Transparent/VertexLit"
}
