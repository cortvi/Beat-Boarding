using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using BeatBoarding.Data;

namespace BeatBoarding.Tools 
{
	[CustomEditor (typeof (Map))]
	public class MapEditor : Editor
	{
		#region Vars
		private Map map;

		public Material waveformMat;
		private ComputeBuffer samplesBuffer;
		private bool rebuildBuffer;

		private float viewPos;
		private float viewSize;
		private float viewWidth;

		private const int WaveformHeigth = 150;
		private const float MinViewSize = 0.04f;
		private const float MaxViewSize = 1.00f;
		#endregion

		public override void OnInspectorGUI () 
		{
			EditorGUI.BeginChangeCheck ();

			// Basic map info
			var song = Extension.ObjectField (map.song, "Song");
			uint bpm = (uint) EditorGUILayout.DelayedIntField ("Song BPM", map.bpm);

			if (map.song != null)
			{
				#region Waveform visualizer
				var rect = GUILayoutUtility.GetRect (0, WaveformHeigth, GUILayout.ExpandWidth (true));
				if (rect.width > 1 && rect.width != viewWidth)
				{
					waveformMat.SetFloat ("viewWidth", rect.size.x);
					viewWidth = rect.width;
					rebuildBuffer = true;
				}
				Graphics.DrawTexture (rect, Texture2D.whiteTexture, waveformMat);
				GUI.DrawTexture (rect, Texture2D.whiteTexture, 0, false, 0, Color.black, 3, 0); 
				#endregion

				#region Scroll view
				float scroll = GUILayout.HorizontalScrollbar (viewPos, viewSize, 0f, 1f);
				if (viewPos != scroll)
				{
					viewPos = scroll;
					rebuildBuffer = true;
				}

				if (Event.current.type == EventType.ScrollWheel
				&&	rect.Contains (Event.current.mousePosition))
				{
					// Allow smoothing the zoom
					float mul = Event.current.keyCode == KeyCode.LeftAlt ? 
						0.001f : .01f;

					viewSize += Event.current.delta.y * mul;
					viewSize = Mathf.Clamp (viewSize, MinViewSize, MaxViewSize);
					rebuildBuffer = true;
				}

				// Small indicator of how many seconds I'm seeing
				EditorGUILayout.LabelField ("Distance: " + viewSize * map.song.length + " seconds.", EditorStyles.miniLabel);
				#endregion
			}

			//TO-DO: Beat overlay
			float beatTime = 1f / (bpm / 60f);
			float viewTime = viewSize * map.song.length;
			int viewBeatsAmount = Mathf.CeilToInt (viewTime * (bpm / 60f));

			for (int b = 0; b != viewBeatsAmount; ++b)
			{

			}

			#region Track changes
			if (EditorGUI.EndChangeCheck ())
			{
				if (song != map.song)
				{
					if (song != null) rebuildBuffer = true;
					else samplesBuffer?.Release ();
					map.song = song;
				}

				if (bpm != map.bpm)
				{
					map.bpm = (int) bpm;
					// Re-make beat-overlay???
				}
			}

			if (rebuildBuffer)
			{
				BuildBuffer();
				rebuildBuffer = false;
			}
			#endregion
		}

		private void Awake () 
		{
			// Initialize stuff
			map = target as Map;
			viewSize = MaxViewSize;
		}

		private void OnDestroy () 
		{
			// Release resources
			samplesBuffer?.Release ();
		}

		#region Helpers
		private void BuildBuffer () 
		{
			// Ensure a song is selected & avoid rebulding when repainting
			if (map.song == null || Event.current.type == EventType.Repaint)
				return;

			// Ensure we start on a L-channel sample
			int offset = (int)(map.song.samples * viewPos);
			if (offset % 2 != 0) offset++;

			// Compute view limits, in sample amounts
			int samplesAmount = (int) (map.song.samples * viewSize);
			int samplesXpixel = (int) ((samplesAmount * 2) / viewWidth);

			// Get actual audio data
			float[] samples = new float[samplesAmount * 2];
			map.song.GetData (samples, offset);

			// Read all samples in the view 
			var waveform = new Vector4[(int) viewWidth];
			unchecked
			{
				for (int i = 0; i != waveform.Length; ++i)
				{
					// Combine all samples insiede each pixel column
					int start = i * samplesXpixel;
					int end = (i + 1) * samplesXpixel;

					
					// Save both positive & negative values
					float pL = 0f, nL = 0f, pR = 0f, nR = 0f;
					for (int s = start; s < end; s += 2)
					{
						if (s + 1 > samples.Length) break;

						float sL = samples[s];
						if (sL > 0f) pL = (pL > sL ? pL : sL);
						else		 nL = (nL < sL ? nL : sL);

						float sR = samples[s + 1];
						if (sR > 0f) pR = (pR > sL ? pR : sR);
						else		 nR = (nR < sL ? nR : sR);
					}

					var a = () =>
					{
						v.x = pL;

					};

					// Write L & R channels separately
					waveform[i].x = pL; waveform[i].z = pR;
					waveform[i].y = nL; waveform[i].w = nR;
				}
			}
			// Create actual buffer
			if (samplesBuffer == null || !samplesBuffer.IsValid ()
			||	samplesBuffer.count != waveform.Length)
			{
				samplesBuffer?.Dispose ();
				samplesBuffer = new ComputeBuffer (waveform.Length, 4 * sizeof (float));
			}
			// Feed the shader
			samplesBuffer.SetData (waveform);
			waveformMat.SetBuffer ("waveform", samplesBuffer);
		}
		#endregion
	}
}
