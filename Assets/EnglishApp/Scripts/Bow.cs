using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bow : MonoBehaviour
{
    public GameObject[] arowPrefabs = null;
    public float arowSpeed = 15.0f;

    //朝目标射击
    public void Shoot(Transform target)
    {
        var dir = target.position - transform.position;
        var rotate = Mathf.Atan(dir.y / dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, rotate);
        //旋转到指定角度后射箭
        int arowIndex = Random.Range(0, arowPrefabs.Length);
        var arow = Instantiate(arowPrefabs[arowIndex], transform.position, transform.rotation) as GameObject;
        StartCoroutine(ArowMove(arow.transform, target));
    }
    IEnumerator ArowMove(Transform arow, Transform target)
    {
        while (!(Vector3.Distance(arow.position, target.position) <= 0.1f))
        {
            arow.transform.position = Vector3.MoveTowards(arow.position, target.position, Time.deltaTime * arowSpeed);
            yield return new WaitForFixedUpdate();
        }
        Destroy(arow.gameObject);
        target.GetComponent<Enemy>().Damage();
    }
}
