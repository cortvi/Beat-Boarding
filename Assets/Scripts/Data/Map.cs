using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BeatBoarding.Data
{
	[CreateAssetMenu (fileName = "Song Map", menuName = "New Song Map")]
	public class Map : ScriptableObject 
	{
		public AudioClip song;
		public int bpm;
	}
}
