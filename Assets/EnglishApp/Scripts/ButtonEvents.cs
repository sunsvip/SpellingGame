using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System.IO;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class ButtonEvents : MonoBehaviour
{
    public DataLoader loader = null;

    public void OnButtonClick(int tag)
    {
        switch (tag)
        {
            //选择词库
            case 1:
                {
                    Debug.Log("点击选择词库");
                    //读取词库列表，显示在选择词库对话框
                    loader.LoadXmlFile();
                }
                break;

            //游戏说明
            case 2:
                {
                    Debug.Log("游戏说明");
                    var textObj = loader.aboutDialog.transform.Find("Window/Scroll View/Viewport/Content/UserDefine");
                    if (textObj)
                    {
                        var text = textObj.GetComponent<Text>();
                        text.text = "Version:" + Application.version + System.Environment.NewLine +
                            System.Environment.NewLine +
                            "自定义词库存放目录:" + 
                            System.Environment.NewLine + Application.persistentDataPath;
                    }
                    loader.aboutDialog.SetActive(true);
                }
                break;
            case 3:
                {
                    loader.LoadVocLibAndStartGame(1);
                }
                break;
            case 4:
                {
                    //DataLoader.Instance(this).LoadVocLibAndStartGame(2);
                    UIToast.ShowTips("呼呼~ 此功能正在开发中...", 2.0f);
                }
                break;
            case 5:
                //退出
                Application.Quit();
                break;
        }

    }
    public void OnCloseButton(GameObject window)
    {
        if(window)
            window.SetActive(false);
    }
    public void OpenUrl(string urlStr)
    {
        Application.OpenURL(urlStr);
    }
}
