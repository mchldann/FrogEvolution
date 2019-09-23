using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class GAFrogController : GAController<NeuralNet> {

	// Parent selection is used at the end of an epoch to determine
	// which neural nets to create the children from
	public enum ParentSelectionMode
	{
		Proportional = 0, // Not actually used in any of our serious training. Was terrible.
		Exponential = 1,  // Also terrible.
		Tournament = 2,
		RankRoulette = 3
	};

	[System.Serializable]
	public class GAParameters {

		// NumberOfBatches controls how many sets of 8 frogs constitute an epoch
		// i.e. Population size = NumberOfBatches * 8
		public int NumberOfBatches = 1;

		// The amount of time given to each batch
		public float batchTime = 10.0f;

		// The speed up factor
		public float timeScale = 3.0f;

		public bool spawnFlies = true;
		public bool flyMovement = false;
		public bool spawnSnakes = false;

		// We hacked the fly obstacle avoidance from the last assignment to work for the frog.
		// A* and neural nets don't really go together...
		public bool frogObstacleAvoidance = true;

		// Originally I thought it might be a good idea to discard frogs (i.e. randomise their
		// neural net weights again) if they performed badly enough. Turned out this wasn't too
		// effective, so we just set this parameter to -9999f in the end.
		public float discardThreshold = 2.9f;

		public ParentSelectionMode parentSelectionMode = ParentSelectionMode.Proportional;

		// These "accentuation" variables make it so that the best performers get a bigger slice
		// of the pie when in the "Proportional" and "Exponential" parent selection methods.
		// Neither of these were really used in the end.
		public float propSelectionAccentuation = 0.5f; // Should be between 0 and 1
		public float expSelectionAccentuation = 0.5f;

		// Turning this on causes a bunch of stuff to be printed to the console
		public bool verbose = false;

		public NeuralNetSettings neuralNetSettings;
	} 

	[System.Serializable]
	public class NeuralNetSettings {

		// Input settings
		public int NumFlyPositions = 2;
		public int NumSnakePositions = 1;
		public int NumObstaclePositions = 2;

		public bool FeedObstacleInfo = true;

		// We thought it might be good for the frog to know its own velocity (so that it could
		// take momentum into account), but this was only effective when we used a miniature
		// training pen.
		public bool FeedOwnVelocity = true;

		public bool FeedLakePosition = true;
		public bool FeedWaterLevel = true;

		// This controls whether the nets use "input normalisation" (the textbook way of doing things)
		// or my weird method of feeding the input in multiple times with different rotations.
		public NeuralNet.InputTransformation inputTransformation = NeuralNet.InputTransformation.RotationSmoothing;
		public int inputSmoothingSegments = 30;

		public int HiddenNeurons = 4;
	}

	public GameObject trainingSnakePrefab;
	public string LoadPath = "";
	public int LoadEpoch = 0;
	public int FrogsOnScreen = 8;
	public int CurrentEpoch = 0;
	public int CurrentBatch = 0;
	public float bestFitnessSoFar = float.MinValue;
	public NeuralNet bestNetSoFar;
	public bool letFrogShootBubble = false;
	public int parameterIndexToUse = 0;
	public GAParameters[] parameters;

	private GAParameters currentParams;
	private float updateTimer = 0.0f;
	private int currentPopIndex = 0;
	private string saveDataPath;

	// These are all used to avoid excessive GetComponent calls in the Update method
	private List<GameObject> penManagers;
	private List<ManagePen> penManagerScripts;
	private List<GameObject> frogs;
	private List<PlayerInfo> frogPlayerInfo;
	private List<NeuralNetSteering> neuralNetSteering;
	
	// Defaults for mutation and crossover rates are as recommended in the "AI Techniques for Game Programming" book.
	public GAFrogController(float mutationRate = 0.001f, float crossoverRate = 0.7f) : base(0, mutationRate, crossoverRate) {}

	public GAFrogController() : base(0, 0.001f, 0.7f) {}

	// Here because it's specified by the base class, but not actually needed. Initialisation is handled by Awake.
	public override void InitPopulation() {}

	// The log file is written to the SaveData directory
	private void WriteToLog(string logLine) {
		StreamWriter writer = File.AppendText(saveDataPath + "/log.txt");
		writer.WriteLine(logLine);
		writer.Close();
	}

	// We serialise the entire population at the start of each epoch so that we can
	// stop Unity and resume later. (Training can take a long time for the frogs!)
	private void SavePopulation() {

		BinaryFormatter bf = new BinaryFormatter();

		if (CurrentEpoch == 0) {
			FileStream paramsFile = File.Create(saveDataPath + "/params.bin");
			bf.Serialize(paramsFile, currentParams);
			paramsFile.Close();
		}

		FileStream popFile = File.Create(saveDataPath + "/population" + CurrentEpoch + ".bin");
		bf.Serialize(popFile, population);
		popFile.Close();

		if (bestFitnessSoFar > float.MinValue) {
			FileStream bestNetFile = File.Create(saveDataPath + "/bestNet" + CurrentEpoch + ".bin");
			bf.Serialize(bestNetFile, bestNetSoFar);
			bestNetFile.Close();
		}
	}

	// Load the entire population from a binary file
	private void LoadPopulation() {

		BinaryFormatter bf = new BinaryFormatter();

		FileStream paramsFile = File.Open("SaveData/" + LoadPath + "/params.bin", FileMode.Open);
		currentParams = (GAParameters)bf.Deserialize(paramsFile);
		paramsFile.Close();

		CurrentEpoch = LoadEpoch;
		string saveFilename = "SaveData/" + LoadPath + "/population" + LoadEpoch + ".bin";

		FileStream popFile = File.Open(saveFilename, FileMode.Open);
		population = (List<NeuralNet>)bf.Deserialize(popFile);
		popFile.Close();
	}

	public void Awake() {

		// Do all the GetComponent calls upfront - Justin said it's much more efficient!
		penManagers = new List<GameObject>(GameObject.FindGameObjectsWithTag("PenManager"));
		penManagerScripts = new List<ManagePen>();
		frogs = new List<GameObject>();
		frogPlayerInfo = new List<PlayerInfo>();
		neuralNetSteering = new List<NeuralNetSteering>();

		// Get references to the frogs and some of their components
		for (int i = 0; i < penManagers.Count; i++) {
			penManagerScripts.Add(penManagers[i].GetComponent<ManagePen>());
			GameObject frog = penManagers[i].GetComponent<ManagePen>().frog;
			frogs.Add(frog);
			frogPlayerInfo.Add(frog.GetComponent<PlayerInfo>());
			neuralNetSteering.Add(frog.GetComponent<NeuralNetSteering>());
		}

		// Set up directory for saving neural nets, etc
		saveDataPath = "SaveData/" + System.DateTime.Now.ToString("yyMMddHHmmss");
		Directory.CreateDirectory(saveDataPath);

		if (LoadPath != "") {

			// Load the population from a saved file
			LoadPopulation();
			populationSize = population.Count;

		} else {

			// Create a new population from scratch
			currentParams = parameters[parameterIndexToUse];
			population = new List<NeuralNet>();
			populationSize = currentParams.NumberOfBatches * FrogsOnScreen;

			for (int i = 0; i < populationSize; i++) {
				
				population.Add(new NeuralNet(currentParams.neuralNetSettings.NumFlyPositions,
				                             currentParams.neuralNetSettings.NumSnakePositions,
				                             currentParams.neuralNetSettings.NumObstaclePositions,
				                             currentParams.neuralNetSettings.FeedObstacleInfo,
				                             currentParams.neuralNetSettings.FeedOwnVelocity,
				                             currentParams.neuralNetSettings.FeedLakePosition,
				                             currentParams.neuralNetSettings.FeedWaterLevel,
				                             currentParams.neuralNetSettings.HiddenNeurons,
				                             currentParams.neuralNetSettings.inputTransformation,
				                             currentParams.neuralNetSettings.inputSmoothingSegments));
			}
		}

		// Create snakes if need be
		ResetSnakes();
	}

	public void Start() {

		WriteToLog("GA parameters");
		WriteToLog("-------------");
		WriteToLog("");
		WriteToLog("Batches per epoch: " + currentParams.NumberOfBatches);
		WriteToLog("Batch time: " + currentParams.batchTime);
		WriteToLog("Frog obstacle avoidance: " + currentParams.frogObstacleAvoidance);
		WriteToLog("Parent selection mode: " + currentParams.parentSelectionMode.ToString());
		WriteToLog("Snakes: " + currentParams.spawnSnakes);
		WriteToLog("Flies: " + currentParams.spawnFlies);
		WriteToLog("Fly movement: " + currentParams.flyMovement);
		WriteToLog("");
		WriteToLog("");
		WriteToLog("Neural net settings");
		WriteToLog("-------------------");
		WriteToLog("Hidden neurons: " + currentParams.neuralNetSettings.HiddenNeurons);
		WriteToLog("");
		WriteToLog("");
		WriteToLog("Training Log");
		WriteToLog("------------");

		Time.timeScale = currentParams.timeScale;

		SavePopulation();

		// It's CRITICAL that this stuff goes in Start(), not Awake() since the frogs may not
		// all be initialised before this component. It was causing me nightmares!
		foreach (GameObject frog in frogs) {
			frog.GetComponent<NeuralNetSteering>().neuralNet = population[currentPopIndex];
			frog.GetComponent<NeuralNetSteering>().neuralNet.ParentFrog = frog;
			frog.GetComponentInChildren<Mouth>().BubbleEnabled = letFrogShootBubble;
			frog.GetComponent<SteeringController>().avoidObstacles = currentParams.frogObstacleAvoidance;
			IncrementPopulationIndex();
		}

		// Initialise fly spawning stuff
		for (int i = 0; i < penManagerScripts.Count; i++) {
			penManagerScripts[i].spawnFlies = currentParams.spawnFlies;
			penManagerScripts[i].flyMovement = currentParams.flyMovement;
		}
	}

	// currentPopIndex stores the index of the last neural net that was used for a frog.
	// It increments over batches but gets reset to zero at the start of an epoch.
	public void IncrementPopulationIndex() {
		currentPopIndex = (currentPopIndex + 1) % populationSize;
	}

	public void ResetSnakes() {

		// Respawn snakes if necessary
		if (currentParams.spawnSnakes) {

			for (int i = 0; i < penManagers.Count; i++) {
				
				List<GameObject> childSnakes = new List<GameObject>();
				Hashtable snakeHomes = new Hashtable();
				
				foreach (Transform child in penManagers[i].transform) {
					if (child.tag == "Predator") {
						childSnakes.Add(child.gameObject);
					} else if (child.name.Contains("SnakeHome")) {
						snakeHomes.Add(child.gameObject, false);
					}
				}

				penManagerScripts[i].snakes = new List<GameObject>(childSnakes);
				
				foreach (GameObject snake in childSnakes) {
					GameObject snakeHome = snake.GetComponent<PredatorStateMachine>().Home;
					if (snakeHomes.Contains(snakeHome)) {
						snakeHomes[snakeHome] = true;
					}
				}
				
				foreach (object o in snakeHomes.Keys) {
					GameObject snakeHome = (GameObject)o;
					if ((bool)snakeHomes[o] == false) {
						GameObject snake = Instantiate(trainingSnakePrefab, snakeHome.transform.position, Quaternion.identity) as GameObject;
						snake.transform.parent = penManagers[i].transform;
						snake.GetComponent<PredatorStateMachine>().Home = snakeHome;
						snake.GetComponent<PredatorStateMachine>().Player = penManagerScripts[i].frog;
						snake.GetComponent<GameObjectTargeter>().Target = penManagerScripts[i].frog;
						snake.GetComponent<HuntTargeter>().Target = penManagerScripts[i].frog;
						penManagerScripts[i].snakes.Add(snake);
					}
				}
			}
		}
	}
	
	public void Update() {

		updateTimer += Time.deltaTime;

		for (int i = 0; i < penManagers.Count; i++) {

			NeuralNet net = neuralNetSteering[i].neuralNet;

			// Water score
			net.waterScore += Time.deltaTime * frogPlayerInfo[i].waterLevel;

			// Water camping score
			if (frogPlayerInfo[i].IsUnderwater() && frogPlayerInfo[i].waterLevel >= 100.0f) {
				net.waterCampingScore += Time.deltaTime;
			}

			// Snake distance score
			foreach (GameObject snake in penManagerScripts[i].snakes) {
				if (snake != null) {
					net.snakeDistScore += Time.deltaTime * Mathf.Min(1.75f, ((Vector2)(snake.transform.position) - (Vector2)(penManagerScripts[i].frog.transform.position)).magnitude);
				}
			}
		}

		// Move to the next batch if it's time to do so
		if (updateTimer > currentParams.batchTime) {

			CurrentBatch++;

			// Move the snakes back to their starting positions
			ResetSnakes();

			for (int i = 0; i < penManagerScripts.Count; i++) {
				penManagerScripts[i].currentSpawnPosition = 0;
			}

			// Reset the flies each time the frogs are reset so that we don't just end up
			// with the hard-to-reach flies
			GameObject[] flies = GameObject.FindGameObjectsWithTag("Fly");
			foreach (GameObject fly in flies) {
				Destroy(fly);
			}

			// Loop over the last neural nets used and store their fitnesses on the nets themselves.
			// This is necessary because the frogs are about to have their scores reset.

			int resetCount = 0; // If a neural net performed badly enough then just randomise its weights again

			for (int i = 1; i <= frogs.Count; i++) {

				// We have to count backwards from currentPopIndex using modular arithmetic to find the neural nets just used
				int popIndex = (currentPopIndex - i + populationSize) % populationSize;
				NeuralNet net = population[popIndex];
				PlayerInfo frogInfo = net.ParentFrog.GetComponent<PlayerInfo>(); // This is rarely called (certainly not every frame) so the GetComponent is OK

				// There was a lot of experimenting with the fitness functions!
				//net.fitness = 1.0f * (float)(frogInfo.score) + (currentParams.batchTime - 7.5f * (float)(frogInfo.DamageTaken)) + 0.25f * net.snakeDistScore;
				//net.fitness = 1.0f * (float)(frogInfo.score) + (currentParams.batchTime - 7.5f * (float)(frogInfo.DamageTaken)) + 0.002f * net.waterScore;
				//net.fitness = 1.0f * (float)(frogInfo.score) + (currentParams.batchTime - 7.5f * (float)(frogInfo.DamageTaken));
				net.fitness = 1.0f * (float)(frogInfo.score) + (currentParams.batchTime - 7.5f * (float)(frogInfo.DamageTaken)) + Mathf.Min(0.0f, 0.5f * (5.0f - net.waterCampingScore));

				//Debug.Log("Fitness is " + net.fitness + ", water score is " + (0.004f * net.waterScore) + ", health score is " + (currentParams.batchTime - 7.5f * (float)(frogInfo.DamageTaken)));
				//Debug.Log("Fitness is " + net.fitness + ", water camping score is " + (- 0.5f * net.waterCampingScore));

				net.waterScore = 0.0f;
				net.waterCampingScore = 0.0f;
				net.snakeDistScore = 0.0f;

				if (net.fitness <= currentParams.discardThreshold) {
					net.RandomiseWeights();
					resetCount++;
				}

				if (net.fitness > bestFitnessSoFar) {
					bestFitnessSoFar = net.fitness;
					bestNetSoFar = net;
				}
			}

			if (currentParams.verbose) {
				Debug.Log("Reset " + resetCount + " neural nets due to bad performance");
			}

			// If we've completed a batch then run the genetic algorithm thingy
			if (currentPopIndex == 0) {

				// Write the total population fitness, etc to the log file
				float totalPopFitness = 0.0f;
				for (int i = 0; i < fitness.Count; i++) {
					totalPopFitness += fitness[i];
				}
				WriteToLog("Epoch: " + CurrentEpoch + ", population fitness: " + totalPopFitness + ", best fitness so far: " + bestFitnessSoFar);

				RunEpoch();
				CurrentEpoch++;
				CurrentBatch = 0;
				SavePopulation();

				// TO DO: Should probably replace these magic numbers
				// 100 means that there are 100 random fly positions created by default,
				// which are shared across pens so no frog gets an unfair advantage.
				// Any penManagerScript could be used for this call since the function is static.
				penManagerScripts[0].ResetSpawnPositions(100);
			}

			for (int i = 0; i < penManagers.Count; i++) {

				// Move the frog back to its start position
				frogs[i].transform.position = penManagerScripts[i].frogHome.transform.position;

				frogs[i].GetComponent<NeuralNetSteering>().neuralNet = population[currentPopIndex];
				frogs[i].GetComponent<NeuralNetSteering>().neuralNet.UpdateWeightsAsVector();
				frogs[i].GetComponent<NeuralNetSteering>().manager = penManagerScripts[i];
				population[currentPopIndex].ParentFrog = frogs[i];

				// Ensure that the snakes are targeting the right frogs
				foreach (GameObject snake in penManagerScripts[i].snakes) {
					if (snake != null) {
						snake.GetComponent<GameObjectTargeter>().Target = frogs[i];
						snake.GetComponent<HuntTargeter>().Target = frogs[i];
						snake.GetComponent<PredatorStateMachine>().Player = frogs[i];
						snake.transform.position = new Vector3(snake.GetComponent<PredatorStateMachine>().Home.transform.position.x,
						                                       snake.GetComponent<PredatorStateMachine>().Home.transform.position.y,
						                                       snake.transform.position.z);
					}
				}

				// Find a new fly to select on the next update
				frogs[i].GetComponent<NeuralNetSteering>().selectedFly = null;

				// Reset the frog's score
				frogPlayerInfo[i].Reset();

				IncrementPopulationIndex();
			}

			updateTimer = 0.0f;
		}
	}

	public override NeuralNet SelectParent() {

		switch (currentParams.parentSelectionMode) {

			case ParentSelectionMode.Proportional:
				if (currentParams.verbose) {
					Debug.Log("Proportional Selected.");
				}
				return SelectParentProportional();

			case ParentSelectionMode.Exponential:
				if (currentParams.verbose) {
					Debug.Log("Exponential Selected.");
				}
				return SelectParentExponential();

			case ParentSelectionMode.Tournament:
				if (currentParams.verbose) {
					Debug.Log("Tournament Selected.");
				}
				return SelectParentTournament(2); // Binary tournament

			case ParentSelectionMode.RankRoulette:
				if (currentParams.verbose) {
					Debug.Log("RankRoulette Selected.");
				}
				return SelectParentRankRoulette(1.8f); // 2 >= sp >= 1

			default:
				if (currentParams.verbose) {
					Debug.Log("Proportional Selected.");
				}		
				return SelectParentProportional();
		}
	}
	
	private NeuralNet SelectParentProportional() {

		float sumFitness = 0.0f;
		float maxFitness = float.MinValue;
		float minFitness = float.MaxValue;

		// Find the maximum and minimum fitnesses
		for (int i = 0; i < fitness.Count; i++) {
			if (fitness[i] > maxFitness) {
				maxFitness = fitness[i];
			}
			if (fitness[i] < minFitness) {
				minFitness = fitness[i];
			}
		}

		// Ensure that all fitnesses are positive
		if (minFitness < 0.0f) {

			maxFitness -= minFitness;

			for (int i = 0; i < fitness.Count; i++) {
				fitness[i] -= minFitness;
			}
		}

		// Calculate the total population's fitness
		for (int i = 0; i < fitness.Count; i++) {
			sumFitness += Mathf.Max(fitness[i] - maxFitness * currentParams.propSelectionAccentuation, 0.0f);
		}

		// Just return a random frog if there were no flies caught
		if (sumFitness == 0.0f) {
			return CopyChromosome(population[Random.Range(0, population.Count)]);
		}
		
		// Weight the change of a frog being chosen based on its fitness
		float cumuFitness = 0.0f;
		float threshold = Random.Range(0.0f, sumFitness);
		
		for (int i = 0; i < fitness.Count; i++) {

			cumuFitness += Mathf.Max(fitness[i] - maxFitness * currentParams.propSelectionAccentuation, 0.0f);

			if (cumuFitness >= threshold) {
				if (currentParams.verbose) {
					Debug.Log("Selected parent " + i + ", fitness = " + fitness[i]);
				}
				return CopyChromosome(population[i]);
			}
		}
		
		// Should never reach this point
		return null; 
	}

	private NeuralNet SelectParentExponential() {

		float sumFitness = 0.0f;
		float minFitness = float.MaxValue;
		
		// Find the minimum fitness
		for (int i = 0; i < fitness.Count; i++) {
			if (fitness[i] < minFitness) {
				minFitness = fitness[i];
			}
		}

		// Ensure that all fitnesses are positive
		if (minFitness < 0.0f) {
			for (int i = 0; i < fitness.Count; i++) {
				fitness[i] -= minFitness;
			}
		}

		for (int i = 0; i < fitness.Count; i++) {
			sumFitness += Mathf.Exp(fitness[i] * currentParams.expSelectionAccentuation);
		}

		// Just return a random frog if there were no flies caught
		if (sumFitness == 0.0f) {
			return CopyChromosome(population[Random.Range(0, population.Count)]);
		}

		// Weight the change of a frog being chosen based on its fitness
		float cumuFitness = 0.0f;
		float threshold = Random.Range(0.0f, sumFitness);

		for (int i = 0; i < fitness.Count; i++) {
			cumuFitness += Mathf.Exp(fitness[i] * currentParams.expSelectionAccentuation);
			if (cumuFitness >= threshold) {
				if (currentParams.verbose) {
					Debug.Log("Selected parent with fitness = " + fitness[i]);
				}
				return CopyChromosome(population[i]);
			}
		}

		// Should never reach this point
		return null; 
	}
	
	// Jayden: If I understood this selection method it's quite straightforward.
	// Pick x individuals from population N and the one with the best fitness
	// goes into the new population. Rinse and repeat. 
	private NeuralNet SelectParentTournament(int tournamentSize) {

		// We can just store the index of the competitors.
		int[] tournamentPopulation = new int[tournamentSize];

		// Create the tournament pool of competitors.
		for (int i = 0; i < tournamentSize; ++i) {
			int randIndex = (int)(Random.value * tournamentSize);
			tournamentPopulation[i] = randIndex;
		}

		int bestIndex = tournamentPopulation[0];
		// Find the competitor with the best fitness, they win!
		foreach (int i in tournamentPopulation) {
			if (fitness[i] > fitness[bestIndex]) {
				bestIndex = i;
			}
		}
		
		return CopyChromosome(population[bestIndex]);
	}

	// Jayden: And here is the rank-based roulette wheel method.
	// This one is supposed to find a higher quality solution
	// but takes longer to converge.
	private NeuralNet SelectParentRankRoulette(float selectivePressure) {
		
		// We want to store a sorted list of indexes based on rank.
		int[] sortedPop = new int[populationSize];
		// Scaled rank values for the sorted population.
		float[] scaledRank = new float[populationSize];
		//int bestIndex = 0;

		// O(n^2) sort, if it's too slow I'll change it.
		List<float> copyFitness = new List<float>(fitness);

		for (int i = 0; i < populationSize; i++) {
			sortedPop[i] = i;
		}

		float tempValue;
		int tempIndex;

		// Insertion sort
		// Rank the LOWEST fitness first because it should get the worst scaled rank
		for (int i = 0; i < (populationSize - 1); i++) {

			for (int j = i + 1; j < populationSize; j++) {

				if (copyFitness[j] < copyFitness[i]) {

					// Swap values
					tempValue = copyFitness[i];
					copyFitness[i] = copyFitness[j];
					copyFitness[j] = tempValue;

					// Swap indices
					tempIndex = sortedPop[i];
					sortedPop[i] = sortedPop[j];
					sortedPop[j] = tempIndex;
				}
			}
		}

		// Scale the rank according to the selective pressure parameter (2 >= SP >= 1).
		for (int i = 0; i < populationSize; ++i) {
			// From the paper: Genetic Algorithm Performance with Different Selection Strategies in Solving TSP
			// Link: http://www.iaeng.org/publication/WCE2011/WCE2011_pp1134-1139.pdf
			scaledRank[i] = 2 - selectivePressure + ( 2 * (selectivePressure - 1) * ( (i - 1) / (populationSize - 1f) ) );
			//Debug.Log(i + ", " + scaledRank[i] + " fitness: " + fitness[sortedPop[i]]);
		}

		// Get the sum of the ranks i.e. sumFitness.
		float sumFitness = 0f;
		for (int i = 0; i <populationSize; ++i)
			sumFitness += scaledRank[i];

		// Now use a standard roulette wheel selection method using the scaled ranks.
		float cumuFitness = 0f;
		float threshold = Random.Range(0f, sumFitness);

		for (int i = 0; i < populationSize; ++i) {
			cumuFitness += scaledRank[i];
			if(cumuFitness >= threshold) {
				//Debug.Log("Rank selected was " + sortedPop[i]);
				return CopyChromosome(population[sortedPop[i]]);
			}
		}

		return null;
	}

	public override float CalcFitness(NeuralNet chromosome) {

		return chromosome.fitness;
	}

	// Now follows the advice from the bottom of page 258 in the "AI Techniques for Game Programming" book.
	public override NeuralNet[] CrossOver(NeuralNet parent1, NeuralNet parent2) {

		NeuralNet child1 = (NeuralNet)(parent1.Clone());
		NeuralNet child2 = (NeuralNet)(parent2.Clone());

		int crossOverPoint = child1.GetRandomCrossOverIndex();
		float tempWeight;

		//Debug.Log("Crossover index chosen was " + crossOverPoint);

		for (int i = 0; i < child1.weightsAsVector.Count; i++) {

			if (i >= crossOverPoint) {
				tempWeight = child1.weightsAsVector[i];
				child1.weightsAsVector[i] = child2.weightsAsVector[i];
				child2.weightsAsVector[i] = tempWeight;
			}
		}

		// Apply the changes to the actual neural nets
		child1.LoadFromWeightsAsVector();
		child2.LoadFromWeightsAsVector();

		return new NeuralNet[]{child1, child2};
	}
	
	public override void Mutate(NeuralNet chromosome) {

		float perturbationAmount = 0.2f;

		for (int i = 0; i < chromosome.weightsAsVector.Count; i++) {

			if (Random.Range(0.0f, 1.0f) <= mutationRate) {
				
				chromosome.weightsAsVector[i] += Random.Range(-1.0f, 1.0f) * perturbationAmount;
				
				if (currentParams.verbose) {
					Debug.Log("Mutated weight " + i + " to " + chromosome.weightsAsVector[i]);
				}
			}
		}

		// Apply the changes to the actual neural net
		chromosome.LoadFromWeightsAsVector();
	}

	public override NeuralNet CopyChromosome(NeuralNet chromosome) {
		return (NeuralNet)(chromosome.Clone());
	}
}
