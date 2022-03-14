using System;
using UnityEngine;
using UnityEngine.AI;

public class LocalTankBehaviour : MonoBehaviour
{

    private NavMeshPath _path;
    private NavMeshAgent _agent;
    

    public float rotationSpeed = 45.0f;
    public float movementSpeed = 3.0f;
    public AnimationCurve turnCurve = AnimationCurve.Linear(0, 0, 0, 1);

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
            _path = new NavMeshPath();
            _agent.CalculatePath(hit.point, _path);
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
        
        FollowPath();
    }

    private void FollowPath()
    {
        if (_path != null && _path.corners.Length > 1)
        {
            var toCorner = _path.corners[1] - transform.position;
            
            if (toCorner.sqrMagnitude < 0.1)
            {
                if (_path.corners.Length > 2)
                {
                    _agent.CalculatePath(_path.corners[_path.corners.Length - 1], _path);
                    toCorner = _path.corners[1] - transform.position;
                }
                else
                {
                    _path = null;
                    return;
                }
            }

            RotateTowardsPath(toCorner, out float slowDown);
            
            var relativeSpeed = toCorner.normalized * movementSpeed * Time.deltaTime * slowDown;
            // this clamps the forward movement vector to point if toCorner is already less
            if (toCorner.sqrMagnitude < relativeSpeed.sqrMagnitude) relativeSpeed = toCorner;
            _agent.Move(relativeSpeed); 
        }
    }

    // returns true if player is allowed to drive
    private void RotateTowardsPath(Vector3 toCorner, out float slowDown)
    {
        float angle = Vector3.Angle(transform.forward, toCorner);
        if (angle > 1.0f)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(toCorner, Vector3.up), rotationSpeed * Time.deltaTime);
        }
        slowDown = turnCurve.Evaluate(1.0f / 180.0f * (180.0f - angle));
    }

    private void OnDrawGizmos()
    {
        if (_path != null && _path.corners.Length > 1)
        {
           // Gizmos.DrawLine(transform.position, _path.corners[0]);
           for (int i = 0; i < _path.corners.Length - 1; i++)
           {
               Gizmos.DrawLine(_path.corners[i], _path.corners[i + 1]);
           }
        }
    }
}
