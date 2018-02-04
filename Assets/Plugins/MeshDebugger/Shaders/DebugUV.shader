
Shader "Debug/UV" {
	Properties{
		[Enum(UV,1,UV2,2,UV3,3,UV4,4)]	
		UV_Mode("Selected UV", Int) = 1
	}
		SubShader{
			Pass {
				Fog { Mode Off }
				CGPROGRAM

				#pragma vertex vert
				#pragma fragment frag

		// vertex input: position, UV
		struct appdata {
			float4 vertex : POSITION;
			float4 texcoord : TEXCOORD0;
			float4 texcoord1 : TEXCOORD1;
			float4 texcoord2 : TEXCOORD2;
			float4 texcoord3 : TEXCOORD3;
		};

		struct v2f {
			float4 pos : SV_POSITION;
			float4 uv : TEXCOORD0;
		};

		half UV_Mode;

		v2f vert(appdata v) {
			v2f o;
			o.pos = UnityObjectToClipPos(v.vertex);
			half z = floor(UV_Mode + 0.5h);
			if (z == 1)
				o.uv = float4(v.texcoord.xy, 0, 0);
			if (z == 2)
				o.uv = float4(v.texcoord1.xy, 0, 0);
			if (z == 3)
				o.uv = float4(v.texcoord2.xy, 0, 0);
			if (z == 4)
				o.uv = float4(v.texcoord3.xy, 0, 0);
			return o;
		}

		half4 frag(v2f i) : SV_Target {
			half4 c = frac(i.uv);
			if (any(saturate(i.uv) - i.uv))
				c.b = 0.5;
			return c;
		}
		ENDCG
	}
	}
}