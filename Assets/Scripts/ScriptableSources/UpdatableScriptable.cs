using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdatableScriptable : ScriptableObject
{
    public System.Action OnValuesUpdated;
    public bool autoUpdate;

    #if UNITY_EDITOR
    public virtual void OnValidate()
    {
        if (autoUpdate)
        {
            UnityEditor.EditorApplication.update += NotifyOfUpdate;
        }
    }

    public void NotifyOfUpdate()
    {
        UnityEditor.EditorApplication.update -= NotifyOfUpdate;
        if(OnValuesUpdated != null)
        {
            OnValuesUpdated?.Invoke();
        }
    }
    #endif
}
