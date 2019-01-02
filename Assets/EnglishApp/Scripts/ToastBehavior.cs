using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToastBehavior : MonoBehaviour
{
    [SerializeField]
    private Text uiText = null;
    public void Show(string tips, float lenTime)
    {
        if(null != uiText)
        {
            uiText.text = tips;
        }
        gameObject.SetActive(true);
        Destroy(gameObject, lenTime);
    }
}
