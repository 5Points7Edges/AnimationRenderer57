Shader "finalize13"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader {
		Tags { "RenderType"="Opaque" "Queue"="Geometry"}
		
		Pass {
			BlendOp Add
            Blend SrcAlpha OneMinusSrcAlpha
            
			CGPROGRAM
			
			#pragma vertex vert
			#pragma fragment frag
			
			sampler2D _MainTex;
			
			struct a2v {
				float4 vertex : POSITION;
				float3 texcoord : TEXCOORD0;
			};
			
			struct v2f {
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};
			
			v2f vert(a2v v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				
				o.uv = v.texcoord;
								
				return o;
			}
			
			fixed4 frag(v2f i) : SV_Target {
				fixed4 color =tex2D(_MainTex, i.uv);

				if(color.a == 0) discard;
				//color.a *= 1.06;
				return color;
			}
			
			ENDCG
		}
	} 
 	FallBack Off
}
