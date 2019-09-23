using UnityEngine;
using System.Collections;

public class TunnelTransport : MonoBehaviour 
{
	public GameObject outTunnel; // The tunnel that this one leads to
	
	void Start () 
	{
	
	}
	

	void Update () 
	{
	
	}
	
	void OnTriggerEnter2D(Collider2D coll)
	{
		if (coll.gameObject.tag == "Player") {

			coll.gameObject.transform.position = new Vector3(outTunnel.transform.position.x, outTunnel.transform.position.y - 1.0f, coll.gameObject.transform.position.z);
			Camera.main.transform.position = new Vector3(outTunnel.transform.position.x, outTunnel.transform.position.y - 1.0f, Camera.main.transform.position.z);

			// Stop targeting the old tunnel
			coll.gameObject.GetComponent<MouseTargeter>().StopTargeting();
			coll.gameObject.GetComponent<AStarTargeter>().StopTargeting();

			// Switch directions
			coll.gameObject.GetComponent<Rigidbody2D>().velocity = new Vector2(0.0f, -1.0f);
			coll.gameObject.GetComponent<Movement>().FaceDown();

			// Clamp the mouse position to the part of the world we want to see
			float xBuffer, yBuffer;

			if (Camera.main.aspect > 1.0f) {
				xBuffer = Camera.main.orthographicSize * Camera.main.aspect;
				yBuffer = Camera.main.orthographicSize;
			} else {
				xBuffer = Camera.main.orthographicSize;
				yBuffer = Camera.main.orthographicSize * Camera.main.aspect;
			}
			
			float xPos = Mathf.Clamp (Camera.main.transform.position.x,
			                          GameObject.Find ("LeftBoundary").transform.position.x + xBuffer - Camera.main.GetComponent<CameraMovementRTS>().AdditionalLeftBuffer,
			                          GameObject.Find ("RightBoundary").transform.position.x - xBuffer + Camera.main.GetComponent<CameraMovementRTS>().AdditionalRightBuffer);
			
			float yPos = Mathf.Clamp (Camera.main.transform.position.y,
			                          GameObject.Find ("BottomBoundary").transform.position.y + yBuffer - Camera.main.GetComponent<CameraMovementRTS>().AdditionalBottomBuffer,
			                          GameObject.Find ("TopBoundary").transform.position.y - yBuffer + Camera.main.GetComponent<CameraMovementRTS>().AdditionalTopBuffer);
			
			Camera.main.transform.position = new Vector3(xPos, yPos, Camera.main.transform.position.z);
		}
	}
}
