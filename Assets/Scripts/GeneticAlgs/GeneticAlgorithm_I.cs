using UnityEngine;
using System.Collections;

public interface GeneticAlgorithm_I<T>
{
	void InitPopulation();
	void SetIndividual(int index, T value);
	T SelectParent();
	float CalcFitness(T chromosome);
	T[] CrossOver(T parent1, T parent2);
	void Mutate(T chromosome);
	void RunEpoch();
}
