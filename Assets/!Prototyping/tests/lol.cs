using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class lol : MonoBehaviour 
{
	public uint bpm;
	private Rigidbody body;
	private new AudioSource audio;

	public Texture2D tex;

    void Start()
    {
		// lmaokai
        body = GetComponent<Rigidbody>();
		audio = GetComponent<AudioSource>();

		tex = AudioWaveform(audio.clip, 1920, 1080, Color.blue);
    }

	private bool playing;
	double secondsBetweenBeats;
	double nextBeatTime = 0;
	uint beat = 0;

	// Update is called once per frame
	void Update () 
    {
		if (Input.GetKeyDown("p"))
		{
			secondsBetweenBeats = 1.0 / (bpm / 60.0);
			playing = true;
			audio.Play();
		}
		if (Input.GetKeyDown(KeyCode.Alpha0))
		{
			Application.targetFrameRate = -1;
		}
		if (Input.GetKeyDown(KeyCode.Alpha1))
		{
			Application.targetFrameRate = 60;
		}
		if (Input.GetKeyDown(KeyCode.Alpha2))
		{
			Application.targetFrameRate = 30;
		}
		if (Input.GetKeyDown(KeyCode.Alpha3))
		{
			Application.targetFrameRate = 10;
		}

		if (Input.GetKeyDown("w")) body.AddForce(Vector3.up * 8f, ForceMode.VelocityChange);
		float input = Input.GetAxis("Horizontal");
		var vel = body.velocity;
		vel.z = 400f * input * Time.deltaTime;
		body.velocity = vel;

		if (Input.GetKeyDown (KeyCode.Space))
		{
			print ((nextBeatTime - audio.time) / secondsBetweenBeats);
		}

		if (playing)
		{
			const float scaleValue = 1.1f;
			if (audio.time >= nextBeatTime)
			{
				nextBeatTime = ++beat * secondsBetweenBeats;

				transform.localScale = Vector3.one * scaleValue;
				GetComponent<Renderer>().material.color = Random.ColorHSV(0.6f, 1.0f);
			}
			else transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * 0.6f, Time.deltaTime * 12f);
		}
    }


	public static Texture2D AudioWaveform(AudioClip aud, int width, int height, Color color) 
	{

		int step = Mathf.CeilToInt((aud.samples * aud.channels) / width);
		float[] samples = new float[aud.samples * aud.channels];

		//getData after the loadType changed
		aud.GetData(samples, 0);

		Texture2D img = new Texture2D(width, height, TextureFormat.RGBA32, false);

		Color[] xy = new Color[width * height];
		for (int x = 0; x < width * height; x++)
		{
			xy[x] = new Color(0, 0, 0, 0);
		}

		img.SetPixels(xy);

		int i = 0;
		while (i < width)
		{
			int barHeight = Mathf.CeilToInt(Mathf.Clamp(Mathf.Abs(samples[i * step]) * height, 0, height));
			int add = samples[i * step] > 0 ? 1 : -1;
			for (int j = 0; j < barHeight; j++)
			{
				img.SetPixel(i, Mathf.FloorToInt(height / 2) - (Mathf.FloorToInt(barHeight / 2) * add) + (j * add), color);
			}
			++i;

		}

		img.Apply();
		return img;
	}
}
