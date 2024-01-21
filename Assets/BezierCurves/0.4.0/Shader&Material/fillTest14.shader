Shader "Custom/BezierTest/fillTest14"
{
    Properties
    {
        _Color("Main Tint",Color)=(1,1,1,1)
        _smoothEdge("smoothEdge",Range(0.1,100))=1
        _approximationStep("approximationStep",Range(1,100))=20
        _enable("enable",Int)=1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        LOD 100
        
        GrabPass
        {
            "_BackgroundTexture"
        }

        Pass
        {
            Tags{"LightMode"="ForwardBase"}
            Cull Off

            ZWrite Off
            
            //BlendOp Add, Add
            //Blend SrcAlpha OneMinusSrcAlpha, OneMinusDstAlpha One
            //Blend Off
            BlendOp Add,Add
            Blend SrcAlpha One,One One
            HLSLPROGRAM

            #pragma target 5.0
            #include "UnityCG.cginc"

            #pragma vertex vert
            #pragma fragment frag


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
            float dTime;
            fixed _smoothEdge;
            float _approximationStep;
            int _enable;
            struct curveData
            {
                float3 start;
                float3 control1;
                float3 control2;
                float3 end;
                int basePointIndex;
            };
            StructuredBuffer<curveData> curvess_buffer;
            StructuredBuffer<curveData> curvest_buffer;

            StructuredBuffer<float3> verticestarget;

            float2 BezierCurve(float t,float2 start, float2 control1, float2 control2, float2 end){
                return (1 - t) * (1 - t) * (1 - t) * start + 3 * t * (1 - t) * (1 - t) * control1 + 3 * t * t * (1 - t) * control2 + t * t * t * end;
            }
            float3 rateFunction_Linear(float3 start, float3 end, float val)
            {
                return (end - start) * val + start;
            }
            float4 rateFunction_Linear(float4 start, float4 end, float val)
            {
                return (end - start) * val + start;
            }
            float2 rateFunction_Linear(float2 start, float2 end, float val)
            {
                return (end - start) * val + start;
            }
            int GetDirection(float3 p1, float3 p2, float3 p3)
            {
                float3 edge1 = p2 - p1;
                float3 edge2 = p3 - p2;

                float3 crossProduct = cross(edge1,edge2);
                // decide which side the triangle points at
                return crossProduct.z > 0 ? 1 : -1;
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
                float time=(-_CosTime.w+1.0)/2;
                
                float4 posS = UnityObjectToClipPos(v.pos);
                float3 worldPosS = mul(unity_ObjectToWorld, v.pos).xyz;

                float4 posT = UnityObjectToClipPos(verticestarget[v.id]);
                float3 worldPosT = mul(unity_ObjectToWorld, verticestarget[v.id]).xyz;
                
                o.pos=rateFunction_Linear(posS,posT,time);
                o.worldPos=rateFunction_Linear(worldPosS,worldPosT,time);
                int curveIndex=v.id/9;
                curveData buffer=curvess_buffer[curveIndex];
                float3 startS=mul(unity_ObjectToWorld, curvess_buffer[curveIndex].start).xyz;
                float3 c1S=mul(unity_ObjectToWorld, curvess_buffer[curveIndex].control1).xyz;
                float3 c2S=mul(unity_ObjectToWorld, curvess_buffer[curveIndex].control2).xyz;
                float3 endS=mul(unity_ObjectToWorld, curvess_buffer[curveIndex].end).xyz;

                float3 startT=mul(unity_ObjectToWorld, curvest_buffer[curveIndex].start).xyz;
                float3 c1T=mul(unity_ObjectToWorld, curvest_buffer[curveIndex].control1).xyz;
                float3 c2T=mul(unity_ObjectToWorld, curvest_buffer[curveIndex].control2).xyz;
                float3 endT=mul(unity_ObjectToWorld, curvest_buffer[curveIndex].end).xyz;

                float3 basePointS=mul(unity_ObjectToWorld, curvess_buffer[buffer.basePointIndex].start).xyz;
                float3 basePointT=mul(unity_ObjectToWorld, curvest_buffer[buffer.basePointIndex].start).xyz;
                float3 basePoint= rateFunction_Linear(basePointS,basePointT,time);
                
                o.start=rateFunction_Linear(startS,startT,time);
                o.C1=rateFunction_Linear(c1S,c1T,time);
                o.C2=rateFunction_Linear(c2S,c2T,time);
                o.end=rateFunction_Linear(endS,endT,time);
                
                int curveIndex0To8=v.id%9;
                
                if (curveIndex0To8>=6)
                {
                    o.fillAll=1;
                    o.orientation=GetDirection(basePoint,o.start,o.end);
                }
                else if (curveIndex0To8>=3 && curveIndex0To8<=5)
                {
                    o.fillAll=0;
                    o.orientation=GetDirection(o.start,o.C2,o.end);
                }
                else
                {
                    o.fillAll=0;
                    o.orientation=GetDirection(o.start,o.C1,o.C2);
                }
                return o;
            }
          

            fixed4 frag (v2f input) : SV_Target
            {
                
                float4 finalColor=_Color;
                float4 a=finalColor.a*0.95;
                a=1;
                if(input.orientation<0)
                {
                   a = -a / (1-a);
                    a=-1;
                }
                finalColor.a=a;
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
                        //dist=min(dist,PointToSegDist(pos.x,pos.y,P1.x,P1.y,P2.x,P2.y));
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
                        //dist=min(dist,PointToSegDist(pos.x,pos.y,P1.x,P1.y,P2.x,P2.y));
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
