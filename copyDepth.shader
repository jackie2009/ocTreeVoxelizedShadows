Shader "Unlit/copyDepth"
{

	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag


			#include "UnityCG.cginc"
			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float zdis : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

		  v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.zdis = -mul(UNITY_MATRIX_MV, v.vertex).z;
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				return float4(i.zdis,0,0,1);
			}
			ENDCG
		}
	}
}