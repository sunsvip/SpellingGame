using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    public int level = 1;
    public float moveSpeedCache= 0.5f;
    public float moveSpeed = 0.5f;
    public float attackSpeed = 2.0f;
    public int attackPower = 5;
    private Animator animCtrl = null;
    private List<Vocabulary> vocList = new List<Vocabulary>();
    private Slider hpSlider = null;
    private Text[] vocText;
    private bool arrive = false;//是否到了 塔下
    private GameController gameCtrl = null;
    void Awake()
    {
        animCtrl = GetComponent<Animator>();

        var animClips = animCtrl.runtimeAnimatorController.animationClips;
        foreach (var clip in animClips)
        {
            //如果动画片段上已经有响应事件了则直接返回 避免重复添加造成多次响应
            if (clip.events.Length > 0)
                break;
            switch (clip.name)
            {
                case "damage":
                    {
                        AnimationEvent animEvent = new AnimationEvent();
                        animEvent.time = clip.length;
                        animEvent.functionName = "DamageAnimEndCallBack";
                        clip.AddEvent(animEvent);
                    }
                    break;
                case "attack":
                    {
                        AnimationEvent animEvent = new AnimationEvent();
                        animEvent.time = clip.length;
                        animEvent.functionName = "AttackAnimEndCallBack";
                        clip.AddEvent(animEvent);
                    }
                    break;
            }
        }

        animCtrl.SetBool(Conf.moveAnim, true);
    }

    private void Start()
    {
        if (2 == gameCtrl.levelType)
        {
            //让单词每隔几秒显现一下
            InvokeRepeating("ShowAndFade", 3.0f, 3.0f);
        }
        else if (3 == gameCtrl.levelType)
        {
            //不显示单词
            vocText[0].enabled = false;
        }
    }
    void Update()
    {
        //Enemy移动
        var animInfo = animCtrl.GetCurrentAnimatorStateInfo(0);
        if (animInfo.IsName("move"))
        {
            if(animCtrl.speed != 0)
            {
                if (animCtrl.speed != moveSpeed)
                    animCtrl.speed = moveSpeed;
                //如果没有移动到塔下 继续前进
                if (!arrive)
                {
                    transform.position += -transform.right * moveSpeed * Time.deltaTime;
                }
            }
        }
        else
        {
            //if (animCtrl.speed != 1)
            //    animCtrl.speed = 1;
            if (animInfo.IsName("death"))
            {
                animCtrl.SetBool(Conf.deathAnim, false);
                if (animInfo.normalizedTime >= 1.0f)
                {
                    gameCtrl.UpdateRemainEnemyList(this);
                    gameObject.SetActive(false);
                    Destroy(gameObject);
                }
            }
        }
    }
    // 如果另一个碰撞器 2D 进入了触发器，则调用 OnTriggerEnter2D (仅限 2D 物理)
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            arrive = true;
            //如果已经走到塔下 停止前进 并攻击
            animCtrl.SetBool(Conf.moveAnim, false);
            animCtrl.SetBool(Conf.attackAnim, true);
        }
    }
    public void InitEnemyData(GameController ctrl)
    {
        gameCtrl = ctrl;
        hpSlider = GetComponentInChildren<Slider>();
        vocText = GetComponentsInChildren<Text>();
        if (gameCtrl.vocList.Count > 0)
        {
            if (level > gameCtrl.vocList.Count)
                level = gameCtrl.vocList.Count;
            for (int i = 0; i < level; i++)
            {
                vocList.Add(gameCtrl.vocList[0]);
                gameCtrl.vocList.RemoveAt(0);
            }

            hpSlider.maxValue = level;
            hpSlider.value = hpSlider.maxValue;
            hpSlider.wholeNumbers = true;
            vocText[0].text = vocList[0].en;
            vocText[1].text = vocList[0].zh;
        }
    }

    void AttackAnimEndCallBack()
    {
        StartCoroutine(WaitAttack());
        //塔受击减血
        gameCtrl.Damage(attackPower);
    }
    IEnumerator WaitAttack()
    {
        animCtrl.SetBool(Conf.attackAnim, false);
        yield return new WaitForSeconds(attackSpeed);
        animCtrl.SetBool(Conf.attackAnim, true);
    }
    void DamageAnimEndCallBack()
    {
        animCtrl.SetBool(Conf.damageAnim, false);
        if (vocList.Count > 0)
            vocList.RemoveAt(0);
        hpSlider.value = vocList.Count;
        if (hpSlider.value < 1)
        {
            //如果血条小于1播放死亡动画
            animCtrl.SetBool(Conf.deathAnim, true);
            var vocWin = transform.Find("VocWin");
            if (vocWin)
                vocWin.gameObject.SetActive(false);
        }
        else
        {
            vocText[0].text = vocList[0].en;
            vocText[1].text = vocList[0].zh;
            animCtrl.SetBool(Conf.moveAnim, true);
        }
        gameCtrl.UpdateRemainVocCount();
    }
    void DeathAnimEndCallBack()
    {
        //如果敌人挂了从列表中移除
        Destroy(gameObject);
        gameCtrl.UpdateRemainEnemyList(this);
    }
    public void Damage()
    {
        animCtrl.SetBool(Conf.damageAnim, true);
        animCtrl.SetBool(Conf.moveAnim, false);
    }
    //被冰冻技能击中时冻结
    public void Freeze()
    {
        StartCoroutine(stopAll());
    }
    private IEnumerator stopAll()
    {
        var oldSpeed = moveSpeedCache;// animCtrl.speed;
        animCtrl.speed = 0;
        yield return new WaitForSeconds(gameCtrl.skillTime);//冻结3秒
        animCtrl.speed = oldSpeed;
    }
    //被减速技能击中
    public void Slow()
    {
        StartCoroutine(SpeedDown());
    }
    private IEnumerator SpeedDown()
    {
        var mSpeed = moveSpeedCache;// moveSpeed;
        moveSpeed /= 4;
        yield return new WaitForSeconds(gameCtrl.skillTime);
        moveSpeed = mSpeed;
    }
    public void ShowVoc()
    {
        StartCoroutine(TipVoc());
    }
    private IEnumerator TipVoc()
    {
        vocText[0].enabled = true;
        yield return new WaitForSeconds(gameCtrl.skillTime);
        vocText[0].enabled = false;
    }
    private void ShowAndFade()
    {
        vocText[0].enabled = !vocText[0].enabled;
    }
}
