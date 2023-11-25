Shader "Custom/BezierTest/fillTest3"
{
    Properties
    {
        _Color("Main Tint",Color)=(1,1,1,1)
        _smoothEdge("smoothEdge",Range(0.1,100))=1
        _approximationStep("approximationStep",Range(1,100))=20
        _Orientation("orientation",Int)=0

        _start("start",Vector)=(0,0,0,0)
        _control1("control1",Vector)=(0,0,0,0)
        _control2("control2",Vector)=(0,0,0,0)
        _end("end",Vector)=(0,0,0,0)

        _fillAll("fillAll",Range(0,1))=0
        
    }
    SubShader
    {
        //Tags { "RenderType"="Opaque" }
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        LOD 100


        Pass
        {
            Tags{"LightMode"="ForwardBase"}
            Cull Front

            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

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
                //float2 uv : TEXCOORD0;
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
            
            fixed4 _Color;

            float4 _start;
            float4 _control1;
            float4 _control2;
            float4 _end;

            fixed _Width;
            fixed _smoothEdge;
            float _approximationStep;
            fixed _fillAll;
            int _Orientation;


            //StructuredBuffer<float3> controlPoints;

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


                o.start=mul(unity_ObjectToWorld, _start).xyz;
                o.C1=mul(unity_ObjectToWorld, _control1).xyz;
                o.C2=mul(unity_ObjectToWorld, _control2).xyz;
                o.end=mul(unity_ObjectToWorld, _end).xyz;

                
                return o;
            }
          

            fixed4 frag (v2f i) : SV_Target
            {
                float4 finalColor=_Color;
                if (_fillAll)return finalColor;
                
                float2 start=i.start.xy;
                float2 control1=i.C1.xy;
                float2 control2=i.C2.xy;
                float2 end=i.end.xy;

                float2 pos= i.worldPos.xy;
                //float2 pos = i.screenPos.xy/i.screenPos.w;
                //pos.xy *= _ScreenParams.xy;
                float dist=10000;
                for(int i=0;i<=_approximationStep;i++){
                    float2 P1=BezierCurve((i-1)/_approximationStep,start,control1,control2,end);
                    float2 P2=BezierCurve(i/_approximationStep,start,control1,control2,end);
                    dist=min(dist,PointToSegDist(pos.x,pos.y,P1.x,P1.y,P2.x,P2.y));
                    
                    if(LeftOfLine(P1,P2,pos) ){
                        
                        if(_Orientation){
                            return finalColor;
                        }
                        return float4(finalColor.rgb,0);
                    }
                }
                
                if(_Orientation){
                    return float4(finalColor.rgb,0);
                }
                return finalColor;
            }
            ENDHLSL
        }
        Pass
        {
            Tags{"LightMode"="ForwardBase"}
            Cull Back

            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

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
                //float2 uv : TEXCOORD0;
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
            
            fixed4 _Color;

            float4 _start;
            float4 _control1;
            float4 _control2;
            float4 _end;

            fixed _Width;
            fixed _smoothEdge;
            float _approximationStep;
            fixed _fillAll;
            int _Orientation;


            //StructuredBuffer<float3> controlPoints;

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


                o.start=mul(unity_ObjectToWorld, _start).xyz;
                o.C1=mul(unity_ObjectToWorld, _control1).xyz;
                o.C2=mul(unity_ObjectToWorld, _control2).xyz;
                o.end=mul(unity_ObjectToWorld, _end).xyz;

                
                return o;
            }
          

            fixed4 frag (v2f i) : SV_Target
            {
                float4 finalColor=_Color;
                if (_fillAll)return finalColor;
                
                float2 start=i.start.xy;
                float2 control1=i.C1.xy;
                float2 control2=i.C2.xy;
                float2 end=i.end.xy;

                float2 pos= i.worldPos.xy;
                //float2 pos = i.screenPos.xy/i.screenPos.w;
                //pos.xy *= _ScreenParams.xy;
                float dist=10000;
                for(int i=0;i<=_approximationStep;i++){
                    float2 P1=BezierCurve((i-1)/_approximationStep,start,control1,control2,end);
                    float2 P2=BezierCurve(i/_approximationStep,start,control1,control2,end);
                    dist=min(dist,PointToSegDist(pos.x,pos.y,P1.x,P1.y,P2.x,P2.y));
                    
                    if(!LeftOfLine(P1,P2,pos) ){
                        
                        if(_Orientation){
                            return finalColor;
                        }
                        return float4(finalColor.rgb,0);
                    }
                }
                
                if(_Orientation){
                    return float4(finalColor.rgb,0);
                }
                return finalColor;
            }
            ENDHLSL
        }
    }
    //FallBack "Diffuse"
    Fallback "Transparent/VertexLit"
}
