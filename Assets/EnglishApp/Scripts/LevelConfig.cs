using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelConfig : MonoBehaviour {
    [HideInInspector]
    public GameObject bg = null;
    [HideInInspector]
    public GameObject wall = null;
    [HideInInspector]
    public Bow bow = null;

    public float hp = 100, mp = 100;//此关的初始hp mp
    
    public Vector3 skillUseMp = new Vector3(10, 30, 20);//三种技能耗蓝数值
    public int mpRecoverySpeed = 2;//自动回蓝,每秒回蓝数量
    public GameObject[] enemyPrefabs;
    public GameObject[] bossPrefabs;
    public float createEnemySpan = 4;//创建敌人的间隔时间
    public int maxEnemyCount = 3;//场景中最大能存在的敌人个数，如果个数已满不再创建敌人
    void Awake () {
        bg = transform.Find("Background").gameObject;
        wall = transform.Find("Wall").gameObject;
        bow = transform.Find("Bow").GetComponent<Bow>();
	}
}
