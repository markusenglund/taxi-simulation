using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class StatisticsUtils : MonoBehaviour
{
    private static double GetStandardNormalVariable()
    {
        System.Random random = new System.Random();

        double u1 = 1 - random.NextDouble();
        double u2 = 1 - random.NextDouble();

        double y1 = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);

        return y1;
    }

    public static float GetRandomFromNormalDistribution(
        float mean,
        float standardDeviation,
        float min = float.NegativeInfinity,
        float max = float.PositiveInfinity
    )
    {

        double standardNormalVariable = GetStandardNormalVariable();
        double value = standardNormalVariable * standardDeviation + mean;

        if (value < min || value > max)
        {
            return GetRandomFromNormalDistribution(mean, standardDeviation, min, max);
        }

        return (float)value;
    }

    public static float getRandomFromLogNormalDistribution(
        float mu, float sigma
    )
    {
        double standardNormalVariable = GetStandardNormalVariable();
        double logNormalVariable = Math.Exp(mu + sigma * standardNormalVariable);

        return (float)logNormalVariable;
    }
}
