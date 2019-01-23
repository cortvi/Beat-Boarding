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

			// calcular cada cuantos segundos/samples hay un beat,
			// calcular en que segundo/sample se encuentra el pixel (basic lerp)
			// S / (S_between_beats) = rango_0-1 >> abs (rango_1-1) >> dibujar linea (pow or whatever) !
			// same for cursor
			// playing-overlay is taking from AudioSource clip time


			float4 frag (v2f_img IN) : COLOR
			{
				float yH = IN.uv.y * 2 - 1;
				float id = IN.uv.x * viewWidth;
				float4 s = lerp (waveform[floor (id)], waveform[ceil (id)], frac (id));

				float alphaL = (yH < s.x && yH > s.y);
				float alphaR = (yH < s.z && yH > s.w);
				float alpha = saturate(alphaL + alphaR);

				float3 bg = float3(0.65, 0.84, 1.00);
				float3 fg = lerp (bg * 0.5, bg, pow (1 - abs(yH), 2));
				return float4 (lerp (0.15, fg, alpha), 1.0);
			}
			ENDCG
		}
	}
}
