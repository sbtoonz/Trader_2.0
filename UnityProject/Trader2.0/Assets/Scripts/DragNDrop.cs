using UnityEngine;
using UnityEngine.EventSystems;
#nullable enable
public class DragNDrop : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public Transform target;
    public bool shouldReturn;
    private bool isMouseDown;
    private Vector3 startMousePosition;
    private Vector3 startPosition;
    Vector3 distance;

    private static bool _donedrag;

    private void Start()
    {
        target = transform;
    }

    private void Update()
    {
        if (isMouseDown)
        {
            var currentPosition = Input.mousePosition;

            var diff = currentPosition - startMousePosition;

            var pos = startPosition + diff;

            target.position = pos;
        }
    }


    public void OnPointerDown(PointerEventData dt)
    {
        isMouseDown = true;
        var position = target.position;
        //Trader20.Trader20.StoreScreenPos!.Value = position;
        _donedrag = isMouseDown;
        startPosition = position;
        startMousePosition = Input.mousePosition;
    }

    public void OnPointerUp(PointerEventData dt)
    {
        isMouseDown = false;
        //Trader20.Trader20.instance.SaveConfig();
        _donedrag = isMouseDown;
        if (shouldReturn) target.position = startPosition;
    }
}
