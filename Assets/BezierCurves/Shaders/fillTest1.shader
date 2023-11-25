Shader "Custom/BezierTest/fillTest1"
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
                float4 screenPos : TEXCOORD1;
            };
            
            fixed4 _Color;
            float2 _start;
            float2 _control1;
            float2 _control2;
            float2 _end;

            fixed _Width;
            fixed _smoothEdge;
            float _approximationStep;
            fixed _fillAll;
            int _Orientation;


            //StructuredBuffer<float3> controlPoints;

            float2 BezierCurve(float t){
                return (1 - t) * (1 - t) * (1 - t) * _start + 3 * t * (1 - t) * (1 - t) * _control1 + 3 * t * t * (1 - t) * _control2 + t * t * t * _end;
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
                o.screenPos = ComputeScreenPos (o.pos);
                return o;
            }
          

            fixed4 frag (v2f i) : SV_Target
            {
                //return _Color;
                float4 finalColor=_Color;
                if (_fillAll)return finalColor;
                float2 screenPos = i.screenPos.xy/i.screenPos.w;
                screenPos.xy *= _ScreenParams.xy;

                float dist=10000;
                for(int i=0;i<=_approximationStep;i++){
                    float2 P1=BezierCurve((i-1)/_approximationStep);
                    float2 P2=BezierCurve(i/_approximationStep);
                    dist=min(dist,PointToSegDist(screenPos.x,screenPos.y,P1.x,P1.y,P2.x,P2.y));
                    
                    if(LeftOfLine(P1,P2,screenPos.xy) ){
                        
                        if(_Orientation){
                            return finalColor;
                        }
                        return float4(finalColor.rgb,smoothstep(-_smoothEdge,_smoothEdge,-dist));
                    }
                }
                
                if(_Orientation){
                    return float4(finalColor.rgb,smoothstep(-_smoothEdge,_smoothEdge,-dist));
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
                float4 screenPos : TEXCOORD1;
            };
            
            fixed4 _Color;
            float2 _start;
            float2 _control1;
            float2 _control2;
            float2 _end;

            fixed _Width;
            fixed _smoothEdge;
            float _approximationStep;
            fixed _fillAll;
            int _Orientation;


            //StructuredBuffer<float3> controlPoints;

            float2 BezierCurve(float t){
                return (1 - t) * (1 - t) * (1 - t) * _start + 3 * t * (1 - t) * (1 - t) * _control1 + 3 * t * t * (1 - t) * _control2 + t * t * t * _end;
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
                o.screenPos = ComputeScreenPos (o.pos);
                return o;
            }
          

            fixed4 frag (v2f i) : SV_Target
            {
                //return _Color;
                float4 finalColor=_Color;
                if (_fillAll)return finalColor;
                float2 screenPos = i.screenPos.xy/i.screenPos.w;
                screenPos.xy *= _ScreenParams.xy;

                float dist=10000;
                for(int i=0;i<=_approximationStep;i++){
                    float2 P1=BezierCurve((i-1)/_approximationStep);
                    float2 P2=BezierCurve(i/_approximationStep);
                    dist=min(dist,PointToSegDist(screenPos.x,screenPos.y,P1.x,P1.y,P2.x,P2.y));
                    
                    if(!LeftOfLine(P1,P2,screenPos.xy) ){
                        if(_Orientation){
                            return finalColor;
                        }
                        return float4(finalColor.rgb,smoothstep(-_smoothEdge,_smoothEdge,-dist));
                    }
                }
                
                if(_Orientation){
                    return  float4(finalColor.rgb,smoothstep(-_smoothEdge,_smoothEdge,-dist));
                }
                return finalColor;
            }
            ENDHLSL
        }
    }
    //FallBack "Diffuse"
    Fallback "Transparent/VertexLit"
}
