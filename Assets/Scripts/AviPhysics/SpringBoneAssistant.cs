﻿using System.Collections;
using System.Collections.Generic;
using UnityChan;
using UnityEngine;

public class SpringBoneAssistant : MonoBehaviour
{
    public UnityChan.SpringManager mSpringManager;

    public Vector3 mTargetBoneAxis = new Vector3(0f, 1f, 0f);

    public void MarkChildren()
    {
        //List<SpringBoneMarker> boneMarkers = new List<SpringBoneMarker>();

        // get spring bones set
        SpringBoneMarker[] boneMarkers = FindObjectsOfType<SpringBoneMarker>();

        // mark children
        for (int i = 0; i < boneMarkers.Length; i++)
        {
            boneMarkers[i].MarkChildren();
        }

        // get again, will include children
        boneMarkers = FindObjectsOfType<SpringBoneMarker>();
        List<UnityChan.SpringBone> springBones = new List<UnityChan.SpringBone> { };
        for (int i = 0; i < boneMarkers.Length; i++)
        {
            // add spring bone
            springBones.Add(boneMarkers[i].AddSpringBone());

            // set vector
            springBones[i].boneAxis = mTargetBoneAxis;

            // unmark object
            boneMarkers[i].UnmarkSelf();
        }

        // append to springbone list without replacing existing
        if(mSpringManager.springBones.Length == 0)
        {
            mSpringManager.springBones = springBones.ToArray();
        }
        else
        {
            List<SpringBone> newSpringBoneList = new List<SpringBone>();
            newSpringBoneList.AddRange(mSpringManager.springBones);
            newSpringBoneList.AddRange(springBones);
            mSpringManager.springBones = newSpringBoneList.ToArray();
        }     
    }

    public void CleanUp()
    {
        SpringBoneMarker[] boneMarkers = FindObjectsOfType<SpringBoneMarker>();
        UnityChan.SpringBone[] springBones = FindObjectsOfType<UnityChan.SpringBone>();

        for (int i = 0; i < boneMarkers.Length; i++)
        {
            DestroyImmediate(boneMarkers[i]);

        }
        for (int i = 0; i < springBones.Length; i++)
        {
            DestroyImmediate(springBones[i]);
        }

    }


}
