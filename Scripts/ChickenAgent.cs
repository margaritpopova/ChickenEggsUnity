using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

/// <summary>
/// A chicken Machine Learning Agent
/// </summary>
public class ChickenAgent : Agent
{
    [Tooltip("移动时施加的力量")]
    public float moveForce = 2f;
    
    [Tooltip("上下倾斜的速度")]
    public float tiltSpeed = 100f;

    [Tooltip("围绕上轴旋转的速度")]
    public float turnSpeed = 100f;

    [Tooltip("鸡爪的空间特性")]
    public Transform chickenPaw;

    [Tooltip("智能体的相机")]
    public Camera agentCamera;

    [Tooltip("是否是训练模式")]
    public bool trainingMode;

    [Tooltip("代理的 rigidbody")]
    new private Rigidbody rigidbody;

    [Tooltip("代理所在的树木区域")]
    private TreesArea treesArea;

    [Tooltip("距离代理最近的巢穴")]
    private Nest nearestNest;

    // 可做平稳的倾斜和转向变化
    private float smoothtiltChange = 0f;
    private float smoothturnChange = 0f;

    // 鸡可以向上下倾斜的最大角度
    private const float MaxtiltAngle = 30f;

    // 接受鸡爪与巢穴碰撞内的最大距离
    private const float PawsRadius = 0.008f;

    // 代理是否被冻结（故意不飞行）
    private bool frozen = false;

    /// <summary>
    ///  
    /// 代理本轮收到的蛋的数量
    /// </summary>
    public int eggObtained { get; private set; }

    /// <summary>
    /// 初始化智能体。
    /// </summary>
    public override void Initialize()
    {
        rigidbody = GetComponent<Rigidbody>();
        treesArea = GetComponentInParent<TreesArea>();

        // 如果不是训练模式，没有最大步数，永远播放
        if (!trainingMode) MaxStep = 0;
    }

    /// <summary>
    /// 在每个情节开始时重置代理，重置蛋的数量，如果需要的话也重置树木。
    /// </summary>
    public override void OnEpisodeBegin()
    {
        if (trainingMode)
        {
            // 只有在训练中更新树木
            treesArea.ResetTrees();
        }

        // 更新鸡蛋数量
        eggObtained = 0;

        // 在新一轮开始之前将速度归零，以使运动停止
        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
        

        // 默认生成在树的前面
        bool inFrontOfTree = true;
        if (trainingMode)
        {
            // 在训练过程中，有50%的概率生成在树的前面
            inFrontOfTree = UnityEngine.Random.value > .5f;
        }

        // 将代理移动到一个新的随机位置
        MoveToSafeRandomPosition(inFrontOfTree);

        // 现在代理已经移动，重新计算最近的树
        UpdateNearestNest();
    }
    
    /// <summary>
    /// 当接收到来自玩家输入或神经网络的动作时调用。
    /// 
    /// actions.ContinuousActions[i] represents:
    /// Index 0: move vector x (+1 = right, -1 = left)
    /// Index 1: move vector y (+1 = up, -1 = down)
    /// Index 2: move vector z (+1 = forward, -1 = backward)
    /// Index 3: tilt angle (+1 = tilt up, -1 = tilt down)
    /// Index 4: turn angle (+1 = turn right, -1 = turn left)
    /// </summary>
    /// <param name="vectorAction">采取的行动</param>

    public override void OnActionReceived (ActionBuffers actions)
    {
        // 如果被冻结，则不执行任何操作
        if (frozen) return;

        // 计算移动向量
        Vector3 move = new Vector3(actions.ContinuousActions[0], actions.ContinuousActions[1], actions.ContinuousActions[2]);//(vectorAction[0], vectorAction[1], vectorAction[2]); old version


        // 在移动向量方向上施加力量
        rigidbody.AddForce(move * moveForce);

        // 获取当前的旋转
        Vector3 rotationVector = transform.rotation.eulerAngles;

        // 计算倾斜和转向旋转
        float tiltChange = actions.ContinuousActions[3];
        float turnChange = actions.ContinuousActions[4];

        // 计算平滑的旋转变化
        smoothtiltChange = Mathf.MoveTowards(smoothtiltChange, tiltChange, 2f * Time.fixedDeltaTime);
        smoothturnChange = Mathf.MoveTowards(smoothturnChange, turnChange, 2f * Time.fixedDeltaTime);

        // 根据平滑值计算新的倾斜和转向
        // 将倾斜限制在一定范围内，以避免翻转
        float tilt = rotationVector.x + smoothtiltChange * Time.fixedDeltaTime * tiltSpeed;
        if (tilt > 180f) tilt -= 360f;
        tilt = Mathf.Clamp(tilt, -MaxtiltAngle, MaxtiltAngle);

        float turn = rotationVector.y + smoothturnChange * Time.fixedDeltaTime * turnSpeed;

        // 应用新的旋转
        transform.rotation = Quaternion.Euler(tilt, turn, 0f);
    }

    /// <summary>
    /// 从环境中收集向量观测，总共有 10 个观测。
    /// </summary>
    /// <param name="sensor">向量传感器</param>
    public override void CollectObservations(VectorSensor sensor)
    {
        // 如果最近的巢穴不存在，则返回一个空数组
        if (nearestNest == null)
        {
            sensor.AddObservation(new float[10]);
            return;
        }
        
        // 观察代理的局部旋转（四元数包含四个观察值）
        sensor.AddObservation(transform.localRotation.normalized);

        // 获取从鸡腿到最近巢穴的方向向量
        Vector3 toNest = nearestNest.NestCenterPosition - chickenPaw.position;

        // 将该向量添加到观察中（Vector3 包含 3 个观察）
        sensor.AddObservation(toNest.normalized);

        // 矢量叉积，观察鸡腿是否位于巢穴前方（1个观察）
        // (+1 表示鸡脚直接在巢穴前方，-1 表示直接在其后方)
        sensor.AddObservation(Vector3.Dot(toNest.normalized, -nearestNest.NestUpVector.normalized));

        // 矢量叉积，观察鸡脚是否指向巢穴方向（1 个观察）
        // (+1 表示鸡脚直接指向巢穴，-1 表示直接指向远离巢穴)
        sensor.AddObservation(Vector3.Dot(chickenPaw.forward.normalized, -nearestNest.NestUpVector.normalized));

        // 观察鸡腿到巢穴的相对距离（1 个观察）
        sensor.AddObservation(toNest.magnitude / TreesArea.AreaDiameter);

        // 总共有 10 个观察
    }

    /// <summary>
    ///当行为类型设置为“Heuristic Only”时，智能体的行为参数上会调用此函数。
    /// 其返回值将被输入到系统。
    /// <see cref="OnActionReceived(float[])"/> 代替使用神经网络
    /// </summary>
    /// <param name="actionsOut">输出动作数组</param>


    public override void Heuristic(in ActionBuffers actionsOut) //(float[] actionsOut)
    {
        // 为所有移动/旋转创建占位符
        Vector3 forward = Vector3.zero;
        Vector3 left = Vector3.zero;
        Vector3 up = Vector3.zero;
        float tilt = 0f;
        float turn = 0f;
        var continuousActionsOut = actionsOut.ContinuousActions;

        // 将键盘和鼠标输入转换为移动和转向
        // 所有值应该在 -1 和 +1 之间

        // Forward/backward
        if (Input.GetKey(KeyCode.W)) forward = transform.forward;
        else if (Input.GetKey(KeyCode.S)) forward = -transform.forward;

        // Left/right
        if (Input.GetKey(KeyCode.A)) left = -transform.right;
        else if (Input.GetKey(KeyCode.D)) left = transform.right;

        // Up/down
        if (Input.GetKey(KeyCode.E)) up = transform.up;
        else if (Input.GetKey(KeyCode.C)) up = -transform.up;

        // tilt up/down
        if (Input.GetAxis("Mouse Y") < 0f) tilt = 1f;
        else if (Input.GetAxis("Mouse Y") > 0f) tilt = -1f;

        // Turn left/right
        if (Input.GetAxis("Mouse X") < 0f) turn = -1f;
        else if (Input.GetAxis("Mouse X") > 0f) turn = 1f;

        // 结合移动向量并进行归一化
        Vector3 combined = (forward + left + up).normalized;

        // 将3个移动值、倾斜和转向添加到动作数组
        continuousActionsOut[0] = combined.x;
        continuousActionsOut[1] = combined.y;
        continuousActionsOut[2] = combined.z;
        continuousActionsOut[3] = tilt;
        continuousActionsOut[4] = turn;
    }

    /// <summary>
    /// 防止NPC移动和执行动作。
    /// </summary>
    public void FreezeAgent()
    {
        Debug.Assert(trainingMode == false, "Freeze/Unfreeze not supported in training");
        frozen = true;
        rigidbody.Sleep();
    }

    /// <summary>
    /// 恢复NPC的移动和动作。
    /// </summary>
    public void UnfreezeAgent()
    {
        Debug.Assert(trainingMode == false, "Freeze/Unfreeze not supported in training");
        frozen = false;
        rigidbody.WakeUp();
    }

    /// <summary>
    /// 将鸡移动到一个安全的随机位置（即不会与任何物体碰撞）。
    /// 如果在树前，还要将脚爪指向巢穴。
    /// </summary>
    /// <param name="inFrontOfNest">是否选择巢穴前的一个位置</param>
    private void MoveToSafeRandomPosition(bool inFrontOfNest)
    {
        bool safePositionFound = false;
        int attemptsRemaining = 100; // 防止无限循环
        Vector3 potentialPosition = Vector3.zero;
        Quaternion potentialRotation = new Quaternion();

        // 循环直到找到安全位置或尝试次数用尽
        while (!safePositionFound && attemptsRemaining > 0)
        {
            attemptsRemaining--;
            if (inFrontOfNest)
            {
                // 选择一个随机的巢穴
                Nest randomNest = treesArea.Nests[UnityEngine.Random.Range(0, treesArea.Nests.Count)];

                // 在巢穴前方找 70 到 150 厘米的位置
                float distanceFromNest = UnityEngine.Random.Range(.7f, 1.5f);

                // 获取与 NestUpVector 垂直的向量
                Vector3 perpendicularVector = Vector3.Cross(randomNest.NestUpVector, Vector3.up).normalized;

                // 将其旋转90度
                Vector3 rotatedVector = Quaternion.AngleAxis(70, randomNest.NestUpVector) * perpendicularVector;

                // 使用旋转后的向量计算新位置
                potentialPosition = randomNest.transform.position + rotatedVector * distanceFromNest;

                // 点在巢穴处（鸡头为变换的中心）
                Vector3 toNest = randomNest.NestCenterPosition - potentialPosition;
                potentialRotation = Quaternion.LookRotation(toNest, Vector3.up);    
            }
            else
            {
                // 从地面选择一个随机高度 （120到250厘米）
                float height = UnityEngine.Random.Range(1.2f, 2.5f);

                // 从区域中心选择一个随机半径
                float radius = UnityEngine.Random.Range(2f, 7f);

                // 选择一个围绕 y 轴旋转的随机方向
                Quaternion direction = Quaternion.Euler(0f, UnityEngine.Random.Range(-180f, 180f), 0f);

                // 结合高度、半径和方向选择一个潜在位置
                potentialPosition = treesArea.transform.position + Vector3.up * height + direction * Vector3.forward * radius;

                // 选择并设置随机的起始倾斜和转向
                float tilt = UnityEngine.Random.Range(-7f, 7f);
                float turn = UnityEngine.Random.Range(-7f, 7f);
                potentialRotation = Quaternion.Euler(tilt, turn, 0f);
            }

            // 检查代理是否会与任何物体碰撞
            Collider[] colliders = Physics.OverlapSphere(potentialPosition, 0.05f);

            // 如果没有重叠的碰撞体，则安全位置是已找到的
            safePositionFound = colliders.Length == 0;
        }

        Debug.Assert(safePositionFound, "Could not find a safe position to spawn");

        // 设置位置和旋转
        transform.position = potentialPosition;
        transform.rotation = potentialRotation;
    }

    /// <summary>
    /// 更新距离鸡最近的巢穴。
    /// </summary>
    private void UpdateNearestNest()
    {
        foreach (Nest nest in treesArea.Nests)
        {
            if (nearestNest == null && nest.NeedTime)
            {
                // 当前没有最近的巢穴，并且此巢穴仍然没有蛋，因此设置为此巢穴
                nearestNest = nest;
            }
            else if (nest.NeedTime)
            {
                // 计算到当前巢穴和当前最近巢穴的距离
                float distanceToNest = Vector3.Distance(nest.transform.position, chickenPaw.position);
                float distanceToCurrentNearestNest = Vector3.Distance(nearestNest.transform.position, chickenPaw.position);

                // 如果当前最近的巢穴有蛋或者这个巢穴更近，则更新最近的巢穴
                if (!nearestNest.NeedTime || distanceToNest < distanceToCurrentNearestNest)
                {
                    nearestNest = nest;
                }
            }
        }
       
    }

    /// <summary>
    /// 当NPC的碰撞体进入触发器碰撞体时调用。
    /// </summary>
    /// <param name="other">The trigger collider</param>
    private void OnTriggerEnter(Collider other)
    {
        TriggerEnterOrStay(other);
    }

    /// <summary>
    /// 当NPC的碰撞体停留在触发器碰撞体中时调用。
    /// </summary>
    /// <param name="other">The trigger collider</param>
    private void OnTriggerStay(Collider other)
    {
        TriggerEnterOrStay(other);
    }

    /// <summary>
    /// 处理当代理的碰撞体进入或停留在触发器碰撞体中
    /// </summary>
    /// <param name="collider">The trigger collider</param>
    private void TriggerEnterOrStay(Collider collider)
    {
        // 检查代理是否与内部的巢穴碰撞
        if (collider.CompareTag("EggPlace"))
        {
            Vector3 closestPointToChickenPaw = collider.ClosestPoint(chickenPaw.position);

            // 处理鸡的碰撞体进入或停留在触发器碰撞体中时的情况。计算孵化蛋的奖励。
            // 注意：除了鸡爪之外的任何碰撞都不应计入。
            if (Vector3.Distance(chickenPaw.position, closestPointToChickenPaw) < PawsRadius)
            {
                //查找此触发器的巢穴
                Nest nest = treesArea.GetNestFromCollider(collider);

                // 尝试孵化蛋，持续0.01时间周期
                // 注意：这发生在固定的时间周期内，即每0.02秒一次，或每秒50次
                int timeReceived = nest.Hatch(.01f);

                // 跟踪已经过的时间量
                eggObtained += timeReceived;

                if (trainingMode)
                {
                    // 计算孵化蛋的奖励
                    float bonus = .02f * Mathf.Clamp01(Vector3.Dot(transform.forward.normalized, -nearestNest.NestUpVector.normalized));
                    AddReward(.01f + bonus);
                }

                // 如果巢穴有蛋，更新最近的巢穴
                if (!nest.NeedTime)
                {
                    UpdateNearestNest();
                }
            }
        }
    }

    /// <summary>
    /// 当NPC与固体物体发生碰撞时调用
    /// </summary>
    /// <param name="collision">The collision info</param>
    private void OnCollisionEnter(Collision collision)
    {
        if (trainingMode && collision.collider.CompareTag("boundary"))
        {
            // 与区域边界碰撞，给予负面奖励
            AddReward(-.5f);
        }
    }

    /// <summary>
    /// 在每一帧被调用，从鸡爪到最近的巢穴画一条线，用于可视化和调试。
    /// </summary>
    private void Update()
    {
        // 从鸡爪到最近的巢穴画一条线 （为了方便观察）
        if (nearestNest != null)
            Debug.DrawLine(chickenPaw.position, nearestNest.NestCenterPosition, Color.green);
    }

    /// <summary>
    /// 每0.02秒调用一次，避免最近的巢穴中的蛋被对手孵化而未更新的情况。
    /// </summary>
    private void FixedUpdate()
    {
        if (nearestNest != null && !nearestNest.NeedTime)
            UpdateNearestNest();
    }
}
