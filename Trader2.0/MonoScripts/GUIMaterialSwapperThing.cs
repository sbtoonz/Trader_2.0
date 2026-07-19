using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GUIMaterialSwapperThing : MonoBehaviour
{
    public Image[]? Images;

    public Image? referenceimg;
    private void Start()
    {
        var temp =Resources.FindObjectsOfTypeAll<Material>().Where(x => x.name == "litpanel");

        var enumerable = temp as Material[] ?? temp.ToArray();
        for (int i = 0; i < Images.Count(); i++)
        {
            Images[i].material = enumerable.ToArray().FirstOrDefault();
            
        }
        foreach (var image in Images!)
        {
            image.material = enumerable.FirstOrDefault();
            image.sprite = referenceimg!.sprite;
        }
    }
}
