using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Xml;
using UnityEngine.EventSystems;
public class GameController : MonoBehaviour
{
    //当前关卡要学习的单词列表
    private LevelConfig currLv = null;
    public GameObject[] levelPrefabs = null;
    public List<Vocabulary> vocList = new List<Vocabulary>();
    //每创建一个Enemy就存入enemyList
    private List<GameObject> enemyList = new List<GameObject>();
    [HideInInspector]
    public int levelType = 0;
    private Vector2 levelNum = Vector2.zero;
    //private int childLevelNum;
    private int missionVocCount = 0;
    //纪录单词熟练度 用于评星
    private int multiKillCount = 0;//连杀次数
    private int maxMultiKillCount = 0;//最高连杀
    private int errorCount = 0; //错误次数
    private int spendTime = 0;//通关用时
    //游戏进度
    private int remainVocCount = 0;
    public GameObject gameOverWin = null;
    public GameObject pauseWin = null;
    public GameObject keyboard = null;
    public Text inputBox = null;
    public GameObject vocWinPrefab = null;
    public Text progress = null;

    public Button[] skillBts = new Button[3];
    public GameObject skillCirclePrefab = null;
    public float skillDamageRange = 5.0f;
    public float skillTime = 5.0f;//技能持续几秒
    private float skillCircleScale = 0;
    
    [HideInInspector]
    public GameObject skillCircle = null;//技能环
    //生命
    public Slider hpSlider = null;
    public Slider mpSlider = null;

    //下一关信息
    private int nextLevelType = 0;
    private string nextLevelNum;
    private string nextLevelVocIndex;


    private Vector3 screenSize = new Vector3();
    private Vector2 initEnemyPos = new Vector2();
    private Vector3 touchPos = new Vector3();
#if UNITY_Android || UNITY_IOS

#else
    string inputStr;
#endif
    void Start()
    {
        Time.timeScale = 1;
        screenSize = Camera.main.ViewportToWorldPoint(Vector2.one);
        initEnemyPos.x = screenSize.x + 1.2f;
        initEnemyPos.y = screenSize.y - 2f;
        skillCircleScale = skillDamageRange / Camera.main.ScreenToWorldPoint(new Vector2(256, 0)).x;
        InitGameData();

        //如果不是Boss关 隐藏单词提示技能
        if (levelType!=3)
        {
            var skillObj = GameObject.Find("Canvas/Skills/ShowVoc");
            if (skillObj)
                skillObj.SetActive(false);
        }
        //游戏开始，自动创建敌人
        StartCoroutine(AutoCreateEnemy());
        //计时器,用于计算通关耗时
        InvokeRepeating("GameTimer", 0.0f, 1.0f);
    }
    void GameTimer()
    {
        spendTime += 1;
        //每秒回蓝
        if (mpSlider.value < mpSlider.maxValue)
        {
            if (mpSlider.value + currLv.mpRecoverySpeed > mpSlider.maxValue)
            {
                mpSlider.value = mpSlider.maxValue;
            }
            else
            {
                mpSlider.value += currLv.mpRecoverySpeed;
            }
        }
        RefreshSkillBtState();
    }
    //屏幕适配
    void SpriteSizeFitter()
    {
        //背景Sprite
        var bgSprite = currLv.bg.GetComponent<SpriteRenderer>();
        var wallSprite = currLv.wall.GetComponent<SpriteRenderer>();
        //获取屏幕的世界坐标系大小 不是屏幕分辨率
        bgSprite.size = new Vector2(screenSize.x * 2, screenSize.y * 2);
        currLv.wall.transform.position = new Vector3(-screenSize.x + wallSprite.bounds.size.x / 2,
            0, currLv.wall.transform.position.z);
        wallSprite.size = new Vector2(wallSprite.size.x, screenSize.y * 2);
        currLv.bow.gameObject.transform.position = currLv.wall.transform.position + new Vector3(-0.2637f, 0, 0);
    }

    bool checkInputChar(char ch)
    {
        return ((ch >= 'a' && ch <= 'z') || (ch>='A' &&ch<='Z') || ch == '.' || ch == ' ' || ch == '_');
    }
//#if UNITY_STANDALONE_WIN
    void Update()
    {
        inputStr = Input.inputString;
        if (inputStr.Length > 0 && checkInputChar(inputStr[0]))
        {
            inputBox.text += inputStr;
        }
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            if (inputBox.text.Length > 0)
            {
                inputBox.text = inputBox.text.Substring(0, inputBox.text.Length - 1);
            }
        }
        else if (Input.GetKeyDown(KeyCode.Return))
        {
            Attack();
        }
    }
//#endif
    //初始化游戏数据，例如读取单词列表，关卡类型等
    private void InitGameData()
    {
        //读取词库
        var missionVocFile = PlayerPrefs.GetString(Conf.missionVocFileKey);
        levelType = PlayerPrefs.GetInt(Conf.levelType);
        var levelNumStr = PlayerPrefs.GetString(Conf.levelNum).Split('-');
        float.TryParse(levelNumStr[0], out levelNum.x);
        float.TryParse(levelNumStr[1], out levelNum.y);

        //LevelVocIndex是 0-19 方式存的，表示索引从0到19，所以要以-分割成两个数字
        var vocIndex = PlayerPrefs.GetString(Conf.levelVocIndex).Split('-');
        var vocIndexRange = new Vector2(int.Parse(vocIndex[0]), int.Parse(vocIndex[1]));

        //从词库xml文件中获取全部单词，在按照vocIndexRange取出当前关卡要学的单词
        XmlDocument vocXmlDoc = new XmlDocument();
        string vocLibPath = Application.persistentDataPath + "/" + missionVocFile + ".xml";
        vocXmlDoc.Load(vocLibPath);
        var vocListNode = vocXmlDoc.SelectSingleNode("VocList");
        var vocs = vocListNode.ChildNodes;

        for (int i = (int)vocIndexRange.x; i <= (int)vocIndexRange.y; i++)
        {
            Vocabulary v = new Vocabulary(vocs[i].SelectSingleNode("en").InnerText,
                vocs[i].SelectSingleNode("zh").InnerText);
            vocList.Add(v);
        }
        //当前剩余单词数，用于判断任务是否完成
        //如果剩余单词数为0说明已通关
        remainVocCount = vocList.Count;
        missionVocCount = vocList.Count;
        progress.text = string.Format("{0}/{1}", remainVocCount, missionVocCount);
        //初始化关卡场景(背景 弓箭...)
        InitLevel();
    }
    private void InitLevel()
    {
        GameObject lv = Instantiate(levelPrefabs[(int)levelNum.y - 1]) as GameObject;
        currLv = lv.GetComponent<LevelConfig>();
        SpriteSizeFitter();//此函数用到currlv 所以必须在currLv初始化后调用
        hpSlider.minValue = 0;
        hpSlider.maxValue = currLv.hp;
        hpSlider.wholeNumbers = true;
        hpSlider.value = currLv.hp;
        mpSlider.minValue = 0;
        mpSlider.maxValue = currLv.mp;
        mpSlider.wholeNumbers = true;
        mpSlider.value = currLv.mp;
    }
    //当用户点击攻击按钮时触发
    //查找当前的敌人是否有和用户输入单词匹配的，如果有则向对应的敌人射箭
    public void Attack()
    {
        //获取当前场景中所有单词 判断是否有匹配
        string text = inputBox.text;
        if (text.Length > 0)
        {
            //是否有匹配的单词
            bool isFind = false;
            foreach (GameObject enemyObj in enemyList)
            {
                string en = enemyObj.transform.Find("VocWin/Voc/en").GetComponent<Text>().text;
                if (text == en)
                {
                    inputBox.text = "";
                    //如果玩家输入正确则射击
                    currLv.bow.Shoot(enemyObj.transform);
                    isFind = true;
                    break;
                }
                else
                {
                    //无匹配单词 输入框闪烁警示
                    isFind = false;
                }
            }
            if (isFind)
            {
                multiKillCount++;
                //纪录最高连杀
                if (multiKillCount > maxMultiKillCount)
                    maxMultiKillCount = multiKillCount;
            }
            else
            {
                multiKillCount = 0;//如果输错，连杀次数归零
                errorCount++;//纪录本关共出错总次数
                StartCoroutine(WarningColor());
            }
        }
    }
    IEnumerator WarningColor()
    {
        inputBox.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        inputBox.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        inputBox.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        inputBox.color = Color.white;
    }

    //自动创建敌人
    IEnumerator AutoCreateEnemy()
    {
        int radomEnemyIndex;
        GameObject m_enemy;
        float radomY;
        GameObject enemyObj;
        while (vocList.Count > 0)
        {
            if (enemyList.Count < currLv.maxEnemyCount)
            {
                if (vocList.Count <= 3)
                {
                    var bossIndex = Random.Range(0, currLv.bossPrefabs.Length);
                    m_enemy = currLv.bossPrefabs[bossIndex];
                }
                else
                {
                    radomEnemyIndex = Random.Range(0, currLv.enemyPrefabs.Length);
                    m_enemy = currLv.enemyPrefabs[radomEnemyIndex];
                }


                radomY = Random.Range(-initEnemyPos.y, initEnemyPos.y);
                enemyObj = Instantiate(m_enemy, new Vector3(initEnemyPos.x, radomY, radomY), m_enemy.transform.rotation) as GameObject;
                var vocWinObj = Instantiate(vocWinPrefab, enemyObj.transform) as GameObject;
                vocWinObj.name = "VocWin";
                vocWinObj.transform.localPosition = new Vector3(0, enemyObj.GetComponent<SpriteRenderer>().bounds.size.y * 0.5f, 0);
                var enemyCtrl = enemyObj.GetComponent<Enemy>();
                enemyCtrl.InitEnemyData(this);
                enemyList.Add(enemyObj);
                //如果场景中的敌人被全部消灭了，则立即创建敌人
                if (enemyList.Count < 1)
                    yield return new WaitForSeconds(0.2f);
                else
                    yield return new WaitForSeconds(currLv.createEnemySpan);
            }
            yield return new WaitForSeconds(0.2f);
        }
    }

    //塔被攻击
    public void Damage(int damageValue)
    {
        if (hpSlider.value < damageValue)
            hpSlider.value = 0;
        else
            hpSlider.value -= damageValue;
        //Debug.Log("塔被攻击，血量剩余:" + hpSlider.value);

        if (hpSlider.value < 1)
        {
            //游戏失败
            ShowGameOverWindow(false);
            
        }
        else
        {
            var remainHpRate = hpSlider.value / hpSlider.maxValue;
            if (remainHpRate <= 0.3f)
            {

                List<SpriteRenderer> renders = new List<SpriteRenderer>();
                foreach (Transform item in currLv.wall.transform)
                {
                    var render = item.GetComponent<SpriteRenderer>();
                    renders.Add(render);
                }
                renders[1].enabled = true;
                renders[0].enabled = false;
            }
            else if (remainHpRate <= 0.7f)
            {
                List<SpriteRenderer> renders = new List<SpriteRenderer>();
                foreach (Transform item in currLv.wall.transform)
                {
                    var render = item.GetComponent<SpriteRenderer>();
                    renders.Add(render);
                }
                renders[1].enabled = false;
                renders[0].enabled = true;
            }
        }
    }
    public void UpdateRemainVocCount()
    {
        remainVocCount--;
        progress.text = string.Format("{0}/{1}", remainVocCount, missionVocCount);
        if (remainVocCount <= 0)
        {
            //游戏胜利 对游戏通关评星 解锁下一关
            ShowGameOverWindow(true);
        }
    }
    private void ShowGameOverWindow(bool isWin)
    {
        Time.timeScale = 0;
        CancelInvoke("GameTimer");
        //评星等级
        int starLevel = 0;
        var gameOverTitle = gameOverWin.transform.Find("Window/Header").GetComponentInChildren<Text>();
        var nextLevelBt = gameOverWin.transform.Find("Window/Menus/Next").gameObject;
        //如果游戏胜利 则解锁下一关
        if (isWin)
        {
            gameOverTitle.text = "游戏胜利";
            nextLevelBt.SetActive(true);
            XmlDocument xmlDoc = new XmlDocument();
            string levelXmlFile = Application.persistentDataPath + "/" + PlayerPrefs.GetString(Conf.missionVocFileKey) + "_Level.xml";
            xmlDoc.Load(levelXmlFile);

            var root = xmlDoc.SelectSingleNode("root");
            var lvListNode = root.SelectSingleNode("LevelList");
            var lvList = lvListNode.ChildNodes;
            //首先设置当前关卡通关状态 及通关评星
            var childLvlist = lvList[(int)levelNum.x - 1].SelectSingleNode("ChildLevelList").ChildNodes;
            var childLv = childLvlist[(int)levelNum.y - 1];
            var passNode = childLv.SelectSingleNode("Pass");
            passNode.InnerText = string.Format("{0}", 1);
            var starNode = childLv.SelectSingleNode("Star");

            //根据最高连杀和正确率评星
            float accuracy = (float)errorCount / missionVocCount;
            if (accuracy <= 0.2f)
                starLevel = 3;
            else if (accuracy < 0.5f)
                starLevel = 2;
            else
                starLevel = 1;
            starNode.InnerText = string.Format("{0}", starLevel);
            if ((int)levelNum.y < childLvlist.Count)
            {
                childLvlist = lvList[(int)levelNum.x - 1].SelectSingleNode("ChildLevelList").ChildNodes;
                childLv = childLvlist[(int)levelNum.y];
                var unlockNode = childLv.SelectSingleNode("Unlock");
                unlockNode.InnerText = string.Format("{0}", 1);

                nextLevelNum = string.Format("{0}-{1}", (int)levelNum.x, (int)levelNum.y + 1);
                int.TryParse(childLv.SelectSingleNode("LevelType").InnerText, out nextLevelType);
                nextLevelVocIndex = lvList[(int)levelNum.x - 1].SelectSingleNode("VocIndex").InnerText;
            }
            else
            {
                if ((int)levelNum.x < lvList.Count)
                {
                    childLvlist = lvList[(int)levelNum.x].SelectSingleNode("ChildLevelList").ChildNodes;
                    childLv = childLvlist[0];
                    var unlock = childLv.SelectSingleNode("Unlock");
                    unlock.InnerText = string.Format("{0}", 1);

                    nextLevelNum = string.Format("{0}-{1}", (int)levelNum.x + 1, 1);
                    int.TryParse(childLv.SelectSingleNode("LevelType").InnerText, out nextLevelType);
                    nextLevelVocIndex = lvList[(int)levelNum.x].SelectSingleNode("VocIndex").InnerText;
                }
                else
                {
                    //全部通关
                    gameOverTitle.text = "游戏全部通关";
                    nextLevelBt.SetActive(false);
                }
            }
            xmlDoc.Save(levelXmlFile);
        }
        else
        {
            starLevel = 0;
            gameOverTitle.text = "游戏失败";
            nextLevelBt.SetActive(false);
        }

        //修改游戏胜利窗口中的数据
        var starsObj = gameOverWin.transform.Find("Window/Content/Stars");
        int tempCount = 0;
        foreach (Transform child in starsObj.transform)
        {
            if (tempCount < starLevel)
                child.GetComponent<Image>().enabled = true;
            else
                child.GetComponent<Image>().enabled = false;

            tempCount++;
        }
        var stats = gameOverWin.transform.Find("Window/Content/Stats");
        var maxMultiKill = stats.transform.Find("MultiKill");
        if (maxMultiKill)
            maxMultiKill.GetComponent<Text>().text = string.Format("最高连杀: {0}", maxMultiKillCount);

        var timer = stats.transform.Find("Timer");
        if (timer)
            timer.GetComponent<Text>().text =
                string.Format("用时: {0}分{1}秒", spendTime / 60, spendTime % 60);

        var error = stats.transform.Find("ErrorCount");
        if (error)
            error.GetComponent<Text>().text = string.Format("错误次数: {0}", errorCount);
        gameOverWin.SetActive(true);
    }
    //消灭敌人后 将其移除当前敌人列表 每消灭一个敌人有回血效果
    public void UpdateRemainEnemyList(Enemy deadEnemy)
    {
        enemyList.Remove(deadEnemy.gameObject);
        Destroy(deadEnemy.gameObject);
        //回血
        if (hpSlider.value < hpSlider.maxValue)
        {
            if (hpSlider.value + deadEnemy.level > hpSlider.maxValue)
            {
                hpSlider.value = hpSlider.maxValue;
            }
            else
            {
                hpSlider.value += deadEnemy.level;
            }
        }
        //回蓝
        if (mpSlider.value < mpSlider.maxValue)
        {
            if (mpSlider.value + deadEnemy.level > mpSlider.maxValue)
            {
                mpSlider.value = mpSlider.maxValue;
            }
            else
            {
                mpSlider.value += deadEnemy.level;
            }
        }
    }

    public void StartNextLevel()
    {
        //更新PlayerPrefs关卡数据
        PlayerPrefs.SetString(Conf.levelNum, nextLevelNum);
        PlayerPrefs.SetInt(Conf.levelType, nextLevelType);
        PlayerPrefs.SetString(Conf.levelVocIndex, nextLevelVocIndex);
    }

    public void OnSkillButtonDown()
    {
        touchPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        touchPos.z = skillCirclePrefab.transform.position.z;
        if (skillCircle == null)
        {
            skillCircle = Instantiate(skillCirclePrefab,
                touchPos,
                Quaternion.Euler(Vector3.zero),
                currLv.transform);
            skillCircle.transform.localScale =
                new Vector3(skillCircleScale, skillCircleScale, 1);
        }
        else
        {
            skillCircle.transform.position = touchPos;
            skillCircle.SetActive(true);
        }

        InvokeRepeating("TouchMove", 0.0f, Time.deltaTime);
    }
    private void TouchMove()
    {
        touchPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        touchPos.z = skillCirclePrefab.transform.position.z;
        skillCircle.transform.position = touchPos;
    }
    public void OnSkillButtonUp(int tag)
    {
        CancelInvoke("TouchMove");
        //遍历所有Enemy，如果enemy距离skillCircle的距离小于半径则enemy被击中
        foreach (GameObject enemy in enemyList)
        {
            if (skillDamageRange >= Vector2.Distance(enemy.transform.position, skillCircle.transform.position))
            {
                var enemyCtrl = enemy.GetComponent<Enemy>();
                switch (tag)
                {
                    case 0:
                        enemyCtrl.Slow();
                        break;
                    case 1:
                        enemyCtrl.Freeze();
                        break;
                    case 2:
                        enemyCtrl.ShowVoc();
                        break;
                }
            }
        }
        skillCircle.SetActive(false);
        //Destroy(skillCircle);
        mpSlider.value -= currLv.skillUseMp[tag];
        RefreshSkillBtState();
    }

    private void RefreshSkillBtState()
    {
        skillBts[0].interactable = mpSlider.value >= currLv.skillUseMp.x;
        skillBts[1].interactable = mpSlider.value >= currLv.skillUseMp.y;
        skillBts[2].interactable = mpSlider.value >= currLv.skillUseMp.z;
    }
}
