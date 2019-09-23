using UnityEngine;
using System.Collections;

public class Egg : MonoBehaviour 
{
	public float hatchTime = 2f;
	public Sprite[] eggSprites;
	public GameObject snakePrefab;
	private SpriteRenderer spriteRend;
	private EdgeCollider2D edgeColl;
	private uint nextSprite;

	void Start () 
	{
		nextSprite = 1;
		spriteRend = GetComponent<SpriteRenderer> ();
		edgeColl = GetComponent<EdgeCollider2D> ();

		InvokeRepeating ("CrackEgg", hatchTime, hatchTime);
	}

	void OnCollisionEnter2D (Collision2D coll)
	{
		if (coll.gameObject.tag == "Player") {
			PlayerInfo.IncrementEggs();
			Destroy(gameObject);
		}
	}

	private void CrackEgg()
	{
		if (nextSprite != eggSprites.Length) 
			spriteRend.sprite = eggSprites [nextSprite++];
		
		if (edgeColl && nextSprite == eggSprites.Length) {
			Destroy (edgeColl);

			Instantiate(snakePrefab, new Vector3(transform.position.x, transform.position.y, snakePrefab.transform.position.z), Quaternion.identity);

			Invoke("DestroyEgg", 1f);
		}
	}

	private void DestroyEgg()
	{
		Destroy(gameObject);
	}
}
