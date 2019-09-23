using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Transform))]
public class CameraMovement : MonoBehaviour {

	public Transform _player;

	public float AdditionalLeftBuffer;
	public float AdditionalRightBuffer;
	public float AdditionalTopBuffer;
	public float AdditionalBottomBuffer;

	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {

		//Camera.main.aspect
		float xBuffer, yBuffer;

		if (Camera.main.aspect > 1.0f) {
			xBuffer = Camera.main.orthographicSize * Camera.main.aspect;
			yBuffer = Camera.main.orthographicSize;
		} else {
			xBuffer = Camera.main.orthographicSize;
			yBuffer = Camera.main.orthographicSize * Camera.main.aspect;
		}

		float xPos = Mathf.Clamp (_player.position.x,
		                          GameObject.Find ("LeftBoundary").transform.position.x + xBuffer - AdditionalLeftBuffer,
		                          GameObject.Find ("RightBoundary").transform.position.x - xBuffer + AdditionalRightBuffer);

		float yPos = Mathf.Clamp (_player.position.y,
		                          GameObject.Find ("BottomBoundary").transform.position.y + yBuffer - AdditionalBottomBuffer,
		                          GameObject.Find ("TopBoundary").transform.position.y - yBuffer + AdditionalTopBuffer);

		transform.position = new Vector3(xPos, yPos, transform.position.z);
	}
}
