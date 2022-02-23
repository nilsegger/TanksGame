using Unity.Netcode;
using UnityEngine;

public struct PlayerInput : INetworkSerializable
{
     public Vector3 destination;
     public bool requestsToShoot;
     public bool rotateTurretLeft;
     public bool rotateTurretRight;
     public bool rotateTankLeft;
     public bool rotateTankRight;

     public void Reset()
     {
         destination = Vector3.zero;
         requestsToShoot = false;
         rotateTurretLeft = false;
         rotateTurretRight = false;
         rotateTankLeft = false;
         rotateTankRight = false;
     }

     public bool ContainsAction()
     {
         return destination != Vector3.zero || requestsToShoot || rotateTurretLeft || rotateTurretRight || rotateTankLeft || rotateTankRight;
     }

     public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
     {
         serializer.SerializeValue(ref destination);
         serializer.SerializeValue(ref requestsToShoot);
         serializer.SerializeValue(ref rotateTurretLeft);
         serializer.SerializeValue(ref rotateTurretRight);
         serializer.SerializeValue(ref rotateTankLeft);
         serializer.SerializeValue(ref rotateTankRight);
     }

     /*
     public void MergeNewer(PlayerInput other)
     {
         if (other.destination != Vector3.zero) destination = other.destination;
         requestsToShoot |= other.requestsToShoot;
         rotateTurretLeft |= other.rotateTurretLeft;
         rotateTurretRight |= other.rotateTurretRight;
         rotateTankLeft |= other.rotateTankLeft;
         rotateTankRight|= other.rotateTankRight;
     }   
     */
}