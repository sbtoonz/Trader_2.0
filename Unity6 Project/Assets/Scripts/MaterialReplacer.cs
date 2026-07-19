using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialReplacer : MonoBehaviour
{
    private void Awake()
    {
        foreach (var VARIABLE in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (VARIABLE.name == "InteriorEnvirnomentZone")
            {
                var component = gameObject.GetComponent<Renderer>();
                component = (Renderer)VARIABLE.GetComponents<Renderer>().Clone();
            }
        }
    }
}
