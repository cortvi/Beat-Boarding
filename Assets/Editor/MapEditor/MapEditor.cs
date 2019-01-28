using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using BeatBoarding.Data;
using System;

namespace BeatBoarding.Tools 
{
	public class MapEditor : EditorWindow 
	{
		#region Vars
		private Map map;
		private AudioSource speaker;

		private View view;
		public Material material;

		private int selectedBeat;
		private int playbackBeat;
		#endregion

		[MenuItem ("Window/Map Editor")]
		public static void Init () 
		{
			var win = GetWindow<MapEditor> ();
			win.titleContent = new GUIContent ("Map Editor");
			win.Show ();
		}

		private void OnGUI () 
		{
			map = Selection.activeObject as Map;
			speaker.clip = map?.song;

			if (map == null) return;
			bool rebuildBuffer = false;
			// ———

			// Map settings
			EditorGUILayout.LabelField ("Map settings: "+ map.name, EditorStyles.boldLabel);
			EditorGUI.BeginChangeCheck ();

			var song = Extension.ObjectField (map.song, "Song");
			var bpm = (int) (uint) EditorGUILayout.IntField ("Song BPM", map.bpm);

			#region Track settings changes
			if (EditorGUI.EndChangeCheck ())
			{
				if (song != map.song)
				{
					// Release buffer is song is removed
					if (song == null) view.buffer?.Release ();
					else rebuildBuffer = true;

					// Save to asset & load on speaker
					map.song = speaker.clip = song;
				}
				if (bpm != map.bpm) map.bpm = bpm;
				EditorUtility.SetDirty (map);
			}
			#endregion

			if (map.song != null)
			{
				#region Waveform View
				EditorGUILayout.Space ();
				var rect = GUILayoutUtility.GetRect (0, View.Height, GUILayout.ExpandWidth (true));
				if (rect.width > 1 && rect.width != view.width)
				{
					view.width = rect.width;
					rebuildBuffer = true;
				}
				Graphics.DrawTexture (rect, Texture2D.whiteTexture, material);

				// Get if mouse is inside Waveform rect
				float mouse = GetMouseInView (rect);
				#endregion

				#region View scroll
				float scroll = GUILayout.HorizontalScrollbar (view.scollPos, view.scrollSize, 0f, 1f);
				if (view.scollPos != scroll)
				{
					view.scollPos = scroll;
					rebuildBuffer = true;
				}

				// Allow zoom-in/-out
				if (mouse >= 0f
				&&	Event.current.type == EventType.ScrollWheel)
				{
					view.scrollSize += Event.current.delta.y * 0.01f;
					view.scrollSize = Mathf.Clamp (view.scrollSize, View.MinScrollSize, View.MaxScrollSize);
					rebuildBuffer = true;
				}
				float seconds = view.scrollSize * map.song.length;
				float startTime = view.scollPos * map.song.length;
				float endTime = (view.scollPos + view.scrollSize) * map.song.length;

				// Feed view limits
				if (rebuildBuffer)
				{
					material.SetFloat ("startTime", startTime);
					material.SetFloat ("endTime", endTime);
				}

				// Small indicator of how many seconds I'm seeing
				EditorGUILayout.LabelField ("Distance: " + seconds + " seconds.", EditorStyles.miniLabel);
				#endregion

				#region Beat overlay
				float beatTime = 1f / (map.bpm / 60f);
				int beatAmount = Mathf.FloorToInt (seconds / beatTime);
				int firstBeat = Mathf.CeilToInt (startTime / beatTime);

				var mark = new Rect (0, rect.yMin, 2f, rect.height);
				for (int b=firstBeat; b!=firstBeat + beatAmount; ++b)
				{
					float pos = Mathf.InverseLerp (startTime, endTime, b * beatTime);
					mark.x = Mathf.Lerp (rect.xMin, rect.xMax, pos);

					Color color;
					if (b == selectedBeat) color = Color.yellow;
					else
					if (b == playbackBeat)
					{
						color = Color.cyan;
						color.a = 0.9f;
					}
					else
					{
						color = Color.white;
						color.a = 0.2f;
					}
					// Draw beat marker
					EditorGUI.DrawRect (mark, color);
				}
				// Draw outer view borders
				GUI.DrawTexture (rect, Texture2D.whiteTexture, 0, false, 0, Color.black, borderWidth: 4, 0);

				// Feed shader with playback info
				if (speaker.isPlaying)
				{
					material.SetFloat ("playbackPos", speaker.time);
					playbackBeat = Mathf.FloorToInt (speaker.time / beatTime);
					Repaint ();
				}
				#endregion

				#region Controls
				if (mouse >= 0f
				&&	Event.current.type == EventType.MouseDown)
				{
					// Right click
					if (Event.current.button == 0)
					{
						float clickTime = Mathf.Lerp (startTime, endTime, mouse);
						selectedBeat = Mathf.RoundToInt (clickTime / beatTime);
						Repaint ();
					}
					else
					// Left click
					if (Event.current.button == 1)
					{
						if (!speaker.isPlaying)
						{
							float pos = selectedBeat * beatTime;
							material.SetFloat ("playbackStart", pos);

							speaker.Play ();
							speaker.time = pos;
						}
						else
						{
							material.SetFloat ("playbackStart", 0f);
							material.SetFloat ("playbackPos", 0f);
							speaker.Stop ();
							Repaint ();
						}
					}
					GUI.FocusControl (null);
				}
				#endregion

				#region Edit events
				// Make sure Maps has same amout of events as beats in the song
				int songBeats = Mathf.FloorToInt (map.song.length / beatTime);
				if (map.events.Length != songBeats)
				{
					var list = new Map.Event[songBeats];
					for (int i = 0; (i != list.Length && i != map.events.Length); ++i)
					{
						// Don't waste previous events
						list[i] = map.events[i];
					}
					map.events = list;
				}
				// Edit current beat event
				EventEditing (ref map.events[selectedBeat]);
				#endregion
			}

			// Rebuild samples buffer on demand
			if (rebuildBuffer || view.buffer == null)
				BuildBuffer ();
		}

		private void Awake () 
		{
			// Generate audio speaker
			speaker = new GameObject(name, typeof(AudioSource)).GetComponent<AudioSource>();
			speaker.gameObject.hideFlags = HideFlags.HideAndDontSave;
			view.scrollSize = View.MaxScrollSize;
		}

		private void OnDestroy () 
		{
			// Release resources
			view.buffer?.Release ();
			DestroyImmediate (speaker.gameObject);
		}

		#region Helpers
		private void EventEditing (ref Map.Event e)
		{
			EditorGUI.BeginChangeCheck ();

			if (EditorGUI.EndChangeCheck ())
			{

				EditorUtility.SetDirty (map);
			}
		}

		private float GetMouseInView (Rect rect) 
		{
			var mouse = Event.current.mousePosition;
			// Return horizontal value relative to Waveform-View rect
			if (rect.Contains (mouse)) return (mouse.x - rect.xMin) / rect.width;
			else return -1f;
		}

		private void BuildBuffer () 
		{
			if (Event.current.type == EventType.Repaint
			||	map.song == null || view.width <= 1f) return;
			// ———

			#region Compute values
			// Ensure we start on a L-channel sample
			int offset = (int)(map.song.samples * view.scollPos);
			if (offset % 2 != 0) offset--;

			// Compute view limits, in number of samples
			int samplesAmount = (int)(map.song.samples * view.scrollSize);

			// Compute how many samples per bar
			int barAmount = (int)(view.width / 4f);
			material.SetFloat ("barAmount", barAmount);
			int samplesXbar = (samplesAmount * 2) / barAmount;

			// Get actual audio data
			float[] samples = new float[samplesAmount * 2];
			map.song.GetData (samples, offset);

			var waveform = new Vector4[barAmount];
			// Skip samples based on amount of them to allow basic performance
			int skipMul = 2 * (samplesAmount / (44100));
			#endregion

			#region Read all samples
			int barStart = 0, barEnd = samplesXbar;
			for (int b = 0; b != waveform.Length; ++b)
			{
				var bar = waveform[b];
				barEnd = samplesXbar * b;
				for (int s = barStart; (s < barEnd && s + 1 < samples.Length); s += 2 * skipMul)
				{
					float sL = samples[s];
					if (sL > 0f) bar.x = (bar.x > sL ? bar.x : sL);
					else bar.y = (bar.y < sL ? bar.y : sL);

					float sR = samples[s + 1];
					if (sR > 0f) bar.z = (bar.z > sL ? bar.z : sR);
					else bar.w = (bar.w < sL ? bar.w : sR);
				}
				waveform[b] = bar;
				barStart = barEnd;
			} 
			#endregion

			// Create actual buffer
			if (view.buffer == null || !view.buffer.IsValid ()
			||	view.buffer.count != waveform.Length)
			{
				view.buffer?.Dispose ();
				view.buffer = new ComputeBuffer (waveform.Length, 4 * sizeof (float));
			}
			// Feed the shader
			view.buffer.SetData (waveform);
			material.SetBuffer ("waveform", view.buffer);

			Repaint ();
		}
		#endregion

		private struct View 
		{
			public float width;
			public ComputeBuffer buffer;

			public float scollPos;
			public float scrollSize;

			public const int Height = 150;
			public const float MinScrollSize = 0.05f;
			public const float MaxScrollSize = 0.40f;
		}
	}
}
