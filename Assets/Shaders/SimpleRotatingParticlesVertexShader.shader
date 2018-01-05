Shader "Unlit/SimpleRotatingParticlesVertexShader"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_Tint("Tint", Color) = (1, 1, 1, .2)
		_Radius("Radius", float) = 5
		_Speed("Speed", float) = 10
	}
		SubShader
	{
		Tags{ "RenderType" = "Opaque" }
		LOD 100

		Pass
	{
		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
		// make fog work
#pragma multi_compile_fog

#include "UnityCG.cginc"
	float4 _Tint;
	float _Radius;
	float _Speed;
	struct appdata
	{
		float4 vertex : POSITION;
		float3 localPos: TEXCOORD0;
		float4 color: COLOR;
	};

	struct v2f
	{
		float3 localPos : TEXCOORD0;
		float4 vertex : SV_POSITION;
		float4 color: COLOR;
	};

	sampler2D _MainTex;
	float4 _MainTex_ST;

	v2f vert(appdata v)
	{
		v2f o;
		o.localPos = v.localPos;
		o.vertex.x = v.vertex.x + sin(_Time.y * 3.141592) / 10;
		o.vertex.y = v.vertex.y;
		o.vertex.z = v.vertex.z + cos(_Time.y * 3.141592) / 10;
		o.vertex.a = v.vertex.a;

		o.vertex = UnityObjectToClipPos(o.vertex);
		o.color = v.color;
		return o;
	}

	fixed4 frag(v2f i) : SV_Target
	{

	return i.color;
	}
		ENDCG
	}
	}
}
