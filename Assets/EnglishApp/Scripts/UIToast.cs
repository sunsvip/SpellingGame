using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class UIToast:MonoBehaviour
{
    public static string tipsPrefabName = "TipsWindow";
    public static void ShowTips(string tips, float time)
    {
        var prefab = Resources.Load<GameObject>(tipsPrefabName);
        
        if (null == prefab) return;

        var tipsObj = GameObject.Instantiate(prefab);

        if (null == tipsObj) return;
        var tipsBehv = tipsObj.GetComponent<ToastBehavior>();
        tipsBehv.Show(tips,time);
    }
}
