Shader "Custom/BezierTest/strokeTest15"
{
    Properties
    {
        _Color("Main Tint",Color)=(1,1,1,1)
        _approximationStep("approximationStep",Range(1,100))=20
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Cull off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

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
            };
            struct curveData
            {
                float3 start;
                float3 control1;
                float3 control2;
                float3 end;
                int basePointIndex;
            };
            float2 BezierCurve(float t,float2 start, float2 control1, float2 control2, float2 end){
                return (1 - t) * (1 - t) * (1 - t) * start + 3 * t * (1 - t) * (1 - t) * control1 + 3 * t * t * (1 - t) * control2 + t * t * t * end;
            }
            float BezierCurve(float t,float start, float control1, float control2, float end){
                return (1 - t) * (1 - t) * (1 - t) * start + 3 * t * (1 - t) * (1 - t) * control1 + 3 * t * t * (1 - t) * control2 + t * t * t * end;
            }
            float BezierCurveDiff(float t,float p1, float p2, float p3, float p4){
                return -3 * (p1 - 3*p2 + 3*p3 - p4) * t * t + 6 * (p1 - 2 * p2 + p3) * t - 3*(p1-p2);
            }
            float BezierCurveDiffDiff(float t,float p1, float p2, float p3, float p4){
                return -6 * (p1 - 3*p2+3*p3 - p4) * t + 6 * ( p1 - 2 * p2 + p3);
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
            
            float distanceToCurveAtPoint(float2 pos,float t,float2 p1,float2 p2,float2 p3,float2 p4)
            {
                float Dx=(BezierCurve(t,p1.x,p2.x,p3.x,p4.x)-pos.x);
                float Dy=(BezierCurve(t,p1.y,p2.y,p3.y,p4.y)-pos.y);
                return sqrt(Dx*Dx + Dy*Dy);
            }
            float f(float2 pos,float t,float2 p1,float2 p2,float2 p3,float2 p4)
            {
                float qx = BezierCurve(t, p1.x, p2.x, p3.x, p4.x);
                float q1x = BezierCurveDiff(t, p1.x, p2.x, p3.x, p4.x);

                float qy = BezierCurve(t, p1.y, p2.y, p3.y, p4.y);
                float q1y = BezierCurveDiff(t, p1.y, p2.y, p3.y, p4.y);

                return (qx-pos.x)*q1x + (qy-pos.y)*q1y;
            }
            float fDiff(float2 pos,float t,float2 p1,float2 p2,float2 p3,float2 p4)
            {
                float qx = BezierCurve(t, p1.x, p2.x, p3.x, p4.x);
                float q1x = BezierCurveDiff(t, p1.x, p2.x, p3.x, p4.x);
                float q2x = BezierCurveDiffDiff(t, p1.x, p2.x, p3.x, p4.x);
                
                float qy = BezierCurve(t, p1.y, p2.y, p3.y, p4.y);
                float q1y = BezierCurveDiff(t, p1.y, p2.y, p3.y, p4.y);
                float q2y = BezierCurveDiffDiff(t, p1.y, p2.y, p3.y, p4.y);

                return q1x*q1x+(qx-pos.x)*q2x+q1y*q1y+(qy-pos.y)*q2y;
            }
            float NewtonIteration(float startT,float2 pos,float2 p1,float2 p2,float2 p3,float2 p4)
            {
                for(int i=0;i<3;i++)
                {
                    if(startT>1||startT<0)return startT;
                    startT = startT - f(pos,startT,p1,p2,p3,p4) / fDiff(pos,startT,p1,p2,p3,p4);
                }
                return startT;
            }

            float shortestDistance(float2 pos,float2 p1,float2 p2,float2 p3,float2 p4)
            {
                float dis=1000;
                float numberOfStartX=4;
                for(int i=0;i<=numberOfStartX;i++)
                {
                    float solution=NewtonIteration(i/numberOfStartX,pos,p1,p2,p3,p4);
                    if(solution>1||solution<0)continue;
                    dis=min(dis,distanceToCurveAtPoint(pos,solution,p1,p2,p3,p4));
                }

                dis=min(dis,distance(pos,BezierCurve(0,p1,p2,p3,p4)));
                dis=min(dis,distance(pos,BezierCurve(1,p1,p2,p3,p4)));
                return dis;
            }
            StructuredBuffer<curveData> curvess_buffer;
            StructuredBuffer<curveData> curvest_buffer;
            StructuredBuffer<float3> StrokeVerticesTarget;

            
            fixed4 _Color;
            float _approximationStep;
            
            v2f vert (appdata v)
            {
                v2f o;
                float time=(-_CosTime.w+1.0)/2;
                
                
                float4 posS = UnityObjectToClipPos(v.pos);
                float3 worldPosS = mul(unity_ObjectToWorld, v.pos).xyz;

                float4 posT = UnityObjectToClipPos(StrokeVerticesTarget[v.id]);
                float3 worldPosT = mul(unity_ObjectToWorld, StrokeVerticesTarget[v.id]).xyz;
                
                o.pos=rateFunction_Linear(posS,posT,time);
                o.worldPos=rateFunction_Linear(worldPosS,worldPosT,time);
                
                int curveIndex=v.id/36;
                curveData buffer=curvess_buffer[curveIndex];
                float3 startS=mul(unity_ObjectToWorld, curvess_buffer[curveIndex].start).xyz;
                float3 c1S=mul(unity_ObjectToWorld, curvess_buffer[curveIndex].control1).xyz;
                float3 c2S=mul(unity_ObjectToWorld, curvess_buffer[curveIndex].control2).xyz;
                float3 endS=mul(unity_ObjectToWorld, curvess_buffer[curveIndex].end).xyz;

                float3 startT=mul(unity_ObjectToWorld, curvest_buffer[curveIndex].start).xyz;
                float3 c1T=mul(unity_ObjectToWorld, curvest_buffer[curveIndex].control1).xyz;
                float3 c2T=mul(unity_ObjectToWorld, curvest_buffer[curveIndex].control2).xyz;
                float3 endT=mul(unity_ObjectToWorld, curvest_buffer[curveIndex].end).xyz;                
                
                o.start=rateFunction_Linear(startS,startT,time);
                o.C1=rateFunction_Linear(c1S,c1T,time);
                o.C2=rateFunction_Linear(c2S,c2T,time);
                o.end=rateFunction_Linear(endS,endT,time);
                
                return o;
            }
          

            fixed4 frag (v2f input) : SV_Target
            {
                
                float4 finalColor=_Color;
                // return finalColor;                
                float2 start=input.start.xy;
                float2 control1=input.C1.xy;
                float2 control2=input.C2.xy;
                float2 end=input.end.xy;

                float2 pos= input.worldPos.xy;

                float dist=shortestDistance(pos,start,control1,control2,end);
                if (dist>0.1)discard;
                return finalColor;
            }
            
            ENDHLSL
        }
    }
}
