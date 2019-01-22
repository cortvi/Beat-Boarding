Shader "Hidden/MapWaveform"
{
	Properties { }

		SubShader
	{
		Lighting Off
		Blend One Zero

		Pass
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma require compute

			uniform StructuredBuffer<float4> waveform;
			uniform float viewWidth;

			float4 frag (v2f_img IN) : COLOR
			{
				float yH = IN.uv.y * 2 - 1;
				float id = floor (IN.uv.x * viewWidth);
				float4 s = waveform[id];

				float alpha = (yH < s.x && yH > s.y) || (yH < s.z && yH > s.w);

				float3 bg = float3(0.65, 0.84, 1.00);
				float3 fg = lerp (bg * 0.5, bg, pow (1 - abs(yH), 2));
				return float4 (lerp (0.15, fg, alpha), 1.0);
			}
			ENDCG
		}
	}
}
