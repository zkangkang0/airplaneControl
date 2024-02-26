using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AirplaneController : MonoBehaviour
{
    //public float speed = 10f;

    

    [Header("Moving speed")]
    [Range(5f, 100f)]
    [SerializeField] private float speed = 10f; //飞机的飞行速度
    //通常，私有字段(private field)是不可见的，使用[SerializeField]将其标记为可序列化，使其在Inspector窗口中可以编辑

    [Header("Rotating speeds")]
    [Range(5f, 500f)]
    [SerializeField] private float pitchSpeed = 50f;  // 飞机俯仰速度
    [SerializeField] private float rollSpeed = 50f;  // 飞机的滚转速度
    [SerializeField] private float yawSpeed = 50f;  // 飞机的偏航速度

    private float pitchInput;  // 输入的俯仰值,用w和s控制
    private float rollInput;  // 输入的滚转值，用a和d控制
    private float yawInput;   //输入的偏航值，用j和k控制



    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        // 获取俯仰输入，
        pitchInput = Input.GetAxis("Vertical"); //这个就代表w和s
        // 获取滚转输入
        rollInput = Input.GetAxis("Horizontal");
        // 获取偏航输入
        // 获取偏航输入
        yawInput = Input.GetKey(KeyCode.J) ? -1f : Input.GetKey(KeyCode.K) ? 1f : 0f;


        // 飞机向前飞行
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        // 应用俯仰输入来旋转飞机
        float pitchAngle = pitchInput * pitchSpeed * Time.deltaTime;
        transform.Rotate(pitchAngle, 0f, 0f);
        // 应用滚转输入来旋转飞机
        float rollAngle = -rollInput * rollSpeed * Time.deltaTime;
        transform.Rotate(0f, 0f, rollAngle);
        // 应用偏航输入来旋转飞机
        float yawAngle = yawInput * yawSpeed * Time.deltaTime;
        transform.Rotate(0f, yawAngle, 0f);






    }
}
