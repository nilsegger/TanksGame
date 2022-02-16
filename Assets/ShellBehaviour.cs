using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShellBehaviour : MonoBehaviour
{

    public float speed = 20.0f;

    private Rigidbody _body;

    // Start is called before the first frame update
    void Start()
    {
        _body = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        _body.MovePosition(_body.transform.position + _body.transform.forward * speed * Time.fixedDeltaTime); 
    }

    private void OnTriggerEnter(Collider other)
    {
        Destroy(gameObject); 
    }
}
