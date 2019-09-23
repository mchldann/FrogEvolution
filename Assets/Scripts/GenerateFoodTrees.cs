using UnityEngine;
using System.Collections;
using System.Collections.Generic;


// Tightly coupled to this class.
public struct Trees
{
	public List<Vector2> positions;
	public List<Vector2> flowerPositions;

	public Trees Clone()
	{
		Trees t = new Trees();
		t.positions = new List<Vector2>();
		t.flowerPositions = new List<Vector2>();

		foreach (Vector2 position in positions) {
			t.positions.Add(new Vector2(position.x, position.y));
		}
		
		foreach (Vector2 position in flowerPositions) {
			t.flowerPositions.Add(new Vector2(position.x, position.y));
		}

		return t;
	}
}


public class GenerateFoodTrees : GAController<Trees>
{
	public GameObject lake1, lake2;
	public float vectorMutate;
	public int runs;
	public int numTrees, numFlowers;
	public float minxBoundary, maxxBoundary;
	public float minyBoundary, maxyBoundary;
	public GameObject appleTreePrefab, flowerTreePrefab, flower1Prefab, flower2Prefab, flower3Prefab;
	public GameObject home, flowerParent;
	public float requiredDistanceFromHome, distanceFromNeighbours;
	private Trees fitTrees;

	public GenerateFoodTrees() : base(50, 0.001f, 0.7f) {}

	void Awake()
	{
		// Run the GA and get a layout of tree positions for us to use.
		InitPopulation();

		for (int i = 0; i < runs; ++i){
			RunEpoch();
		}

		fitTrees = ReturnFittest();

		// Instantiate the trees in the positions calculated by the GA.
		for(int i = 0; i < fitTrees.positions.Count; ++i) {
			if (Random.value < 0.5) {
				GameObject tree = (GameObject)Instantiate(appleTreePrefab, new Vector3(fitTrees.positions[i].x, fitTrees.positions[i].y, transform.position.z), Quaternion.identity);
				tree.transform.parent = transform;
			} else {
				GameObject tree = (GameObject)Instantiate(flowerTreePrefab, new Vector3(fitTrees.positions[i].x, fitTrees.positions[i].y, transform.position.z), Quaternion.identity);
				tree.transform.parent = transform;
			}
		}

		// Instantiate the flowers in the positions calculated by the GA.
		for(int i = 0; i < fitTrees.flowerPositions.Count; ++i) {
			float rand = Random.value;
			if (rand < 0.3) {
				GameObject tree = (GameObject)Instantiate(flower1Prefab, new Vector3(fitTrees.flowerPositions[i].x, fitTrees.flowerPositions[i].y, flowerParent.transform.position.z), Quaternion.identity);
				tree.transform.parent = flowerParent.transform;
			} else if (rand < 0.6) {
				GameObject tree = (GameObject)Instantiate(flower2Prefab, new Vector3(fitTrees.flowerPositions[i].x, fitTrees.flowerPositions[i].y, flowerParent.transform.position.z), Quaternion.identity);
				tree.transform.parent = flowerParent.transform;
			} else {
				GameObject tree = (GameObject)Instantiate(flower3Prefab, new Vector3(fitTrees.flowerPositions[i].x, fitTrees.flowerPositions[i].y, flowerParent.transform.position.z), Quaternion.identity);
				tree.transform.parent = flowerParent.transform;
			}
		}
	}


	// Randomly initalise the population by putting trees anywhere within 
	// the specified boundaries.
	public override void InitPopulation()
	{
		population = new List<Trees>();

		for (int i = 0; i < populationSize; ++i) {
			
			Trees t = new Trees();
			t.positions = new List<Vector2>();
			t.flowerPositions = new List<Vector2>();

			population.Insert(i, t);
			
			for (int j = 0; j < numTrees; ++j) {
				population[i].positions.Add(new Vector2(Random.Range(minxBoundary, maxxBoundary), Random.Range(minyBoundary, maxyBoundary))); 
			}

			for (int j = 0; j < numFlowers; ++j) {
				population[i].flowerPositions.Add(new Vector2(Random.Range(minxBoundary, maxxBoundary), Random.Range(minyBoundary, maxyBoundary))); 
			}
		}
	}
	

	// TO DO: Move the tournament selection to the GAController abstract interface 
	// so it can be shared between this and the frog.
	public override Trees SelectParent()
	{
		int tournamentSize = populationSize / 2;
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
	

	// Fitness calculation.
	public override float CalcFitness(Trees chromosome)
	{
		float sumDistFromGoal = 0f;
		float sumDistFromOthers = 0f;
		float flowerScore = 1f;

		// We want to loop through each tree position and
		// compare its distance to every other tree position and goal tree.
		for (int i = 0; i < chromosome.positions.Count; ++i) {
			
			for (int j = 0; j < chromosome.positions.Count; ++j) {
				
				// Skip this iteration as we don't want to compare the tree
				// position to itself.
				if (i == j)
					continue;

				// If these two tree are really close, instead halve the fitness
				if (Vector2.Distance(chromosome.positions[j], chromosome.positions[i]) < 2)
					sumDistFromOthers *= 0.5f;
				else
					sumDistFromOthers += Vector2.Distance(chromosome.positions[j], chromosome.positions[i]);
			}

			// If outside boundary return 0 as this is a solution we never want.
			if (chromosome.positions[i].x > maxxBoundary || chromosome.positions[i].x < minxBoundary ||
				chromosome.positions[i].y > maxyBoundary || chromosome.positions[i].y < minyBoundary) {
					return 0;
			// If inside a lake we dont want it either.
			} else if ( (Vector2.Distance(lake1.transform.position, chromosome.positions[i]) < 1f) || 
				        (Vector2.Distance(lake2.transform.position, chromosome.positions[i]) < 1f) ) {
					return 0;
			} else {
				sumDistFromGoal += Vector2.Distance(Vector2.zero, chromosome.positions[i]);
			}

			float distToOtherFlowers = 0f; 
			// Now compute the flower fitness value
			for (int j = 0; j < chromosome.flowerPositions.Count; ++j) {

				// If outside boundary return 0 as this is a solution we never want.
				if (chromosome.flowerPositions[j].x > maxxBoundary || chromosome.flowerPositions[j].x < minxBoundary ||
					chromosome.flowerPositions[j].y > maxyBoundary || chromosome.flowerPositions[j].y < minyBoundary) {
					return 0;
				// If inside a lake we dont want it either.
				} else if ( (Vector2.Distance(lake1.transform.position, chromosome.flowerPositions[j]) < 1f) || 
				        	(Vector2.Distance(lake2.transform.position, chromosome.flowerPositions[j]) < 1f) ) {
					return 0;
				} 
				
				float distToTree = Vector2.Distance(chromosome.positions[i], chromosome.flowerPositions[j]);
				if (distToTree < 3f && distToTree > 1.5f)
					flowerScore += 100f; // Just an arbitary constant for now.
				else
					flowerScore -= 100f; // Punishment...

				for (int flowerPos = 0; flowerPos < chromosome.flowerPositions.Count; ++flowerPos) {
					if(Vector2.Distance(chromosome.flowerPositions[flowerPos], chromosome.flowerPositions[j]) < 1) {
						break; // This is going to result in a lower fitness value in the long run.
					} else {
						distToOtherFlowers += Vector2.Distance(chromosome.flowerPositions[flowerPos], chromosome.flowerPositions[j]);
						distToOtherFlowers += Vector2.Distance(Vector2.zero, chromosome.flowerPositions[flowerPos]); // dist from middle should be maximised
					}
				}
			}

			flowerScore += distToOtherFlowers;
		}

		return sumDistFromGoal + sumDistFromOthers + flowerScore;
	}
	

	// Do a simple crossover where child1 and child2 keep the
	// first half of their respective parents set of positions, 
	// but the second half is inherited from the other parent.
	// i.e. child2 = [half parent 2 | half parent 1]
	public override Trees[] CrossOver(Trees parent1, Trees parent2)
	{
		// First calculate the midpoint for the tree positions.
		int midPoint = parent1.positions.Count / 2;
		
		Trees[] children = new Trees[2];
		children[0].positions = new List<Vector2>();
		children[1].positions = new List<Vector2>();

		for(int i = 0; i < midPoint; ++i) {
			children[0].positions.Add(parent1.positions[i]);
			children[1].positions.Add(parent2.positions[i]);
		}

		for (int i = midPoint; i < parent1.positions.Count; ++i) {
			children[0].positions.Add(parent2.positions[i]);
			children[1].positions.Add(parent1.positions[i]);
		}

		// Now do the same for the flowers.
		midPoint = parent1.flowerPositions.Count / 2;

		children[0].flowerPositions = new List<Vector2>();
		children[1].flowerPositions = new List<Vector2>();

		for(int i = 0; i < midPoint; ++i) {
			children[0].flowerPositions.Add(parent1.flowerPositions[i]);
			children[1].flowerPositions.Add(parent2.flowerPositions[i]);
		}

		for (int i = midPoint; i < parent1.flowerPositions.Count; ++i) {
			children[0].flowerPositions.Add(parent2.flowerPositions[i]);
			children[1].flowerPositions.Add(parent1.flowerPositions[i]);
		}

		return children;
	}
	

	public override void Mutate(Trees chromosome)
	{
		// Loop through each position
		for (int i = 0; i < chromosome.positions.Count; ++i) {
			
			float x = chromosome.positions[i].x;
			float y = chromosome.positions[i].y;

			// Mutate x pos
			if(Random.value < mutationRate) {
				x = Random.Range(x - vectorMutate, x + vectorMutate);
			}

			// Mutate y pos
			if(Random.value < mutationRate) {
				y = Random.Range(y - vectorMutate, y + vectorMutate);
			}

			chromosome.positions[i] = new Vector2(x, y);
		}

		// Loop through each flower position
		for (int i = 0; i < chromosome.flowerPositions.Count; ++i) {
			
			float x = chromosome.flowerPositions[i].x;
			float y = chromosome.flowerPositions[i].y;

			// Mutate x pos
			if(Random.value < mutationRate) {
				x = Random.Range(x - vectorMutate, x + vectorMutate);
			}

			// Mutate y pos
			if(Random.value < mutationRate) {
				y = Random.Range(y - vectorMutate, y + vectorMutate);
			}

			chromosome.flowerPositions[i] = new Vector2(x, y);
		}
	}


	public override Trees CopyChromosome(Trees chromosome)
	{
		return chromosome.Clone();
	}
}
