using UnityEngine;
using System.Collections;

[RequireComponent(typeof(PlayerInfo))]
public class Mouth : MonoBehaviour {
	
	private float rotationOffset;
	private PlayerInfo playerInfo;
	private Movement movement;

	public bool survivalMode = false;
	public bool BubbleEnabled = true;
	public float BubbleCost = 20.0f;
	public float BubbleLaunchDistance = 0.3f;
	public GameObject waterProjectilePrefab;
	
	void Awake () {
		movement = transform.parent.GetComponent<Movement>();
		rotationOffset = transform.parent.rotation.eulerAngles.z;
		playerInfo = transform.parent.gameObject.GetComponent<PlayerInfo>();
	}
	
	// Update is called once per frame
	void Update () 
	{
		SprayWater();
	}

	void OnTriggerEnter2D(Collider2D other) {

		if (other.gameObject.tag.Equals ("Fly")) {

			Flocking flocker = other.gameObject.GetComponent<Flocking>();

			if (flocker != null) {
				Destroy (other.gameObject.GetComponent<Flocking>());
				Flocking.DestroyFlockMember(other.gameObject);
			}

			Destroy (other.gameObject);

			transform.parent.gameObject.GetComponent<PlayerInfo>().IncrementScore();

			if (survivalMode)
				FlyPlayerInfo.DecrementFlyCount();
		}
	}

	public bool SprayWater(bool frogIsBot = false, Vector2? target = null)
	{
		if (BubbleEnabled && !PlayerInfo.isPaused && (frogIsBot || Input.GetMouseButtonDown(0)) && playerInfo.waterLevel > PlayerInfo.BUBBLE_COST)
		{
			if (!frogIsBot) {
				target = (Vector2?)(Camera.main.ScreenToWorldPoint(Input.mousePosition));
			}
			
			Vector2 shotDirection = (Vector2)target - (Vector2)(transform.position);
			
			float angle = Mathf.Atan2(shotDirection.y, shotDirection.x) * Mathf.Rad2Deg;

			if (transform.parent.GetComponent<Animator>().GetBool("Sitting")) {
				transform.parent.GetComponent<Animator>().SetBool("Sitting", false);
			}

			if (movement != null) {
				movement.OverrideRotation(angle);
			}

			MouseTargeter mouseTargeter = transform.parent.GetComponent<MouseTargeter>();
			if (mouseTargeter != null) {
				mouseTargeter.StopTargeting();
			}

			AStarTargeter aStarTargeter = transform.parent.GetComponent<AStarTargeter>();
			if (aStarTargeter != null) {
				aStarTargeter.StopTargeting();
			}

			shotDirection.Normalize();
			
			Instantiate(waterProjectilePrefab,
			            new Vector3(transform.position.x + shotDirection.x * BubbleLaunchDistance, transform.position.y + shotDirection.y * BubbleLaunchDistance, transform.position.z),
			            Quaternion.Euler(0.0f, 0.0f, angle - rotationOffset));

			playerInfo.ReduceWaterAfterBubble();

			return true;

		} else {
			return false;
		}
	}
}
