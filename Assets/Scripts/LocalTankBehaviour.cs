using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

public class LocalTankBehaviour : MonoBehaviour
{

    private NavMeshAgent _agent;

    public Camera m_Camera;
    // Start is called before the first frame update
    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
    }
    
    private void HandleTouch(Vector3 pos)
    {
        Ray ray = m_Camera.ScreenPointToRay(pos);
        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(ray, out hit))
        {
            _agent.SetDestination(hit.point); 
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            HandleTouch(Input.mousePosition);
        }

        if (Input.touchCount > 0)
        {
            HandleTouch(Input.GetTouch(0).position);
        }
    }
}
