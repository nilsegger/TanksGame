using System.Numerics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class TankVisibility : NetworkBehaviour
{

    public Transform m_Eyes;
    
    public float m_VisibilityRange = 15.0f;
    public float m_AngleRange = 180.0f;

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

    private float CalculateDegreesBetweenPlayerSightAndOther(NetworkObject player, NetworkObject other)
    {
        Vector2 xzPlayer = new Vector2(player.transform.position.x, player.transform.position.z);
        Vector2 xzOther = new Vector2(other.transform.position.x, other.transform.position.z);
        var otherForward = xzOther - xzPlayer;

        var p1 = new Vector2(m_Eyes.forward.x, m_Eyes.forward.z) * otherForward.magnitude;
        var p2 = otherForward;

        return (Mathf.Atan2(p1.y, p1.x) - Mathf.Atan2(p2.y, p2.x)) * Mathf.Rad2Deg;
    }
    
     private void CheckVisibility(ulong player, ulong other)
     {
         var playerObject = NetworkManager.Singleton.ConnectedClients[player].PlayerObject;
         var otherObject = NetworkManager.Singleton.ConnectedClients[other].PlayerObject;
         
         var distance = Vector3.Distance(playerObject.transform.position, otherObject.transform.position);
         
         bool inDistance = distance <= m_VisibilityRange;

         if (inDistance)
         {
             var degreesBetween = CalculateDegreesBetweenPlayerSightAndOther(playerObject, otherObject);
             inDistance = -m_AngleRange / 2.0f <= degreesBetween && degreesBetween <= m_AngleRange / 2.0f;
             
             Debug.DrawLine(m_Eyes.position, m_Eyes.position + (m_Eyes.rotation * Quaternion.Euler(0.0f, degreesBetween, 0.0f) * Vector3.forward) * m_VisibilityRange, inDistance ? Color.green : Color.red);
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
     
}
