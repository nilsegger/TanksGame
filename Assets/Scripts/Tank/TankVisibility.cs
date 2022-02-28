using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

public class TankVisibility : NetworkBehaviour
{

    public Transform m_Turret;
    public float m_VisibilityForwardOffset = 4.0f;
    public float m_VisibilityRadius = 1.0f;

    private NetworkObject _networkObject;

    private void Start()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        
        _networkObject = GetComponent<NetworkObject>();
        Assert.IsNotNull(_networkObject);
    }
    
    void Update()
    {
        if (!NetworkManager.Singleton.IsServer || !GameManagerBehaviour.GameBegun) return;

        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            if (client.Key == OwnerClientId) continue;
            CheckVisibility(OwnerClientId, client.Key);
        }
    }

    private Vector3 VisibilityCenter()
    {
         return m_Turret.position + m_Turret.forward * m_VisibilityForwardOffset;
    }
    
     private void CheckVisibility(ulong player, ulong other)
     {
         var playerObject = NetworkManager.Singleton.ConnectedClients[player].PlayerObject;
         var otherObject = NetworkManager.Singleton.ConnectedClients[other].PlayerObject;
         bool inDistance = Vector3.Distance(otherObject.transform.position, VisibilityCenter()) <= m_VisibilityRadius;

         if (inDistance)
         {
             Debug.DrawLine(playerObject.transform.position, otherObject.transform.position, Color.green);
         }

         bool isObserver = false;

         var enumerator = NetworkManager.Singleton.ConnectedClients[other].PlayerObject.GetObservers();
         do
         {
             ulong client = enumerator.Current;
             if (client == player)
             {
                 isObserver = true;
                 break;
             }
         } while (enumerator.MoveNext());
         enumerator.Dispose();
         
         if (!isObserver && inDistance)
         {
             NetworkManager.Singleton.ConnectedClients[other].PlayerObject.NetworkShow(player);
         }
         else if(isObserver && !inDistance)
         {
             NetworkManager.Singleton.ConnectedClients[other].PlayerObject.NetworkHide(player);
         }
     }
     
     private void OnDrawGizmos()
     {
        Gizmos.DrawWireSphere(VisibilityCenter(), m_VisibilityRadius);
     }
}
