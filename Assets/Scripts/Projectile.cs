using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Movement))]
public class Projectile : MonoBehaviour
{
	private Movement movement;
	private Vector3 originalPos;
	private Vector3 currentPos;
	private Vector3 facingDir;
	public float destroyDistance = 4f;
	public float speed = 3f;

	void Start ()
	{
		originalPos = transform.position;
		movement = GetComponent<Movement>();
		facingDir = transform.rotation * Vector3.up;
		facingDir.Normalize();
	}

	void Update () 
	{
		currentPos = transform.position;

		if (Vector3.Distance(currentPos, originalPos) >= destroyDistance)
			Destroy(gameObject);

		movement.Move(facingDir * speed);
	}

	public void OnTriggerEnter2D(Collider2D coll)
	{
		if (coll.gameObject.tag == "Egg" || coll.gameObject.tag == "Predator") {
			Destroy(gameObject);
		}
	}
}
