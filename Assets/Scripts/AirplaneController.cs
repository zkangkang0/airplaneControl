using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AirplaneController : MonoBehaviour
{
    //public float speed = 10f;

    

    [Header("Moving speed")]
    [Range(5f, 100f)]
    [SerializeField] private float speed = 10f; //�ɻ��ķ����ٶ�
    //ͨ����˽���ֶ�(private field)�ǲ��ɼ��ģ�ʹ��[SerializeField]������Ϊ�����л���ʹ����Inspector�����п��Ա༭

    [Header("Rotating speeds")]
    [Range(5f, 500f)]
    [SerializeField] private float pitchSpeed = 50f;  // �ɻ������ٶ�
    [SerializeField] private float rollSpeed = 50f;  // �ɻ��Ĺ�ת�ٶ�
    [SerializeField] private float yawSpeed = 50f;  // �ɻ���ƫ���ٶ�

    private float pitchInput;  // ����ĸ���ֵ,��w��s����
    private float rollInput;  // ����Ĺ�תֵ����a��d����
    private float yawInput;   //�����ƫ��ֵ����j��k����



    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        // ��ȡ�������룬
        pitchInput = Input.GetAxis("Vertical"); //����ʹ���w��s
        // ��ȡ��ת����
        rollInput = Input.GetAxis("Horizontal");
        // ��ȡƫ������
        // ��ȡƫ������
        yawInput = Input.GetKey(KeyCode.J) ? -1f : Input.GetKey(KeyCode.K) ? 1f : 0f;


        // �ɻ���ǰ����
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        // Ӧ�ø�����������ת�ɻ�
        float pitchAngle = pitchInput * pitchSpeed * Time.deltaTime;
        transform.Rotate(pitchAngle, 0f, 0f);
        // Ӧ�ù�ת��������ת�ɻ�
        float rollAngle = -rollInput * rollSpeed * Time.deltaTime;
        transform.Rotate(0f, 0f, rollAngle);
        // Ӧ��ƫ����������ת�ɻ�
        float yawAngle = yawInput * yawSpeed * Time.deltaTime;
        transform.Rotate(0f, yawAngle, 0f);






    }
}
