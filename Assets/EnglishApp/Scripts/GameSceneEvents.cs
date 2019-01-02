using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class GameSceneEvents : MonoBehaviour
{
    public GameController gameCtrl = null;

    private bool keyboardShift = false;//是否切换大小写
    void Start()
    {
        Time.timeScale = 1;
        gameCtrl.pauseWin.SetActive(false);
        gameCtrl.gameOverWin.SetActive(false);
        gameCtrl.inputBox.text = "";

        //动态添加Keyboard按钮事件
#if UNITY_ANDROID || UNITY_IOS
        AddKeyboardEvents();
        gameCtrl.keyboard.SetActive(true);
#else
        gameCtrl.keyboard.SetActive(false);
#endif
    }

    public void OnButtonClick(int tag)
    {
        switch (tag)
        {
            case 1:
                {
                    //Pause Button Call Back
                    if (gameCtrl.pauseWin)
                    {
                        gameCtrl.pauseWin.SetActive(true);
#if UNITY_ANDROID || UNITY_IOS
                        gameCtrl.keyboard.SetActive(false);
#endif
                        Time.timeScale = 0;
                    }

                }
                break;
            case 2:
                {
                    //Home Button Call Back
                    Time.timeScale = 1;
                    SceneManager.LoadScene(1);
                }
                break;
            case 3:
                {
                    //Restart Button Call Back
                    SceneManager.LoadScene(2);
                }
                break;
            case 4:
                {
                    //继续游戏
                    gameCtrl.pauseWin.SetActive(false);
#if UNITY_ANDROID || UNITY_IOS
                    gameCtrl.keyboard.SetActive(true);
#endif
                    Time.timeScale = 1.0f;
                }
                break;
            case 5:
                {
                    //下一关
                    gameCtrl.StartNextLevel();//先把下一关得信息写入PlayerPrefs再开始
                    SceneManager.LoadScene(2);
                }
                break;
            default:
                break;
        }
    }

    public void OnKeyboardClick(string str)
    {
        switch (str)
        {
            case "space":
                if (gameCtrl.inputBox.text.Length > 0 && ' ' != gameCtrl.inputBox.text[gameCtrl.inputBox.text.Length - 1])
                    gameCtrl.inputBox.text += " ";
                break;
            case "delete":
                if (gameCtrl.inputBox.text.Length > 0)
                    gameCtrl.inputBox.text = gameCtrl.inputBox.text.Substring(0, gameCtrl.inputBox.text.Length - 1);
                break;
            case "shift":
                #region //大小写转换
                {
                    var texts = gameCtrl.keyboard.GetComponentsInChildren<Text>();
                    if (!keyboardShift)
                    {
                        foreach (var item in texts)
                        {
                            item.text = item.text.ToUpper();
                        }
                    }
                    else
                    {
                        foreach (var item in texts)
                        {
                            item.text = item.text.ToLower();
                        }
                    }
                    keyboardShift = !keyboardShift;
                }
                #endregion
                break;
            case "enter":
                {
                    //提交
                    gameCtrl.Attack();
                }
                break;
            case "clear":
                {
                    gameCtrl.inputBox.text = "";
                }
                break;
            default:
                gameCtrl.inputBox.text += str;
                break;
        }

    }
    private void AddKeyboardEvents()
    {
        var bts = gameCtrl.keyboard.GetComponentsInChildren<Button>();
        foreach (var bt in bts)
        {
            var btext = bt.GetComponentInChildren<Text>();
            if (null != btext)
            {
                bt.onClick.AddListener(delegate ()
                {
                    OnKeyboardClick(btext.text);
                });
            }
            else
            {
                bt.onClick.AddListener(delegate ()
                {
                    OnKeyboardClick(bt.name);
                });
            }
        }
    }
    public void OnButtonDown(int tag)
    {
        //技能button按下时
        gameCtrl.OnSkillButtonDown();
    }
    public void OnButtonUp(int tag)
    {
        gameCtrl.OnSkillButtonUp(tag);
    }
}
