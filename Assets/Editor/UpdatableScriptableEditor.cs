using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UpdatableScriptable), true)]
public class UpdatableScriptableEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        UpdatableScriptable obj = (UpdatableScriptable)target;
        if (GUILayout.Button("Update"))
        {
            obj.NotifyOfUpdate();
            EditorUtility.SetDirty(target);
        }
    }
}
