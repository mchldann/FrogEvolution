using UnityEngine;
using System.Collections;

public enum SnakeDirections
{
	Up = 0,
	Left = 1,
	Down = 2,
	Right = 3
};

[RequireComponent(typeof(GameObjectTargeter))]
[RequireComponent(typeof(HuntTargeter))]
[RequireComponent(typeof(AStarTargeter))]
[RequireComponent(typeof(Wander))]
[RequireComponent(typeof(Seek))]
[RequireComponent(typeof(Movement))]
[RequireComponent(typeof(Animator))]
public class PredatorStateMachine : MonoBehaviour 
{
	private static int MaxSnakes;

	private GameObjectTargeter homeTargeter;
	private HuntTargeter huntTargeter;
	private AStarTargeter aStarTargeter;
	private Wander wanderer;
	private Seek seek;
	private Movement movement;
	private Animator animator;
	private float timeSinceWentHome;
	private bool wasChasing = false;
	private GameObject child = null;
	private float parentingTimer;
	public State currentState; // Made public just for debugging purposes
	private AudioSource SoundSource;

	[HideInInspector]
	public float bubbleTimeLeft = 0.0f;

	private float bubbleFlickerTime = 1.0f;
	private float bubbleFlickerFrequency = 8.0f;
	private float chaseTimeLeft = 0.0f;
	private float normalSpeed;
	private float chaseSpeed;
	private float timeUnderwater = 0.0f;
	private Vector3 originalScale;

	// Making the snakes go in/out of the water is a bit tricky... When they're on the edge of
	// a lake's trigger they have a tendency to flicker between being inside/outside of the trigger.
	// I've made it so that they have to move a certain distance before flipping state.
	private const float WATER_TRANSITION_DISTANCE = 0.5f;
	private Vector2 lastWaterTransitionPos = Vector2.zero;
	private bool lastOnLand = true;

	// Stop hisses being played while there's another hiss going
	private bool soundWasPlaying = false;
	private static bool soundPlaying = false;
	
	public GameObject Home;
	public GameObject Player;
	public GameObject Egg;
	public bool DemoMode = false;
	public bool TrainingMode = false;
	public float ParentAge; // age in seconds
	public float ParentDesire = 0.3f;
	public float LeashLength;
	public float GiveUpDistance = 4.0f;
	public float MinChaseTime = 5.0f;
	public float GoHomeTimeout = 1.5f;
	public float KnockForce = 250.0f;
	public float BubbleTime;
	public float StaySunkTime = 1.0f;
	public float SunkDeadTime = 2.0f;
	public float MinReemergenceTime = 0.1f;
	public bool nearObstacle = false;
	public SpriteRenderer bubble;
	public AudioClip AttackSound;
	public AudioClip SplashSound;
	
	public enum State
	{
		Chasing,
		Wandering,
		HeadingHome,
		Parenting,
		Bubbled,
		Sunk
	};
	
	void Awake ()
	{
		homeTargeter = GetComponent<GameObjectTargeter>();
		huntTargeter = GetComponent<HuntTargeter>();
		aStarTargeter = GetComponent<AStarTargeter>();
		wanderer = GetComponent<Wander>();
		seek = GetComponent<Seek>();
		movement = GetComponent<Movement>();
		animator = GetComponent<Animator>();
		originalScale = transform.localScale;
		
		parentingTimer = 0f;
		timeSinceWentHome = GoHomeTimeout;
		
		// Ensure that the snake has someone to target and a home.
		if (!TrainingMode && (Home == null || Player == null))
		{
			// Place in predator hierarchy.
			transform.parent = GameObject.Find("Predators").transform;
			
			// Set the player for the predator to chase and this predators home base.
			if (Player == null) {
				Player = GameObject.FindGameObjectWithTag("Player");
			}

			if (Random.Range(0, 2) == 0)
				Home = GameObject.Find ("SnakeHomeLeft");
			else
				Home = GameObject.Find ("SnakeHomeRight");
		}
		
		huntTargeter.Target = Player;
		homeTargeter.Target = Home;
		
		currentState = State.HeadingHome; // So we don't play the chasing sound immediately!
		
		SoundSource = gameObject.AddComponent<AudioSource>();
		SoundSource.loop = false;

		// Difficulty settings
		int difficulty = 1;
		GameObject gameMasterGameObj = GameObject.Find("GameMaster");
		GameMaster gameMaster = null;

		if (gameMasterGameObj != null) {
			gameMaster = gameMasterGameObj.GetComponent<GameMaster>();
			difficulty = gameMaster.difficulty;
		}

		switch (difficulty) {
			case 0:
				// Easy
				BubbleTime = 6.0f;
				normalSpeed = 1.5f;
				chaseSpeed = 1.9f;
				LeashLength = 8.0f;
				break;
			case 1:
			default:
				// Normal
				BubbleTime = 3.0f;
				normalSpeed = 2.0f;
				chaseSpeed = 3.0f;
				LeashLength = 8.0f;
				break;
			case 2:
				// Hard
				BubbleTime = 2.0f;
				normalSpeed = 2.0f;
				chaseSpeed = 3.5f;
				LeashLength = 12.0f;
				break;
			case 3:
				// Insane
				BubbleTime = 1.5f;
				normalSpeed = 3.0f;
				chaseSpeed = 4.0f;
				LeashLength = 9999.0f;
				break;
		}

		if (difficulty == 0) {
			huntTargeter.dumbAttack = true;
		}
		
		MaxSnakes = 5 + difficulty;
		
		ParentAge = 40.0f - 10.0f * (float)(difficulty);

		// For the class demonstration
		if (DemoMode) {

			// Make the snakes chase over the whole map in the demo
			LeashLength = 9999.0f;

			if (gameMaster != null) {
				if (gameMaster.SmartDemoSnakes) {
					huntTargeter.dumbAttack = false;
				} else {
					huntTargeter.dumbAttack = true;
				}
			} else {
				// Default
				huntTargeter.dumbAttack = false;
			}
		}
	}
	
	// Update is called once per frame
	void Update () 
	{
		if (soundWasPlaying && !SoundSource.isPlaying) {
			soundPlaying = false;
			soundWasPlaying = false;
		}

		if (currentState != State.Sunk) {
			transform.localScale = originalScale;
		} else {
			transform.localScale = GameObject.FindGameObjectWithTag("Player").transform.localScale; // So that the underwater animation is the same size as the frog's
		}

		timeSinceWentHome += Time.deltaTime;
		parentingTimer += Time.deltaTime;
		chaseTimeLeft = Mathf.Max(0.0f, chaseTimeLeft - Time.deltaTime);
		//waterTransitionTimer += Time.deltaTime;
		
		UpdateState();
		
		// Defaults
		seek.weight = 1.0f;
		aStarTargeter.underlyingTargeter = homeTargeter;
		bubble.enabled = false;
		gameObject.layer = LayerMask.NameToLayer("Default");
		
		switch(currentState)
		{
		case State.Chasing:
			aStarTargeter.underlyingTargeter = huntTargeter;
			child = null;
			wanderer.weight = 0.0f;
			movement.acceleration = 5.0f;
			movement.speed = chaseSpeed;
			if (!wasChasing) {
				if (!soundPlaying) {
					soundPlaying = true;
					soundWasPlaying = true;
					if (!TrainingMode) {
						SoundSource.clip = AttackSound;
						SoundSource.Play();
					}
				}
				chaseTimeLeft = MinChaseTime;
			}
			wasChasing = true;
			break;
		case State.HeadingHome:
			child = null;
			homeTargeter.Target = Home;
			wanderer.weight = 0.0f;
			movement.acceleration = 1.0f;
			movement.speed = normalSpeed;
			
			if (wasChasing) {
				timeSinceWentHome = 0.0f;
				wasChasing = false;
			}
			break;
		case State.Parenting:
			homeTargeter.Target = child;
			wanderer.weight = 0.2f;
			movement.acceleration = 1.0f;
			movement.speed = normalSpeed;
			wasChasing = false;
			break;
		case State.Wandering:
			child = null;
			seek.weight = 0.0f;
			wanderer.weight = 1.0f;
			movement.acceleration = 1.0f;
			movement.speed = normalSpeed;
			wasChasing = false;
			break;
		case State.Bubbled:

			bubble.enabled = true;

			// Make the bubble flicker if it's about to expire
			if (bubbleTimeLeft < bubbleFlickerTime) {
				if (((int)(Time.unscaledTime * bubbleFlickerFrequency * 2.0f)) % 2 == 0) {
					bubble.enabled = false;
				}
			}

			gameObject.layer = LayerMask.NameToLayer("BubbledSnake");
			child = null;
			seek.weight = 0.0f;
			wanderer.weight = 0.0f;
			movement.acceleration = 0.0f;
			movement.speed = 0.0f;
			wasChasing = false;
			break;
		case State.Sunk:
			gameObject.layer = LayerMask.NameToLayer("BubbledSnake");
			bubble.enabled = false;
			child = null;
			seek.weight = 0.0f;
			wanderer.weight = 0.0f;
			movement.acceleration = 0.0f;
			movement.speed = 0.0f;
			wasChasing = false;
			break;
		}
		
		UpdateAnimation();
	}
	
	
	private void UpdateState()
	{
		// Transition between water and land
		if ((((Vector2)transform.position) - lastWaterTransitionPos).magnitude > WATER_TRANSITION_DISTANCE) {

			// Emerge from water
			if ((currentState == State.Sunk) && lastOnLand) {
				currentState = State.Bubbled;
				lastWaterTransitionPos = ((Vector2)transform.position);
				if (!soundPlaying) {
					soundPlaying = true;
					soundWasPlaying = true;
					if (!TrainingMode) {
						SoundSource.clip = SplashSound;
						SoundSource.Play();
					}
				}
			}

			// Go into water
			if ((currentState != State.Sunk) && !lastOnLand) {
				currentState = State.Sunk;
				lastWaterTransitionPos = ((Vector2)transform.position);
				timeUnderwater = 0.0f;
				if (!soundPlaying) {
					soundPlaying = true;
					soundWasPlaying = true;
					if (!TrainingMode) {
						SoundSource.clip = SplashSound;
						SoundSource.Play();
					}
				}
			}
		}

		if (currentState == State.Sunk) {

			timeUnderwater += Time.deltaTime;
				
			if (timeUnderwater > SunkDeadTime) {
				PlayerInfo.IncrementSnakesDrowned();
				Destroy(this.gameObject);
			}

			return;
		}

		if (bubbleTimeLeft > 0.0f) {
			currentState = State.Bubbled;
			bubbleTimeLeft -= Time.deltaTime;
			return;
		}
		
		// Let the parent remain with the egg
		if(child)
		{
			//Debug.Log("with child");
			if ((((Vector2)(transform.position) - (Vector2)(Player.transform.position)).magnitude < GiveUpDistance) || chaseTimeLeft > 0.0f)
			{
				currentState = State.Chasing;
				return;
			}
			else 
			{
				currentState = State.Parenting;
				return;
			}
		}

		// Don't attempt to lay eggs near obstacles - we don't want to lay eggs in the lake!
		if (!nearObstacle && (parentingTimer >= ParentAge) && !child) 
		{
			parentingTimer = 0f;

			GameObject[] snakes = GameObject.FindGameObjectsWithTag("Predator");
			GameObject[] eggs = GameObject.FindGameObjectsWithTag("Egg");

			// Extra randomization step to see if an egg is to be created.
			if ((snakes.Length + eggs.Length < MaxSnakes) && (Random.Range(0f,1f) <= ParentDesire)) {
				LayEgg();
				currentState = State.Parenting;
				return;
			}
		}

		float distanceFromHome = ((Vector2)(transform.position) - (Vector2)(Home.transform.position)).magnitude;
		float distanceFromPlayer = ((Vector2)(transform.position) - (Vector2)(Player.transform.position)).magnitude;
		float playerDistanceFromHome = ((Vector2)(Player.transform.position) - (Vector2)(Home.transform.position)).magnitude;
		
		if ((distanceFromHome > LeashLength) && (distanceFromPlayer > GiveUpDistance) && (chaseTimeLeft <= 0.0f)) {

			currentState = State.HeadingHome;

		} else if (timeSinceWentHome > GoHomeTimeout) {	

			currentState = State.Wandering;

			// Try targeting the player directly to see if they're reachable
			homeTargeter.Target = Player;
			aStarTargeter.underlyingTargeter = homeTargeter;
			Vector2? target = aStarTargeter.GetTarget();
			
			if (target != null) 
			{	
				// Check if we're gonna chase.
				if (((playerDistanceFromHome < LeashLength) || (distanceFromPlayer < GiveUpDistance) || (chaseTimeLeft > 0.0f))
				    && !Player.GetComponent<PlayerInfo>().IsUnderwater()) {

					currentState = State.Chasing;	
				} 
			}
		}
	}
	
	public void Sink() {
		lastOnLand = false;
	}
	
	public void Unsink() {
		lastOnLand = true;
	}
	
	private void UpdateAnimation()
	{
		if (currentState == State.Sunk) {
			animator.SetBool("Sunk", true);
			return;
		} else {
			animator.SetBool("Sunk", false);
		}
		
		float actualRotation = transform.localEulerAngles.z - movement.angleAdjustment;
		
		while (actualRotation < 0.0f)
			actualRotation += 360.0f;
		
		while (actualRotation > 360.0f)
			actualRotation -= 360.0f;
		
		SnakeDirections dir = SnakeDirections.Up;
		
		if ((actualRotation > 45.0f) && (actualRotation < 135.0f)) {
			dir = SnakeDirections.Up;
		} else if ((actualRotation > 135.0f) && (actualRotation < 225.0f)) {
			dir = SnakeDirections.Left;
		} else if ((actualRotation > 225.0f) && (actualRotation < 315.0f)) {
			dir = SnakeDirections.Down;
		} else if ((actualRotation > 315.0f) || (actualRotation < 45.0f)) {
			dir = SnakeDirections.Right;
		}
		
		animator.SetInteger("Direction", (int)dir);
	}
	
	
	private void CheckIfHitPlayer(Collider2D other) 
	{
		if ((currentState != State.Bubbled) && (currentState != State.Sunk) && (other.gameObject.tag.Equals ("Player"))) {

			PlayerInfo playerInfo = other.gameObject.GetComponent<PlayerInfo>();

			if (!playerInfo.IsInvulnerable()) {
				
				playerInfo.DecrementHealth();
				playerInfo.MakeInvulnerable();
				
				// Knock the player
				GameObject player = other.gameObject;
				Vector2 knockDirection = ((Vector2)(player.transform.position - transform.position)).normalized;
				player.GetComponent<Rigidbody2D>().AddForce(knockDirection * KnockForce);
			}
		}
	}
	
	
	private void LayEgg()
	{
		child = (GameObject)Instantiate(Egg, transform.position - Vector3.down + new Vector3(0.0f, 0.0f, 1.0f), Quaternion.identity);
	}
	
	
	public void OnTriggerEnter2D(Collider2D other) 
	{
		CheckIfHitPlayer(other);
		
		if (other.gameObject.tag == "Projectile") {
			GetComponent<Rigidbody2D>().velocity = Vector2.zero;
			bubbleTimeLeft = BubbleTime;
			if (currentState != State.Sunk) {
				currentState = State.Bubbled;
			}
		}
	}
	
	
	public void OnTriggerStay2D(Collider2D other) 
	{
		// Try to ensure we don't get stuck ramming against an obstacle
		if ((LayerMask.LayerToName(other.gameObject.layer) == "Obstacles") || 
		    (LayerMask.LayerToName(other.gameObject.layer) == "Pond")) {

			AStarTargeter ast = GetComponent<AStarTargeter>();

			if (ast != null) {
				ast.ForceRecalculate();
			}
		}

		CheckIfHitPlayer(other);
	}
}
