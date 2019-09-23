using UnityEngine;
using System.Collections;

[RequireComponent(typeof(TextMesh))]
public class ChangeMusic : MonoBehaviour {

	private TextMesh textMesh;

	public Music musicPlayer;

	void Awake() {
		textMesh = GetComponent<TextMesh>();
	}

	// Use this for initialization
	void Start () {
		textMesh.text = "Music: " + musicPlayer.getCurrentTrackName();
	}

	void OnMouseDown()
	{
		musicPlayer.changeTrack();
		textMesh.text = "Music: " + musicPlayer.getCurrentTrackName();
	}
	
	void OnMouseEnter()
	{
		transform.localScale *= 1.5f;
	}
	
	void OnMouseExit()
	{
		transform.localScale /= 1.5f;
	}
}
