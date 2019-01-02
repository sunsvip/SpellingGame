using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;

public class DataLoader : MonoBehaviour
{
    private List<string> fileList = new List<string>();
    private List<string> vocLibNameList = new List<string>();
    private WWW net;
    public GameObject choseDialog, itemPrefab, aboutDialog;
    public Text headerTitle = null;

    private void Start()
    {
        choseDialog.SetActive(false);
        aboutDialog.SetActive(false);

        string missionVocLibName = PlayerPrefs.GetString(Conf.missionVocFileKey);
        if (missionVocLibName.Length > 0)
        {
            headerTitle.text = "任务词库" + System.Environment.NewLine + missionVocLibName;
        }
        else
        {
            headerTitle.text = "请选择任务词库";
        }
    }
    #region //读取XML
    public void LoadXmlFile()
    {
        //如果没有网络 判断本地是否存在配置文件 如果有则去读本地文件
        var confFile = Application.persistentDataPath + Conf.xmlFileName;
        if (File.Exists(confFile)){
            string confContent = File.ReadAllText(confFile);
            //校验本地配置文件是否有效
            if (confContent.Length < 10)
            {
                Debug.Log("删除无效配置文件");
                File.Delete(confFile);
            }
        }
        if (File.Exists(confFile))
        {
            LoadAddressList();
        }
        else
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                UIToast.ShowTips("无法连接网络，请检查网络", 2.0f);
            }
            else
            {
                //如果有网络更新xml
                UpdateAndShowVocLibList();
            }
        }

    }
    #endregion
    #region//读取词库在下地址列表
    public void LoadAddressList()
    {
        Debug.Log("LoadAddressList");
        //更新文件列表
        fileList.Clear();
        XmlDocument xmlDoc = new XmlDocument();
        var path = Application.persistentDataPath + Conf.xmlFileName;
        xmlDoc.Load(path);
        if (xmlDoc != null)
        {
            var rootNode = xmlDoc.SelectSingleNode("root");
            var fileAddressNode = rootNode.SelectSingleNode("fileAddress");
            var nodes = fileAddressNode.ChildNodes;
            foreach (var node in nodes)
            {
                XmlNode n = (XmlNode)node;
                fileList.Add(n.InnerText);
            }
            ShowChoseDialog();
            var newVerStr = rootNode.SelectSingleNode("appVersion");
            if (newVerStr != null && newVerStr.InnerText != Application.version)
            {
                var appUrl = rootNode.SelectSingleNode("downloadApp");
                if (appUrl != null)
                    StartCoroutine(gotoDownloadNewVersion(appUrl.InnerText));
            }
        }
        else
        {
            UIToast.ShowTips("读取配置文件失败!", 2.0f);
        }
    }
    private IEnumerator gotoDownloadNewVersion(string url)
    {
        UIToast.ShowTips("发现新版本", 3f);
        yield return new WaitForSeconds(3f);
        Application.OpenURL(url);
    }
    #endregion
    #region//更新xml文件
    public void UpdateAndShowVocLibList()
    {
        Debug.Log("下载更新xml文件");
        StartCoroutine(UpdateXmlFile(Conf.xmlFileAddress));
    }
    IEnumerator UpdateXmlFile(string address)
    {
        UIToast.ShowTips("正在更新词库列表...", 2.0f);
        net = new WWW(address);
        yield return net;

        //下载xml完成后将文件写入
        while (!net.isDone)
        {
            yield return 0;
        }
        //如果本地已存在xml 则检测xml版本
        if (File.Exists(Application.persistentDataPath + Conf.xmlFileName))
        {
            if (isNewVersion(net.text))
            {
                //有新版本
                File.WriteAllText(Application.persistentDataPath + Conf.xmlFileName, net.text);
            }
        }
        else
        {
            File.WriteAllText(Application.persistentDataPath + Conf.xmlFileName, net.text);
        }

        LoadAddressList();
    }
    #endregion
    #region//检测是否有新版本xml
    bool isNewVersion(string xmlStr)
    {
        string str = string.Empty;
        //将xml文件第一行xml版本描述删掉，否则解析xml报错
        for (int i = 0; i < xmlStr.Length; i++)
        {
            if (xmlStr[i] == '>')
            {
                str = xmlStr.Substring(i + 1);
                break;
            }
        }
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(str);

        var root = xmlDoc.SelectSingleNode("root");
        var node = root.SelectSingleNode("xmlVersion");

        //网络端xml版本号
        float xmlVersion = 0.0f;
        float.TryParse(node.InnerText, out xmlVersion);

        //本地xml版本号
        xmlDoc = new XmlDocument();
        var path = Application.persistentDataPath + Conf.xmlFileName;

        xmlDoc.Load(path);
        root = xmlDoc.SelectSingleNode("root");
        node = root.SelectSingleNode("xmlVersion");
        float localXmlVersion = 0.0f;
        float.TryParse(node.InnerText, out localXmlVersion);

        if (xmlVersion != localXmlVersion)
        {
            return true;
        }
        return false;
    }
    #endregion

    #region //将数据填充到词库选择界面
    public void ShowChoseDialog()
    {
        choseDialog.SetActive(true);
        var content = choseDialog.transform.Find("Window/Scroll View/Viewport/Content");
        if (content)
        {
            //先清空item再更新item
            foreach (Transform child in content.transform)
            {
                Destroy(child.gameObject);
            }

            for (int i = 0; i < fileList.Count; i++)
            {
                #region //截取文件名
                string fileName = string.Empty;

                for (int j = fileList[i].Length - 1; j >= 0; j--)
                {
                    if (fileList[i][j] == '/')
                    {
                        fileName = fileList[i].Substring(j + 1);
                        vocLibNameList.Add(fileName);
                        break;
                    }
                }
                #endregion
                //Debug.Log(fileName);
                //创建一个Item
                var item = UnityEngine.Object.Instantiate(itemPrefab, content) as GameObject;
                item.name = fileList[i];
                var title = item.transform.Find("Title").GetComponent<Text>();
                title.text = fileName;

                var textButton = title.GetComponent<Button>();
                var downloadButton = item.transform.Find("Buttons/Download").GetComponent<Button>();
                var refreshButton = item.transform.Find("Buttons/Refresh").GetComponent<Button>();
                var deleteButton = item.transform.Find("Buttons/Delete").GetComponent<Button>();

                //设置Button的响应事件
                textButton.onClick.AddListener(delegate ()
                {
                    OnClickButton(item, fileName, 0);
                });
                downloadButton.onClick.AddListener(delegate ()
                {
                    OnClickButton(item, fileName, 1);
                });
                refreshButton.onClick.AddListener(delegate ()
                {
                    OnClickButton(item, fileName, 2);
                });
                deleteButton.onClick.AddListener(delegate ()
                {
                    OnClickButton(item, fileName, 3);
                });
                //如果词库文件已存在 禁用下载按钮 激活重新下载按钮和删除按钮 设置Item可选为默认词库
                //如果词库文件不存在 激活下载按钮 禁用重新下载按钮和删除按钮 设置Item不可选为默认词库

                if (File.Exists(Application.persistentDataPath + @"/" + fileName))
                {
                    downloadButton.interactable = false;
                    refreshButton.interactable = true;
                    deleteButton.interactable = true;
                }
                else
                {
                    downloadButton.interactable = true;
                    refreshButton.interactable = false;
                    deleteButton.interactable = false;
                }
            }

            ShowUserDefineVocLib(content, itemPrefab);
        }
    }
    #endregion
    #region //读取本地用户自定义词库
    private void ShowUserDefineVocLib(Transform content, GameObject itemPrefab)
    {
        string path = Application.persistentDataPath + "/";
        DirectoryInfo info = new DirectoryInfo(path);
        var files = info.GetFiles("*.txt", SearchOption.TopDirectoryOnly);
        Debug.Log("词库文件个数:"+files.Length);
        foreach (var file in files)
        {
            if (!vocLibNameList.Contains(file.Name))
            {
                //Windows平台把文本转码为UTF-8防止中文无法显示,词库无法解析
                if(Application.platform == RuntimePlatform.WindowsPlayer)
                {
                    ConvertToUtf8(file.Name);
                }
                var item = GameObject.Instantiate(itemPrefab, content) as GameObject;
                item.name = "UserDefine";
                var title = item.transform.Find("Title").GetComponent<Text>();
                title.text = file.Name;

                var textButton = title.GetComponent<Button>();
                var downloadButton = item.transform.Find("Buttons/Download").GetComponent<Button>();
                var refreshButton = item.transform.Find("Buttons/Refresh").GetComponent<Button>();
                var deleteButton = item.transform.Find("Buttons/Delete").GetComponent<Button>();
                downloadButton.interactable = false;
                refreshButton.interactable = false;
                //设置Button的响应事件
                textButton.onClick.AddListener(delegate ()
                {
                    OnClickButton(item, file.Name, 0);
                });

                deleteButton.onClick.AddListener(delegate ()
                {
                    OnClickButton(item, file.Name, 3);
                    Destroy(item);
                });
            }
        }
    }
    #endregion
    #region //将用户自定义词库转换为UTF-8,否则无法正常解析词库
    void ConvertToUtf8(string file)
    {
        string filePath = Application.persistentDataPath + "/" + file;
        StreamReader sr = new StreamReader(filePath, Encoding.GetEncoding("GB18030"));
        string text = sr.ReadToEnd();
        sr.Close();
        StreamWriter sw = new StreamWriter(filePath, false, Encoding.UTF8);
        sw.WriteLine(text);
        sw.Flush();
        sw.Close();
    }
    #endregion
    #region //词库操作Button的响应事件
    public void OnClickButton(GameObject item, string fileName, int id)
    {
        Button[] bts = new Button[3];
        bts[0] = item.transform.Find("Buttons/Download").GetComponent<Button>();
        bts[1] = item.transform.Find("Buttons/Refresh").GetComponent<Button>();
        bts[2] = item.transform.Find("Buttons/Delete").GetComponent<Button>();
        //Debug.Log("FileList Size:" + fileList.Count);
        switch (id)
        {
            case 0:
                {
                    //如果file存在 则设置为默认词库
                    if (!File.Exists(Application.persistentDataPath + @"/" + fileName))
                    {
                        UIToast.ShowTips("请先下载词库", 2.0f);
                    }
                    else
                    {
                        PlayerPrefs.SetString(Conf.missionVocFileKey, fileName.Substring(0, fileName.Length - 4));
                        PlayerPrefs.Save();
                        UIToast.ShowTips("设置默认词库:" + fileName, 2.0f);
                        headerTitle.text = "任务词库" + System.Environment.NewLine + PlayerPrefs.GetString(Conf.missionVocFileKey);
                    }

                }
                break;
            case 1:
                //下载按钮响应
                {
                    StartCoroutine(DownloadFile(item.name,
                        Application.persistentDataPath + @"/" + fileName,
                        bts));
                }
                break;
            case 2:
                //更新按钮响应
                {
                    StartCoroutine(DownloadFile(item.name, Application.persistentDataPath + @"/" + fileName, bts));
                }
                break;
            case 3:
                //删除按钮响应
                {
                    DeleteVocabularyLib(fileName, bts);
                }
                break;
            default:
                break;
        }
    }
    #endregion

    #region //下载文件
    private IEnumerator DownloadProgress(string path, Button[] bts)
    {
        string progress = string.Empty;
        while (!net.isDone)
        {
            progress = net.progress * 100.0 + "%";
            //.Log("进度：" + progress);
            yield return 1;
        }
        if (net.error != null)
        {
            UIToast.ShowTips("下载出错:" + net.error, 2.0f);
        }
        else
        {
            progress = "100%";
            if (null != net.text)
            {
                File.WriteAllText(path, net.text, System.Text.Encoding.UTF8);
                UIToast.ShowTips("下载完成:" + path, 2.0f);
                bts[0].interactable = false;
                bts[1].interactable = true;
                bts[2].interactable = true;
            }
        }
        yield return 1;
    }
    private IEnumerator DownloadFile(string address, string path, Button[] bts)
    {
        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            net = new WWW(address);
            yield return StartCoroutine(DownloadProgress(path, bts));
        }
        else
        {
            UIToast.ShowTips("无网络连接...", 2.0f);
            yield return 0;
        }
    }
    #endregion
    #region //删除词库文件
    public void DeleteVocabularyLib(string fileName, Button[] bts)
    {
        string path = Application.persistentDataPath + @"/" + fileName;
        if (File.Exists(path))
        {
            File.Delete(path);
            var vocLibName = fileName.Substring(0, fileName.Length - 4);
            path = Application.persistentDataPath + @"/" + vocLibName + ".xml";
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            path = Application.persistentDataPath + @"/" + vocLibName + "_Level.xml";
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            bts[0].interactable = true;
            bts[1].interactable = false;
            bts[2].interactable = false;
            UIToast.ShowTips("删除文件:" + fileName, 2.0f);
        }
    }
    #endregion
    
    #region 加载词库供游戏使用
    public void LoadVocLibAndStartGame(int sceneIndex)
    {
        string vocFileName = PlayerPrefs.GetString(Conf.missionVocFileKey);
        string filePath = Application.persistentDataPath + @"/" + vocFileName + ".txt";
        if (File.Exists(filePath))
        {
            string xmlFilePath = Application.persistentDataPath + @"/" +
                vocFileName + ".xml";
            if (File.Exists(xmlFilePath))
            {
                //如果已经解析过词库，直接加载xml
                Debug.Log("加载xml词库，开始游戏");
                SceneManager.LoadScene(sceneIndex);
            }
            else
            {
                //如果词库未解析，则解析词库
                Debug.Log("文件不存在:" + xmlFilePath);
                StartCoroutine(RegexMatch(vocFileName, delegate ()
                {
                    LoadVocLibAndStartGame(sceneIndex);
                }));
            }
        }
        else
        {
            UIToast.ShowTips("词库不存在，请先设置词库", 2.0f);
        }
    }
    #endregion

    #region 解析词库，正则匹配抓取单词
    IEnumerator RegexMatch(string file, UnityEngine.Events.UnityAction callBack)
    {
        string filePath = Application.persistentDataPath + "/" + file + ".txt";
        filePath = Regex.Replace(filePath, "/", @"\");
        var www = new WWW("file://" + filePath);
        yield return www;
        string nText = www.text;
        string pattern = @"[a-zA-Z]+.*[\u4e00-\u9fa5]+|[\u4e00-\u9fa5]+.*[a-zA-Z]+";
        var mat = Regex.Match(nText, pattern);
        int count = 0;
        List<Vocabulary> vocList = new List<Vocabulary>();
        while (mat.Success)
        {
            count++;
            string str = Regex.Replace(mat.Value, @"(\[.*\])+|[\uFE30-\uFFA0]+", " ");
            str = Regex.Replace(str, @"[ \n\s*\r]+", " ");

            //匹配词性 如abj.
            var c = Regex.Match(str, @"[a-z]+\.");
            string en = string.Empty;
            if (c.Success)
            {
                if (c.Value.Contains("sb") || c.Value.Contains("sth"))
                {
                    en = str.Substring(0, c.Index + c.Length);
                    str = str.Substring(c.Index + c.Length);
                }
                else
                {
                    en = str.Substring(0, c.Index - 1);
                    str = str.Substring(c.Index);
                }
                vocList.Add(new Vocabulary(en, str));
            }
            else
            {
                var enMat = Regex.Match(str, @"[a-zA-Z]+([ -][a-zA-Z]+)*");
                str = Regex.Replace(str, @"[a-zA-Z]+([ -][a-zA-Z]+)*", "");

                vocList.Add(new Vocabulary(enMat.Value, str));
            }
            mat = mat.NextMatch();
        }
        if (vocList.Count > 0)
        {
            //SaveVocList(SortVocList(vocList), file + ".xml");
            SaveVocList(vocList, file + ".xml");
            callBack.Invoke();
        }
        else
        {
            UIToast.ShowTips("词库解析失败，请检查词库格式是否正确", 2.0f);
        }
    }
    #endregion
    #region 将解析出的单词列表存入xml中
    private void SaveVocList(List<Vocabulary> vocList, string fileName)
    {
        XmlDocument xmlDoc = new XmlDocument();
        var head = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null);
        xmlDoc.AppendChild(head);

        XmlNode root = xmlDoc.CreateElement("VocList");
        xmlDoc.AppendChild(root);
        foreach (var voc in vocList)
        {
            //Debug.Log(voc.en + "----" + voc.zh);
            var vocNode = xmlDoc.CreateNode(XmlNodeType.Element, "Voc", null);
            var enNode = xmlDoc.CreateNode(XmlNodeType.Element, "en", null);
            var zhNode = xmlDoc.CreateNode(XmlNodeType.Element, "zh", null);
            enNode.InnerText = voc.en;
            zhNode.InnerText = voc.zh;
            vocNode.AppendChild(enNode);
            vocNode.AppendChild(zhNode);
            root.AppendChild(vocNode);
        }
        xmlDoc.Save(Application.persistentDataPath + "/" + fileName);
    }
    #endregion

    #region 将单词按照长短排序
    private List<Vocabulary> SortVocList(List<Vocabulary> vocList)
    {
        for (int i = 0; i < vocList.Count - 1; i++)
        {
            for (int k = i; k < vocList.Count; k++)
            {
                if (vocList[i].en.Length > vocList[k].en.Length)
                {
                    Vocabulary voc = vocList[i];
                    vocList[i] = vocList[k];
                    vocList[k] = voc;
                }
            }
        }
        return vocList;
    }
    #endregion
}
