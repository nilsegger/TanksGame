using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkedTankBehaviour : NetworkBehaviour 
{
    // Start is called before the first frame update

    public Camera m_PlayerCamera; 
    
    void Start()
    {
        NetworkManager.Singleton.SceneManager.OnLoad += OnSceneLoad;
    }

    private void OnSceneLoad(ulong clientid, string scenename, LoadSceneMode loadscenemode, AsyncOperation asyncoperation)
    {
        if (IsOwner)
        {
            if (!scenename.ToLower().Contains("lobby"))
            {
                ActivatePlayerCamera();
            }
        }
    }

    private void ActivatePlayerCamera()
    {
        m_PlayerCamera.gameObject.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
