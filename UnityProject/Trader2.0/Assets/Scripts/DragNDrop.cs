using UnityEngine;
using UnityEngine.EventSystems;
public class DragNDrop : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public Transform target;
    public bool shouldReturn;
    private bool isMouseDown;
    private Vector3 startMousePosition;
    private Vector3 startPosition;

    internal static bool donedrag;
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
        donedrag = isMouseDown;
        Debug.Log("Draggable Mouse Down");

        startPosition = target.position;
        startMousePosition = Input.mousePosition;
    }

    public void OnPointerUp(PointerEventData dt)
    {
        Debug.Log("Draggable mouse up");

        isMouseDown = false;

        donedrag = isMouseDown;
        if (shouldReturn) target.position = startPosition;
    }
}
