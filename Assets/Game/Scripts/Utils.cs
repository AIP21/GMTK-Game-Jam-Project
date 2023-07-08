using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Utils : MonoBehaviour
{
    /**
    <Summary> Returns a random float between min and max, but weighted so that they are closer to the target value. </Summary>
    **/
    public static float WeightedRandom(float min, float max, float target, float weightingStrength = 1f)
    {
        float random = UnityEngine.Random.Range(min, max);
        float weightedRandom = (random + target * weightingStrength) / (1 + weightingStrength);
        return weightedRandom;
    }

    /**
    <Summary> Returns a random integer between min and max, but weighted so that they are closer to the target value. </Summary>
    **/
    public static int WeightedRandom(int min, int max, int target, float weightingStrength = 1f)
    {
        int random = UnityEngine.Random.Range(min, max);
        int weightedRandom = (int)((random + target * weightingStrength) / (1 + weightingStrength));
        return weightedRandom;
    }
}
