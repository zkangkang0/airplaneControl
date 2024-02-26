using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{

    public Transform target; // �ɻ������Transform
    public float smoothSpeed = 0.125f; // ��������ƽ���ٶ�

    private Vector3 offset;
    private Vector3 desiredPosition;


    // Start is called before the first frame update
    void Start()
    {
        if (target != null)
        {
            offset = transform.position - target.position;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (target != null)
        {
            desiredPosition = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

            transform.LookAt(target);
            //ʼ�ճ���target
        }
    }

}
