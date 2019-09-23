using UnityEngine;
using System.Collections;

public class Lightning : MonoBehaviour 
{
	new private Light light;
	new private AudioSource audio;
	public float min, max; // The minimum and maximum time between lightning strikes.
	public float lightningLength; // How long the lightning should last.
	private bool play, iscalled;

	void Start () 
	{
		light = GetComponent<Light>();	
		audio = GetComponent<AudioSource>();
		play = true;
	}

	
	void Update ()
	{
		if (play) {
			play = false;
			Invoke("ShowLightning", Random.Range(min, max));
		}
	}


	void ShowLightning()
	{
		audio.Play();
		light.intensity = 1f;
		Invoke("HideLightning", lightningLength);
	}

	void HideLightning()
	{
		audio.Stop();
		light.intensity = 0f;
		play = true;
	}
}
