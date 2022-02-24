using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
 
public class Dragscript : MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler {
 
    public static Vector3 move;
    Vector3 initialpos;
    Vector3 distance;
    float speed=0.2f;
    void Start()
    {
        move = Vector3.zero;
    }
 
    #region IBeginDragHandler implementation
    public void OnBeginDrag (PointerEventData eventData)
    {
        initialpos = transform.position;
        move = Vector3.zero;
 
    }
    #endregion
 
    #region IDragHandler implementation
 
    public void OnDrag (PointerEventData eventData)
    {
        if (Input.mousePosition.x < Screen.width / 2 && Input.mousePosition.y < Screen.height/2) {
            distance = Input.mousePosition - initialpos;
            distance = Vector3.ClampMagnitude (distance, 45 * Screen.width / 708);
            transform.position = initialpos + distance;
            Vector3 move1 = distance.normalized;
            move.x = move1.x * speed;
            move.z = move1.y * speed;
        }
 
    }
 
    #endregion
 
    #region IEndDragHandler implementation
 
    public void OnEndDrag (PointerEventData eventData)
    {
        move = Vector3.zero;
        transform.position = initialpos;
    }
 
 
    #endregion
 
}