using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    public int fogOfWarCasts = 40;
    public float fogOfWarRange = 100; // degrees
    public float fogOfWarVisibilityRange = 25.0f;
    public float turretRotationSpeed = 20.0f;
    public float tankRotationSpeed = 15.0f;
    

    private Camera _camera;
    private FogOfWarBehaviour _fogOfWarBehaviour;
    private UnityEngine.AI.NavMeshAgent _agent;
    
    private Transform _turret;

    // Start is called before the first frame update
    void Start()
    {
        _camera = Camera.main;
        _fogOfWarBehaviour = _camera.GetComponent<FogOfWarBehaviour>();
        _agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        _turret = transform.Find("Tank").Find("TankRenderers").Find("TankTurret");
    }

    void Clicked() {
        Ray ray = _camera.ScreenPointToRay(Input.mousePosition);

        RaycastHit hit = new RaycastHit();

        if (Physics.Raycast (ray, out hit))
        {
            Debug.Log("Clicked: " + hit.point);
            _agent.SetDestination(hit.point);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(0)) {
            Clicked();
        }

        if (Input.GetKey("q"))
        {  
            _turret.Rotate(Vector3.up, -turretRotationSpeed * Time.deltaTime);
        }
        
        if (Input.GetKey("e"))
        {  
            _turret.Rotate(Vector3.up, turretRotationSpeed * Time.deltaTime);
        }

        if (Input.GetKey("a"))
        {
            gameObject.transform.Rotate(Vector3.up, -tankRotationSpeed * Time.deltaTime);
        }
        
        if (Input.GetKey("d"))
        {
            gameObject.transform.Rotate(Vector3.up, tankRotationSpeed * Time.deltaTime);
        }

        if (Input.GetKey("s"))
        {
            _agent.SetDestination(gameObject.transform.position);
        }

        Vector3 eyesPosition = _turret.position - _turret.forward * 1.0f;

        List<Vector4> points = new List<Vector4>();

        for(int i = 0; i < fogOfWarCasts; i++) {
            Vector3 rotatedForward = Quaternion.Euler(0, (-fogOfWarRange / 2.0f) + i * (fogOfWarRange / (float)fogOfWarCasts), 0) * _turret.forward;

            RaycastHit hit = new RaycastHit();

            Vector3 result;
            if (Physics.Raycast (eyesPosition, rotatedForward, out hit, fogOfWarVisibilityRange))
            {
                result = hit.point;
                Debug.DrawLine(eyesPosition, hit.point, Color.red);
            } else
            {
                result = eyesPosition + rotatedForward * fogOfWarVisibilityRange;
                Debug.DrawLine(eyesPosition, eyesPosition + rotatedForward * fogOfWarVisibilityRange, Color.green);
            }

            Vector3 screenPoint = _camera.WorldToScreenPoint(result);
            points.Add(new Vector4(1.0f / _camera.pixelWidth * screenPoint.x, 1.0f / _camera.pixelHeight * screenPoint.y, 0, 0));

        }

        Vector3 screenCenterPoint = _camera.WorldToScreenPoint(eyesPosition);
        _fogOfWarBehaviour.SetCenterPoint(new Vector4(1.0f / _camera.pixelWidth * screenCenterPoint.x , 1.0f / _camera.pixelHeight * screenCenterPoint.y, 0, 0));
        _fogOfWarBehaviour.SetPoints(points);

    }

    void FixedUpdate() {

    }
}
