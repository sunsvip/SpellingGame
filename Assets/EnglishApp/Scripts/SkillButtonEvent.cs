using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public class SkillButtonEvent : MonoBehaviour, IPointerDownHandler, IPointerUpHandler{
    public UnityEvent onButtonDown = new UnityEvent();
    public UnityEvent onButtonUp = new UnityEvent();

    private Button bt = null;
    private void Start()
    {
        bt = this.GetComponent<Button>();
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        if (bt.interactable)
        {
            onButtonDown.Invoke();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (bt.interactable)
        {
            onButtonUp.Invoke();
        }
    }
}
