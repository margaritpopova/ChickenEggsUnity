using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 管理一个带有蛋的巢
/// </summary>

public class Nest : MonoBehaviour
{
    [Tooltip("一个蛋对象，用于在巢中显示")]
    public GameObject eggObj;
    MeshRenderer EggRenderer;

    /// <summary>
    /// 表示巢内部的Collider
    /// </summary>
    [HideInInspector]
    public Collider nestInsideCollider;
       
    // 表示巢壳的实体Collider
    private Collider nestShellCollider; 

    /// <summary>
    /// 一个指向巢外的向量，局部坐标系
    /// </summary>
    public Vector3 NestUpVector
    {
        get
        {
            return nestInsideCollider.transform.up;
        }
    }
    /// <summary>
    /// 巢的中心位置，全局坐标系
    /// </summary>
    public Vector3 NestCenterPosition
    {
        get
        {
            return nestInsideCollider.transform.position;
        }
    }
    /// <summary>
    /// 在巢中孵化蛋所需的时间量
    /// </summary>
    public float TimeAmount { get; private set; }

    /// <summary>
    /// 孵化的蛋数量
    /// </summary>
    public int EggsAmount { get; private set; }

    /// <summary>
    /// 是否还有剩余时间来继续孵化蛋
    /// </summary>
    public bool NeedTime
    {
        get
        {
            return TimeAmount > 0f;
        }
    }

    /// <summary>
    /// 当巢醒来时调用
    /// </summary>
    private void Awake()
    {
        // 查找蛋的mesh renderer 
        EggRenderer = eggObj.GetComponent<MeshRenderer>();

        // 查找蛋的 nest inside and nest shell colliders
        nestInsideCollider = transform.Find("NestInsideCollider").GetComponent<Collider>();
        nestShellCollider = transform.Find("NestShell").GetComponent<Collider>();
    }


    /// <summary>
    /// 尝试孵化蛋
    /// </summary>
    /// <param name="tAmount">花费的时间量</param>
    /// <returns>实际花费的时间值</returns>
    public int Hatch(float tAmount)
    {
        // 追踪孵化所花费的时间（不能超过必要的时间）
        float timeSpended = Mathf.Clamp(tAmount, 0f, TimeAmount);

        // 减去时间
        TimeAmount -= tAmount;

        if (TimeAmount <= 0)
        {
            // 没有剩余时间
            TimeAmount = 0;

            // 禁用巢内部和巢壳的碰撞器
            nestInsideCollider.gameObject.SetActive(false);
            EggRenderer.enabled = true;
            EggsAmount++;
        }

        // 返回花费的时间量
        return EggsAmount;
    }

    /// <summary>
    /// 重新巢
    /// </summary>
    public void ResetNest()
    {
        // 重新填充时间
        TimeAmount = 1f;

        // 启用 nest inside 和 nest shell colliders
        nestInsideCollider.gameObject.SetActive(true);
        //nestShellCollider.gameObject.SetActive(true);

        // 隐藏蛋对象，表示它尚未孵化
        EggRenderer.enabled = false;
        EggsAmount = 0;
    }


}
