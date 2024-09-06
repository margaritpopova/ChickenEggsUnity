using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 管理一组树木植物和附加的巢收集
/// </summary>
public class TreesArea : MonoBehaviour
{
    // 代理和树木可以用于观察相对距离的区域直径
    public const float AreaDiameter = 20f;
    
    // 在这片树木区域中所有树木植物的列表（树木植物有多个巢）
    private List<GameObject> treesPlants;

    // 查找字典，用于从巢内部碰撞器（放置蛋的地方）查找巢
    private Dictionary<Collider, Nest> NestNInsideDictionary;

    /// <summary>
    /// 在树木区域中所有巢的列表
    /// </summary>
    public List<Nest> Nests { get; private set; }

    /// <summary>
    /// 重置巢和树
    /// </summary>
    public void ResetTrees()
    {
        // 围绕Y轴和轻微围绕X和Z轴旋转每个树木植物 （为避免过度拟合）
        foreach (GameObject tree in treesPlants)
        {
            float xRotation = UnityEngine.Random.Range(-5f, 5f);
            float yRotation = UnityEngine.Random.Range(-180f, 180f);
            float zRotation = UnityEngine.Random.Range(-5f, 5f);
            tree.transform.localRotation = Quaternion.Euler(xRotation, yRotation, zRotation);
        }

        // 重置每个巢
        foreach (Nest nest in Nests)
        {
            nest.ResetNest();
        }
    }

    /// <summary>
    /// Gets the <see cref="Nest"/> 巢的内部和外部碰撞体的匹配
    /// </summary>
    public Nest GetNestFromCollider(Collider collider)
    {
        return NestNInsideDictionary[collider];
    }

    /// <summary>
    /// 区域唤醒时调用
    /// </summary>
    private void Awake()
    {
        // 初始化变量
        treesPlants = new List<GameObject>();
        NestNInsideDictionary = new Dictionary<Collider, Nest>();
        Nests = new List<Nest>();

        // 找这个游戏对象的所有子巢
        FindChildNest(transform);
    }

    /// <summary>
    /// 递归查找作为父transform子对象的所有巢和树
    /// </summary>
    /// <param name="parent">要检查的子对象的父对象</param>
    private void FindChildNest(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child.CompareTag("tree"))
            {
                // 找到了一个树，把它添加到 treesPlants 列表中
                treesPlants.Add(child.gameObject);

                // Look for nests within the tree
                FindChildNest(child);
            }
            else
            {
                // 不是树，查找 Nest 组件
                Nest nest = child.GetComponent<Nest>();
                if (nest != null)
                {
                    // 找到了一个巢，把它添加到 Nests 列表中
                    Nests.Add(nest);

                    // 将nestInsideCollider添加到查找字典中
                    NestNInsideDictionary.Add(nest.nestInsideCollider, nest);
                }
                else
                {
                    // 未找到巢组件，因此检查子对象
                    FindChildNest(child);
                }
            }
        }
    }        
}
