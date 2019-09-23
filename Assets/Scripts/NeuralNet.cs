using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class NeuralNet : System.ICloneable {

	public enum InputTransformation
	{
		Normalise = 0,
		RotationSmoothing = 1,
		None = 2
	};

	[HideInInspector]
	[System.NonSerialized]
	public GameObject ParentFrog;

	[HideInInspector]
	public float fitness = 0.0f;

	[HideInInspector]
	public float snakeDistScore = 0.0f;

	[HideInInspector]
	public float waterScore = 0.0f;

	[HideInInspector]
	public float waterCampingScore = 0.0f;
	
	public int inputNeurons;
	public int hiddenLayerNeurons;
	public int outputNeurons;

	// Input settings
	public int NumFlyPositions = 2;
	public int NumSnakePositions = 1;
	public int NumObstaclePositions = 2;
	public bool FeedObstacleInfo = true;
	public bool FeedOwnVelocity = true;
	public bool FeedLakePosition = true;
	public bool FeedWaterLevel = true;
	public InputTransformation inputTransformation;
	public int inputSmoothingSegments = 30;
	
	public float defaultInputExponent = 1.0f;
	public float defaultOutputExponent = 1.0f;
	
	public float[][] neuronValues = new float[3][]; // For storing calculated values
	public float[][] weights = new float[2][];

	//[HideInInspector]
	public float shotDistanceGene;

	public float shotDistanceMultiplier = 3.0f;
	
	public List<float> weightsAsVector; // Useful to have the weights in this format for mutating
	public List<int> crossOverPoints;
	
	private float previousRotation = 0.0f;

	public float getShotDistance() {
		return shotDistanceGene * shotDistanceMultiplier;
	}
	
	public System.Object Clone() {
		
		NeuralNet clone = new NeuralNet(NumFlyPositions, NumSnakePositions, NumObstaclePositions, FeedObstacleInfo, FeedOwnVelocity,
		                                FeedLakePosition, FeedWaterLevel, hiddenLayerNeurons, inputTransformation, inputSmoothingSegments);

		clone.ParentFrog = ParentFrog;
		clone.defaultInputExponent = defaultInputExponent;
		clone.defaultOutputExponent = defaultOutputExponent;
		clone.weights = new float[][]{(float[])(weights[0].Clone()), (float[])(weights[1].Clone())}; // Deep copy!
		clone.shotDistanceGene = shotDistanceGene;
		clone.UpdateWeightsAsVector();
		clone.CalculateCrossOverIndices();

		return (System.Object)clone;
	}

	// The "weightsAsVector" is what actually gets mutated / crossed over.
	// This method loads the weights back into the actual neural net.
	public void LoadFromWeightsAsVector() {

		int counter = 0;

		for (int i = 0; i < weights[0].Length; i++) {
			weights[0][i] = weightsAsVector[counter];
			counter++;
		}

		for (int i = 0; i < weights[1].Length; i++) {
			weights[1][i] = weightsAsVector[counter];
			counter++;
		}

		shotDistanceGene = weightsAsVector[counter];
		counter++;
	}

	// This method updates the vector of weights from the actual neural net
	public void UpdateWeightsAsVector() {
		
		weightsAsVector = new List<float>();
		
		for (int i = 0; i < weights[0].Length; i++) {
			weightsAsVector.Add(weights[0][i]);
		}
		for (int i = 0; i < weights[1].Length; i++) {
			weightsAsVector.Add(weights[1][i]);
		}

		weightsAsVector.Add(shotDistanceGene);
	}
	
	public void RandomiseWeights() {
		
		weights[0] = new float[(inputNeurons + 1) * hiddenLayerNeurons]; // +1 for the exponent
		weights[1] = new float[(hiddenLayerNeurons + 1) * outputNeurons]; // +1 for the exponent
		
		// Hidden layer
		for (int i = 0; i < hiddenLayerNeurons; i++) {
			
			weights[0][i * (inputNeurons + 1)] = defaultInputExponent; // Exponent
			
			for (int j = 0; j < inputNeurons; j++) {
				weights[0][i * (inputNeurons + 1) + j + 1] = Random.value - 0.5f;
			}
		}
		
		// Output
		for (int i = 0; i < outputNeurons; i++) {
			
			weights[1][i * (hiddenLayerNeurons + 1)] = defaultOutputExponent; // Exponent
			
			for (int j = 0; j < hiddenLayerNeurons; j++) {
				weights[1][i * (hiddenLayerNeurons + 1) + j + 1] = Random.value - 0.5f;
			}
		}

		// Start with a value between 0.5 and 1.5
		shotDistanceGene = 0.5f + Random.value;
		
		UpdateWeightsAsVector();
	}
	
	public NeuralNet(int NumFlyPositions, int NumSnakePositions, int NumObstaclePositions, bool FeedObstacleInfo, bool FeedOwnVelocity,
	                 bool FeedLakePosition, bool FeedWaterLevel, int hiddenLayerNeurons, InputTransformation inputTransformation, int inputSmoothingSegments) {

		this.NumFlyPositions = NumFlyPositions;
		this.NumSnakePositions = NumSnakePositions;
		this.NumObstaclePositions = NumObstaclePositions;
		this.FeedObstacleInfo = FeedObstacleInfo;
		this.FeedOwnVelocity = FeedOwnVelocity;
		this.FeedLakePosition = FeedLakePosition;
		this.FeedWaterLevel = FeedWaterLevel;
		this.inputNeurons = (NumFlyPositions + NumSnakePositions + NumObstaclePositions) * 2 + (FeedObstacleInfo ? 2 : 0) + (FeedOwnVelocity ? 2 : 0) + (FeedLakePosition ? 2 : 0) + (FeedWaterLevel ? 1 : 0);
		this.hiddenLayerNeurons = hiddenLayerNeurons;
		this.outputNeurons = 2;
		this.inputTransformation = inputTransformation;
		this.inputSmoothingSegments = inputSmoothingSegments;

		neuronValues = new float[3][];
		neuronValues[0] = new float[inputNeurons];
		neuronValues[1] = new float[hiddenLayerNeurons];
		neuronValues[2] = new float[outputNeurons];
		
		RandomiseWeights();

		CalculateCrossOverIndices();
	}

	// This method calculates the basic output from the neural net, given
	// an array of input values. It doesn't make use of any of the game world's symmetries.
	public float[] CalculateOutputNoSymmetry(float[] inputValues) {
	
		float[] result = new float[outputNeurons];
		
		if (inputValues.Length != inputNeurons) {
			Debug.Log("ERROR: Wrong number of inputs provided to neural net!");
			return result;
		}
		
		for (int i = 0; i < inputValues.Length; i++) {
			neuronValues[0][i] = inputValues[i];
		}
		
		// Hidden layer
		for (int i = 0; i < hiddenLayerNeurons; i++) {
			
			float exponent = weights[0][i * (inputNeurons + 1)];
			
			for (int j = 0; j < inputNeurons; j++) {
				neuronValues[1][i] += neuronValues[0][j] * weights[0][i * (inputNeurons + 1) + j + 1];
			}
			
			// Squash between -1 and 1
			neuronValues[1][i] = Squash(neuronValues[1][i], exponent);
		}
		
		// Output
		for (int i = 0; i < outputNeurons; i++) {
			
			float exponent = weights[1][i * (hiddenLayerNeurons + 1)];
			
			for (int j = 0; j < hiddenLayerNeurons; j++) {
				neuronValues[2][i] += neuronValues[1][j] * weights[1][i * (hiddenLayerNeurons + 1) + j + 1];
			}
			
			// Squash between -1 and 1
			neuronValues[2][i] = Squash(neuronValues[2][i], exponent);
		}
		
		return neuronValues[2];
	}

	// This method is the one actually called by the neural net steering behaviour.
	// It does make use of rotational symmetries.
	public float[] CalculateOutput(float[] inputValues) {

		float smoothingCutoff = 2.5f;

		float temp = Mathf.Rad2Deg * Mathf.Atan2(ParentFrog.GetComponent<Rigidbody2D>().velocity.y, ParentFrog.GetComponent<Rigidbody2D>().velocity.x);
		float frogRotation = previousRotation + (temp - previousRotation) * Mathf.Min(smoothingCutoff, ParentFrog.GetComponent<Rigidbody2D>().velocity.magnitude) / smoothingCutoff;
		frogRotation = previousRotation + 0.05f * (frogRotation - previousRotation);

		previousRotation = frogRotation;

		//Debug.DrawLine((Vector2)(ParentFrog.transform.position), (Vector2)(ParentFrog.transform.position) + MathsHelper.rotateVector(new Vector2(1.0f, 0.0f), frogRotation), Color.yellow);

		if (inputTransformation == InputTransformation.Normalise) {

			// Rotate the input to the frog's frame of reference
			// Don't rotate the water level since it's a scalar!
			for (int i = 0; i < (inputValues.Length - (FeedWaterLevel ? 1 : 0)); i += 2) {
				Vector2 unrotatedVec = new Vector2(inputValues[i], inputValues[i + 1]);
				//Debug.DrawLine((Vector2)(ParentFrog.transform.position), (Vector2)(ParentFrog.transform.position) + unrotatedVec, Color.cyan);
				Vector2 rotatedVec = MathsHelper.rotateVector(unrotatedVec, -frogRotation);
				inputValues[i] = rotatedVec.x;
				inputValues[i + 1] = rotatedVec.y;
			}

			float[] outputValues = CalculateOutputNoSymmetry(inputValues);

			// Rotate the output back to world co-ordinates
			for (int i = 0; i < outputValues.Length; i += 2) {
				Vector2 unrotatedVec = new Vector2(outputValues[i], outputValues[i + 1]);
				//Vector2 rotatedVec = MathsHelper.rotateVector(unrotatedVec, frogRotation * Mathf.Rad2Deg);
				Vector2 rotatedVec = MathsHelper.rotateVector(unrotatedVec, frogRotation);
				outputValues[i] = rotatedVec.x;
				outputValues[i + 1] = rotatedVec.y;
			}
			
			return outputValues;

		} else {

			// Since rotations shouldn't make any difference to the frog's behaviour, it's probably
			// a good idea to enforce this, which is what the following code does. The code assumes
			// that the input is in the form of a list of 2d vectors and that the output is a single
			// 2d vector. We found that normalising angles to the frog's frame of reference wasn't
			// as helpful (see our report).

			int numSegments = 1;

			if (inputTransformation == InputTransformation.RotationSmoothing) {
				numSegments = inputSmoothingSegments;
			}

			Vector2 output = Vector2.zero;

			for (int segment = 0; segment < numSegments; segment++) {

				// Rotate the input
				// Don't rotate the water level since it's a scalar!
				float[] rotatedInput = new float[inputValues.Length];
				for (int i = 0; i < (inputValues.Length - (FeedWaterLevel ? 1 : 0)); i += 2) {
					Vector2 vec = new Vector2(inputValues[i], inputValues[i + 1]);
					vec = MathsHelper.rotateVector(vec, (float)segment * 360.0f / (float)numSegments);
					rotatedInput[i] = vec.x;
					rotatedInput[i + 1] = vec.y;
				}

				// Set the water level
				if (FeedWaterLevel) {
					rotatedInput[rotatedInput.Length - 1] = inputValues[inputValues.Length - 1];
				}

				// Rotate the output back to the original frame of reference
				float[] rotatedOutput = CalculateOutputNoSymmetry(rotatedInput);
				Vector2 rotatedOutputVec = new Vector2(rotatedOutput[0], rotatedOutput[1]);
				Vector2 restoredOutputVec = MathsHelper.rotateVector(rotatedOutputVec, (float)segment * -360.0f / (float)numSegments);
				output += restoredOutputVec;
			}

			float[] outputValues = new float[] {output.x, output.y};
			
			return outputValues;
		}
	}

	// Calculate the points where we can actually crossover the network.
	// (Don't just split anywhere and risk messing up a neuron's weights.)
	public void CalculateCrossOverIndices() {

		crossOverPoints = new List<int>();
		int counter = 0;
		
		// Hidden layer
		for (int i = 0; i < hiddenLayerNeurons; i++) {
			crossOverPoints.Add(counter);
			counter += (inputNeurons + 1);
		}
		
		// Output
		for (int i = 0; i < outputNeurons; i++) {
			crossOverPoints.Add(counter);
			counter += (hiddenLayerNeurons + 1);
		}
		
		// Don't crossover right at the start because then it's not really a crossover
		crossOverPoints.Remove(0);
	}
	
	public int GetRandomCrossOverIndex() {
		int returnVal = crossOverPoints[Random.Range(0, crossOverPoints.Count)];
		//Debug.Log("Crossing over at index = " + returnVal);
		return returnVal;
	}
	
	private float Squash(float unsquashedValue, float exponent) {
		return 2.0f / (1.0f + Mathf.Exp(-unsquashedValue * exponent)) - 1.0f;
	}
}
