using BezierSolution;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetRailData : MonoBehaviour
{
    [SerializeField]
    BezierSpline railSpline;

    public BezierSpline GetMountedSpline()
    {
        return railSpline;
    }
}
