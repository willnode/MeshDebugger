
Shader "Debug/Tangents" {
	Properties{
		[Enum(Normal,1,Tangent,2,Bitangent,3,WorldNormal,4,WorldTangent,5,WorldBitangent,6)]	
		Tan_Mode("Visualization Mode", Int) = 1
	}
	SubShader{
		Pass {
			Fog { Mode Off }
			CGPROGRAM
			#pragma exclude_renderers gles
			#pragma vertex vert
			#pragma fragment frag

			half Tan_Mode;

			// vertex input: position, tangent
			struct appdata {
				float4 vertex : POSITION;
				float4 normal : NORMAL;
				float4 tangent : TANGENT;
			};

			struct v2f {
				float4 pos : SV_POSITION;
				fixed4 color : COLOR;
			};

			v2f vert(appdata v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				half z = floor(Tan_Mode + 0.5h);
				if (z == 1)
				o.color.xyz = v.normal.xyz * 0.5 + 0.5;
				if (z == 2)
				o.color.xyz = v.tangent.xyz * 0.5 + 0.5;
				if (z == 3) {
					float3 binormal = cross(v.normal, v.tangent.xyz) * v.tangent.w;
					o.color.xyz = binormal * 0.5 + 0.5;
				}
				if (z == 4)
				o.color.xyz = mul((float3x3)unity_ObjectToWorld, v.normal.xyz) * 0.5 + 0.5;
				if (z == 5)
				o.color.xyz = mul((float3x3)unity_ObjectToWorld, v.tangent.xyz) * 0.5 + 0.5;
				if (z == 6) {
					float3 binormal = cross(v.normal.xyz, v.tangent.xyz) * v.tangent.w;
					o.color.xyz = binormal * 0.5 + 0.5;
				}

				o.color.w = 1;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target { return i.color; }
			ENDCG
		}
	}
}