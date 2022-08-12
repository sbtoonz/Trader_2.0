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

        foreach (var image in Images!)
        {
            image.material = temp.FirstOrDefault();
            image.sprite = referenceimg!.sprite;
        }
    }
}
