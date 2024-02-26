using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class FrontCameraController : MonoBehaviour
{

    //public Transform pilotView; // ǰ�ӽ�

    //// Start is called before the first frame update
    //void Start()
    //{

    //}

    //// Update is called once per frame
    //void Update()
    //{
    //    if (pilotView != null)
    //    {
    //        transform.position = pilotView.position;
    //        transform.rotation = pilotView.rotation;
    //    }
    //}

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

            //transform.LookAt(target);
            //ʼ�ճ���target
        }
    }

}
