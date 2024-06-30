using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using Random = System.Random;

public class StatisticsUtils : MonoBehaviour
{
    private static double GetStandardNormalVariable(Random random)
    {
        double u1 = 1 - random.NextDouble();
        double u2 = 1 - random.NextDouble();

        double standardNormalVariable = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);

        return standardNormalVariable;
    }

    public static float GetRandomFromNormalDistribution(
        Random random,
        float mean,
        float standardDeviation,
        float min = float.NegativeInfinity,
        float max = float.PositiveInfinity
    )
    {

        double standardNormalVariable = GetStandardNormalVariable(random);
        double value = standardNormalVariable * standardDeviation + mean;

        if (value < min || value > max)
        {
            return GetRandomFromNormalDistribution(random, mean, standardDeviation, min, max);
        }

        return (float)value;
    }

    public static float getRandomFromLogNormalDistribution(Random random,
        float mu, float sigma
    )
    {
        double standardNormalVariable = GetStandardNormalVariable(random);
        double logNormalVariable = Math.Exp(mu + sigma * standardNormalVariable);

        return (float)logNormalVariable;
    }

    public static float CalculateMedian(List<float> values)
    {
        List<float> sortedList = values.OrderBy(x => x).ToList();
        int count = sortedList.Count;
        if (count % 2 == 0)
        {
            return (sortedList[count / 2 - 1] + sortedList[count / 2]) / 2;
        }
        else
        {
            return sortedList[count / 2];
        }
    }
}
