using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class LevelManager : MonoBehaviour
{
    public Text levelSceneTitle = null;
    [Tooltip("VocCount EveryLevel")]
    public int vocCountEveryLevel = 20;
    [Tooltip("Child Level Count")]
    public int childLevelCount = 5;//默认每关有5个子关卡，普通1 普通2 进阶1 进阶2 Boss
    [Tooltip("ScrollView Content")]
    public Transform content = null;
    public GameObject levelItemPrefab = null;
    public GameObject levelButtonPrefab = null;

    void Start()
    {
        var fixScrollViewPort = content.parent.GetComponent<RectTransform>();
        fixScrollViewPort.anchorMax = Vector2.one;

        InitLevelView();
    }
    public void OnReturnButtonClick()
    {
        SceneManager.LoadScene(0);
    }
    private void InitLevelView()
    {
        string missionFile = PlayerPrefs.GetString(Conf.missionVocFileKey);

        if (missionFile.Length > 0)
        {
            string levelFile = Application.persistentDataPath + "/" + missionFile + "_Level.xml";
            if (!File.Exists(levelFile))
            {
                //如果关卡配置文件不存在则根据当前任务词库创建关卡配置文件
                {
                    string vocLibPath = Application.persistentDataPath + "/" + missionFile + ".xml";

                    if (File.Exists(vocLibPath))
                    {
                        XmlDocument vocXmlDoc = new XmlDocument();
                        vocXmlDoc.Load(vocLibPath);
                        var vocListNode = vocXmlDoc.SelectSingleNode("VocList");
                        var vocs = vocListNode.ChildNodes;

                        int levelCount = vocs.Count / vocCountEveryLevel;
                        int remainCount = vocs.Count % vocCountEveryLevel;
                        bool appendRemain = false;//是否把剩余单词加到左后一个Level
                        if (levelCount > 0)
                        {
                            //剩余单词数大于每关限定单词数的1/3则新加一个关卡
                            if (remainCount > vocCountEveryLevel / 3)
                            {
                                levelCount = levelCount + 1;
                                appendRemain = false;
                            }
                            else
                            {
                                appendRemain = true;//剩余单词过少，将其插入最后一关
                            }
                        }
                        else
                        {
                            levelCount = levelCount + 1;
                            appendRemain = false;
                        }
                        //创建关卡配置文件
                        var levelXmlDoc = new XmlDocument();
                        levelXmlDoc.CreateXmlDeclaration("1.0", "utf-8", "");
                        var root = levelXmlDoc.CreateNode(XmlNodeType.Element, "root", "");
                        levelXmlDoc.AppendChild(root);

                        var levelVocCount = levelXmlDoc.CreateNode(XmlNodeType.Element, "LevelVocCount", "");
                        levelVocCount.InnerText = string.Format("{0}", vocs.Count);
                        root.AppendChild(levelVocCount);

                        var levelList = levelXmlDoc.CreateNode(XmlNodeType.Element, "LevelList", "");
                        root.AppendChild(levelList);
                        #region 关卡数据
                        for (int i = 0; i < levelCount; i++)
                        {
                            //关卡列表
                            var level = levelXmlDoc.CreateNode(XmlNodeType.Element, "Level", "");
                            levelList.AppendChild(level);
                            //第几关卡
                            var node = levelXmlDoc.CreateNode(XmlNodeType.Element, "Num", "");
                            node.InnerText = string.Format("{0}", i + 1);
                            level.AppendChild(node);
                            //此关卡的任务单词索引
                            node = levelXmlDoc.CreateNode(XmlNodeType.Element, "VocIndex", "");
                            int currentIndex = i * vocCountEveryLevel;
                            if (i < levelCount - 1)
                            {
                                node.InnerText = string.Format("{0}-{1}", currentIndex, i * vocCountEveryLevel + vocCountEveryLevel - 1);
                            }
                            else
                            {
                                if (appendRemain)
                                {
                                    node.InnerText = string.Format("{0}-{1}", currentIndex, i * vocCountEveryLevel + vocCountEveryLevel + remainCount - 1);
                                }
                                else
                                {
                                    node.InnerText = string.Format("{0}-{1}", currentIndex, vocs.Count - 1);
                                }
                            }
                            level.AppendChild(node);
                            //子关卡列表
                            var childLevelList = levelXmlDoc.CreateNode(XmlNodeType.Element, "ChildLevelList", "");
                            level.AppendChild(childLevelList);
                            #region 子关卡数据
                            for (int k = 0; k < childLevelCount; k++)
                            {
                                //子关卡
                                var childLevel = levelXmlDoc.CreateNode(XmlNodeType.Element, "ChildLevel", "");
                                childLevelList.AppendChild(childLevel);
                                //第几关
                                var childLevelNum = levelXmlDoc.CreateNode(XmlNodeType.Element, "Num", "");
                                childLevelNum.InnerText = string.Format("{0}", k + 1);
                                childLevel.AppendChild(childLevelNum);
                                //是否解锁了此关卡
                                var unlock = levelXmlDoc.CreateNode(XmlNodeType.Element, "Unlock", "");
                                //第一关默认解锁
                                if (0 == i && 0 == k)
                                {
                                    unlock.InnerText = string.Format("{0}", 1);
                                }
                                else
                                {
                                    unlock.InnerText = string.Format("{0}", 0);
                                }
                                childLevel.AppendChild(unlock);
                                //是否通关
                                var pass = levelXmlDoc.CreateNode(XmlNodeType.Element, "Pass", "");
                                pass.InnerText = string.Format("{0}", 0);
                                childLevel.AppendChild(pass);
                                //通关评星
                                var star = levelXmlDoc.CreateNode(XmlNodeType.Element, "Star", "");
                                star.InnerText = string.Format("{0}", 0);
                                childLevel.AppendChild(star);
                                //关卡类型 1:普通 2:进阶 3:Boss
                                var levelType = levelXmlDoc.CreateNode(XmlNodeType.Element, "LevelType", "");
                                int lt = 0;
                                if (k < (childLevelCount - 1) / 2)
                                {
                                    lt = 1;
                                }
                                else if (k < childLevelCount - 1)
                                {
                                    lt = 2;
                                }
                                else if (k == childLevelCount - 1)//最后一关是boss关卡
                                {
                                    lt = 3;
                                }
                                childLevel.AppendChild(levelType);
                                levelType.InnerText = string.Format("{0}", lt);
                            }
                            #endregion
                        }

                        #endregion
                        levelXmlDoc.Save(levelFile);
                        //如果任务配置文件不存在 创建
                        InitLevelView();
                    }
                    else
                    {
                        //如果关卡配置文件不存在 并且对应的词库文件也不存在 则回到主界面
                        SceneManager.LoadScene(0);
                    }
                }
            }
            else
            {
                //读取关卡配置文件，根据配置文件创建关卡
                //Debug.Log("关卡配置文件存在,读取关卡配置文件，根据配置文件创建关卡");
                CreateLevelItemFromFile(levelFile);
            }

        }
    }
    private void CreateLevelItemFromFile(string levelFile)
    {
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.Load(levelFile);

        var root = xmlDoc.SelectSingleNode("root");
        //显示当前任务词库名和任务单词总数
        levelSceneTitle.text = "任务词库:" + PlayerPrefs.GetString(Conf.missionVocFileKey) + System.Environment.NewLine +
            "单词总数:" + root.SelectSingleNode("LevelVocCount").InnerText;

        var levelRoot = root.SelectSingleNode("LevelList");
        var levelList = levelRoot.ChildNodes;
        foreach (XmlNode level in levelList)
        {
            var item = Instantiate(levelItemPrefab, content) as GameObject;
            var childLevelList = level.SelectSingleNode("ChildLevelList").ChildNodes;
            foreach (XmlNode childLevel in childLevelList)
            {
                var levelBt = Instantiate(levelButtonPrefab, item.transform) as GameObject;
                var bt = levelBt.GetComponentInChildren<Button>();
                bool unlock = int.Parse(childLevel.SelectSingleNode("Unlock").InnerText) == 1;
                //如果已解锁，显示关卡号以及评星
                if (unlock)
                {
                    bt.interactable = true;
                    var text = levelBt.GetComponentInChildren<Text>();
                    text.text = level.SelectSingleNode("Num").InnerText + "-" + childLevel.SelectSingleNode("Num").InnerText;

                    bool pass = int.Parse(childLevel.SelectSingleNode("Pass").InnerText) == 1;
                    if (pass)
                    {
                        //如果已通关显示评星
                        int starCount = int.Parse(childLevel.SelectSingleNode("Star").InnerText);
                        var starLevel = levelBt.transform.Find("StarLevel");
                        int tempCount = 0;
                        foreach (Transform child in starLevel.transform)
                        {
                            if (tempCount < starCount)
                                child.GetComponent<Image>().enabled = true;
                            else
                                child.GetComponent<Image>().enabled = false;

                            tempCount++;
                        }
                    }
                    bt.onClick.AddListener(delegate ()
                    {
                        //将用户选择的关卡信息存入PlayerPrefs以便GameScene加载使用
                        //关卡类型 关卡对应单词索引
                        PlayerPrefs.SetString(Conf.levelVocIndex, level.SelectSingleNode("VocIndex").InnerText);
                        PlayerPrefs.SetInt(Conf.levelType, int.Parse(childLevel.SelectSingleNode("LevelType").InnerText));
                        PlayerPrefs.SetString(Conf.levelNum, text.text);
                        SceneManager.LoadScene(2);

                        //Debug.Log(Conf.levelVocIndex + PlayerPrefs.GetString(Conf.levelVocIndex));
                        //Debug.Log(Conf.levelType + PlayerPrefs.GetInt(Conf.levelType));
                        //Debug.Log(Conf.levelNum + PlayerPrefs.GetString(Conf.levelNum));

                    });
                }
                else
                {
                    bt.interactable = false;
                }
            }
        }
    }
}
