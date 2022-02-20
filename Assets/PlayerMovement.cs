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
    public float shellSpawnOffset = 1.3f;
    public float shellReloadTimeS = 1.0f;

    public GameObject shell; 
    
    public Camera _camera;
    
    private FogOfWarBehaviour _fogOfWarBehaviour;
    private UnityEngine.AI.NavMeshAgent _agent;
    
    private Transform _turret;

    private bool _turnLeft = false;
    private bool _turnRight = false;
    private bool _stopAtPosition = false;
    private bool _rotateTurretLeft = false;
    private bool _rotateTurretRight = false;
    private bool _shootShell = false;
    private Vector3 _eyesPosition;
    private List<Vector4> _sightPoints;

    private float _shellReloadTimer = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
        _fogOfWarBehaviour = _camera.GetComponent<FogOfWarBehaviour>();
        _agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        _turret = transform.Find("Tank").Find("TankRenderers").Find("TankTurret");
        _sightPoints = new List<Vector4>(new Vector4[fogOfWarCasts]);
        SetEyesPosition();    
    }

    /** TODO Clicked must not occur for 100 frames in row when mouse is being clicked, create timeout */
    void Clicked() {
        Ray ray = _camera.ScreenPointToRay(Input.mousePosition);

        RaycastHit hit = new RaycastHit();

        if (Physics.Raycast (ray, out hit))
        {
            // Debug.Log("Clicked: " + hit.point);
            _agent.SetDestination(hit.point);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(0)) {
            Clicked();
        }

        _rotateTurretLeft = Input.GetKey("q");
        _rotateTurretRight = Input.GetKey("e");
        _stopAtPosition = Input.GetKey("s");
        _turnLeft = Input.GetKey("a");
        _turnRight = Input.GetKey("d");
        _shootShell = Input.GetKey("space");
        
        Vector3 screenCenterPoint = _camera.WorldToScreenPoint(_eyesPosition);
        _fogOfWarBehaviour.SetCenterPoint(new Vector4(1.0f / _camera.pixelWidth * screenCenterPoint.x , 1.0f / _camera.pixelHeight * screenCenterPoint.y, 0, 0));
        _fogOfWarBehaviour.SetPoints(_sightPoints);

    }

    private void SetEyesPosition()
    {
        _eyesPosition = _turret.position; // - _turret.forward * 1.0f;
    }

    private void RaycastSight()
    {
        for(int i = 0; i < fogOfWarCasts; i++) {
            Vector3 rotatedForward = Quaternion.Euler(0, (-fogOfWarRange / 2.0f) + i * (fogOfWarRange / (float)fogOfWarCasts), 0) * _turret.forward;
            RaycastHit hit = new RaycastHit();
            Vector3 result;
            if (Physics.Raycast (_eyesPosition, rotatedForward, out hit, fogOfWarVisibilityRange))
            {
                result = hit.point;
            } else
            {
                result = _eyesPosition + rotatedForward * fogOfWarVisibilityRange;
            }
            Vector3 screenPoint = _camera.WorldToScreenPoint(result);
            _sightPoints[i] = new Vector4(1.0f / _camera.pixelWidth * screenPoint.x, 1.0f / _camera.pixelHeight * screenPoint.y, 0, 0);
        }
    }

    private void SpawnShell()
    {
        Vector3 shellPosition = _turret.position + _turret.forward * shellSpawnOffset;
        Instantiate(shell, shellPosition, _turret.transform.rotation);
    }

    void FixedUpdate() {
        if(_rotateTurretLeft)  _turret.Rotate(Vector3.up, -turretRotationSpeed * Time.deltaTime);
        if(_rotateTurretRight)  _turret.Rotate(Vector3.up, turretRotationSpeed * Time.deltaTime);
        if(_turnLeft) gameObject.transform.Rotate(Vector3.up, -tankRotationSpeed * Time.deltaTime);
        if(_turnRight) gameObject.transform.Rotate(Vector3.up, tankRotationSpeed * Time.deltaTime);
        if (_stopAtPosition) _agent.SetDestination(gameObject.transform.position);
        if (_shootShell && _shellReloadTimer <= 0.0f)
        {
            SpawnShell();
            _shellReloadTimer = shellReloadTimeS;
        } else if (_shellReloadTimer > 0.0f)
        {
            _shellReloadTimer -= Time.fixedDeltaTime;
        }
        
        SetEyesPosition();
        RaycastSight();
    }
}
