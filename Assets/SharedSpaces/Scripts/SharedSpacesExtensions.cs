using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SharedSpacesExtensions
{
    public static bool IsCloseTo(this float a, float b, float epsilon = 0.0001f)
    {
        return Mathf.Abs(a - b) < epsilon;
    }
}
