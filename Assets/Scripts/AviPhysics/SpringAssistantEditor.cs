#if UNITY_EDITOR // => Ignore from here to next endif if not in editor
using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(SpringBoneAssistant))]
public class SpringAssistantEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SpringBoneAssistant myScript = (SpringBoneAssistant)target;
        if (GUILayout.Button("Mark Children"))
        {
            myScript.MarkChildren();
        }
        if (GUILayout.Button("Clean Up"))
        {
            myScript.CleanUp();
        }
    }
}
#endif
