
Shader "Debug/FrontBack" {
	Properties{
	_FrontCol("Front Color", Color) = (1,.5,0,1)
	_BackCol("Back Color", Color) = (0,.5,1,1)
	}
		SubShader{
			Pass {
				Fog { Mode Off }
				Cull Back
				CGPROGRAM

				#pragma vertex vert
				#pragma fragment frag

				fixed4 _FrontCol;

	// vertex input: position, normal
	struct appdata {
		float4 vertex : POSITION;
		float3 normal : NORMAL;
	};

	struct v2f {
		float4 pos : SV_POSITION;
		float4 color : COLOR;
	};

	v2f vert(appdata v) {
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.color = _FrontCol;
		return o;
	}

	fixed4 frag(v2f i) : SV_Target { return i.color; }
	ENDCG
}
Pass {
	Fog { Mode Off }
	Cull Front
	CGPROGRAM

	#pragma vertex vert
	#pragma fragment frag

	fixed4 _BackCol;

	// vertex input: position, normal
	struct appdata {
		float4 vertex : POSITION;
		float3 normal : NORMAL;
	};

	struct v2f {
		float4 pos : SV_POSITION;
		float4 color : COLOR;
	};

	v2f vert(appdata v) {
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.color = _BackCol;
		return o;
	}

	fixed4 frag(v2f i) : SV_Target { return i.color; }
	ENDCG
}
	}
}