Shader "Hidden/Map Waveform"
{
	Properties
	{
		_BarColor ("Waveform color", Color) = (0.65, 0.84, 1.00, 1.00)
		_PlaybackColor ("Playback overlay color", Color) = 	(0.47, 1.00, 0.55, 1.00)
	}

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
			// ———
			float3 _BarColor;
			float3 _PlaybackColor;

			uniform StructuredBuffer<float4> waveform;
			uniform float barAmount;

			uniform float startTime;
			uniform float endTime;

			uniform float playbackStart;
			uniform float playbackPos;
			// ———

			float WaveformAlpha (float position, float height)
			{
				float bar = position * barAmount;
				float4 s = lerp (waveform[floor (bar)], waveform[ceil (bar)], frac (bar));

				float alphaL = (height < s.x && height > s.y);
				float alphaR = (height < s.z && height > s.w);
				return saturate (alphaL + alphaR);
			}

			float PlaybackOverlay (float position) 
			{
				float time = lerp (startTime, endTime, position);
				float alpha = smoothstep (playbackStart, playbackPos, time);
				alpha = time > playbackPos ? 0 : alpha;
				return lerp (0, 0.6, pow (alpha, 0.3));
			}

			float4 frag (v2f_img IN) : COLOR
			{
				// Waveform color
				float height = IN.uv.y * 2 - 1;
				float3 barColor = pow (_BarColor, 0.4545454545);  // linear -> gamma
				barColor = lerp (barColor * 0.5, barColor, pow (1 - abs(height), 2));

				// Playback overlay
				float3 overlayColor = pow (_PlaybackColor, 0.4545454545);  // linear -> gamma

				// Add all layers
				float3 frag;
				frag = lerp (0.15, barColor, WaveformAlpha (IN.uv.x, height));
				frag = lerp (frag, overlayColor, PlaybackOverlay (IN.uv.x));

				return float4 (frag, 1.0);
			}
			ENDCG
		}
	}
}
