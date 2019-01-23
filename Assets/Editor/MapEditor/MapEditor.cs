using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using BeatBoarding.Data;
using System;

namespace BeatBoarding.Tools 
{
	[CustomEditor (typeof (Map))]
	public class MapEditor : Editor
	{
		#region Vars
		private Map map;

		private AudioSource speaker;

		public Material waveformMat;
		private ComputeBuffer samplesBuffer;
		private bool rebuildBuffer;

		private float viewPos;
		private float viewSize;
		private float viewWidth;
		private Rect viewRect;

		private const int WaveformHeigth = 150;
		private const float MinViewSize = 0.01f;
		private const float MaxViewSize = 1.0f;
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
				viewRect = GUILayoutUtility.GetRect (0, WaveformHeigth, GUILayout.ExpandWidth (true));
				if (viewRect.width > 1 && viewRect.width != viewWidth)
				{
					waveformMat.SetFloat ("viewWidth", viewRect.size.x);
					viewWidth = viewRect.width;
					rebuildBuffer = true;
				}
				Graphics.DrawTexture (viewRect, Texture2D.whiteTexture, waveformMat);
				GUI.DrawTexture (viewRect, Texture2D.whiteTexture, 0, false, 0, Color.black, 3, 0); 
				#endregion

				#region Scroll view
				float scroll = GUILayout.HorizontalScrollbar (viewPos, viewSize, 0f, 1f);
				if (viewPos != scroll)
				{
					viewPos = scroll;
					rebuildBuffer = true;
				}

				if (Event.current.type == EventType.ScrollWheel
				&&	viewRect.Contains (Event.current.mousePosition))
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
			//float beatTime = 1f / (bpm / 60f);
			//float viewTime = viewSize * map.song.length;
			//int viewBeatsAmount = Mathf.CeilToInt (viewTime * (bpm / 60f));
			//
			//for (int b = 0; b != viewBeatsAmount; ++b)
			//{
			//
			//}

			//TO-DO: Clicking utils
			if (Event.current.type == EventType.MouseDown
			&&	viewRect.Contains (Event.current.mousePosition))
			{
				float songFactor = (Event.current.mousePosition.x - viewRect.xMin) / viewRect.width;
				speaker.time = (map.song.length * viewSize * songFactor) + (viewPos * map.song.length);
				if (!speaker.isPlaying) speaker.Play ();
			}

			#region Track changes
			if (EditorGUI.EndChangeCheck ())
			{
				if (song != map.song)
				{
					if (song != null) rebuildBuffer = true;
					else samplesBuffer?.Release ();
					speaker.clip = map.song = song;
				}

				if (bpm != map.bpm)
				{
					map.bpm = (int) bpm;
					// Re-make beat-overlay???
				}
			}
			#endregion

			BuildBuffer();
		}

		private void Awake () 
		{
			// Initialize stuff
			map = target as Map;
			viewSize = MaxViewSize;

			// Generate audio speaker
			speaker = new GameObject(name, typeof(AudioSource)).GetComponent<AudioSource>();
			speaker.hideFlags = HideFlags.HideAndDontSave;
			speaker.clip = map.song;
		}

		private void OnDestroy () 
		{
			// Release resources
			samplesBuffer?.Release ();
			DestroyImmediate (speaker);
		}

		#region Helpers
		private void BuildBuffer () 
		{
			// Ensure a song is selected & avoid rebulding when repainting
			if (map.song == null || Event.current.type == EventType.Repaint) return;
			// Pass only if demanded rebuilding or if it's null
			if (!rebuildBuffer && samplesBuffer != null) return;
			if (viewWidth < 1f) return;

			// Ensure we start on a L-channel sample
			int offset = (int)(map.song.samples * viewPos);
			if (offset % 2 != 0) offset++;

			// Compute view limits, in sample amounts
			int samplesAmount = (int) (map.song.samples * viewSize);
			int samplesXpixel = (int) ((samplesAmount * 2) / viewWidth);

			// Get actual audio data
			float[] samples = new float[samplesAmount * 2];
			map.song.GetData (samples, offset);

			#error Do no use viewidt! Insted use a fixed column value, and lerp visually in shader
			var waveform = new Vector4[(int) viewWidth];
			unchecked
			{
				// Skip samples based on amount of them to allow basic performance
				int skipMul = (int) (2d * (samplesAmount / 44100));

				for (int s = 0; s < samples.Length; s += 2)
				{
					if (s + 1 >= samples.Length) break;
					var col = waveform[0];

					float sL = samples[s];
					if (sL > 0f) col.x = (col.x > sL ? col.x : sL);
					else		 col.y = (col.y < sL ? col.y : sL);
						
					float sR = samples[s + 1];
					if (sR > 0f) col.z = (col.z > sL ? col.z : sR);
					else		 col.w = (col.w < sL ? col.w : sR);
				}


				/*
				// Read all samples in the view 
				for (int i = 0; i != waveform.Length; ++i)
				{
					// Combine all samples insiede each pixel column
					int start = i * samplesXpixel;
					int end = (i + 1) * samplesXpixel;

					// Save both R/L positive & negative values
					float pL = 0f, nL = 0f, pR = 0f, nR = 0f;
					void FeedVector (out Vector4 v)
					{
						v.x = pL; v.z = pR;
						v.y = nL; v.w = nR;
					}

					// Get highest / lowest samples
					for (int s = start; s < end; s += 2 + skipMul)
					{
						if (s + 1 > samples.Length) break;
						
						float sL = samples[s];
						if (sL > 0f) pL = (pL > sL ? pL : sL);
						else		 nL = (nL < sL ? nL : sL);
						
						float sR = samples[s + 1];
						if (sR > 0f) pR = (pR > sL ? pR : sR);
						else		 nR = (nR < sL ? nR : sR);
					}
					FeedVector (out waveform[i]);
				}
				*/
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
