using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameBasicUIBehaviour : NetworkBehaviour 
{

    public Text m_CountdownText;
    public float m_UpdatePingEveryS = 1.0f;

    private float _countdown = 0.0f;

    private Dictionary<int, double> _pingsSendTime = new Dictionary<int, double>();
    private int _nextPingId = 0;
    private float _pingCooldown = 0.0f;
    

    private IEnumerator WaitToBeginCountdown(double waitTime, float countdownTime)
    {
        if (waitTime > 0.0)
        {
            yield return new WaitForSeconds((float) waitTime);
        }
        _countdown = countdownTime;
    }
    
    [ClientRpc]
    public void SetGameCountdownClientRpc(double beginAtTime, float countdownTime)
    {
        double waitFor = beginAtTime - NetworkManager.ServerTime.Time;
        StartCoroutine(WaitToBeginCountdown(waitFor, countdownTime));
    }
    
    // Update is called once per frame
    void Update()
    {
        if (_countdown > 0.0f)
        {
            m_CountdownText.gameObject.SetActive(true);
            m_CountdownText.text = ((int)_countdown).ToString();
            _countdown -= Time.deltaTime;
        }
        else
        {
            m_CountdownText.gameObject.SetActive(false);
        }

        if (GameManagerBehaviour.GameBegun)
        {
            if (_pingCooldown <= 0.0f)
            {
                SendPingRequest();
                _pingCooldown = m_UpdatePingEveryS;
            }
            else
            {
                _pingCooldown -= Time.deltaTime;
            }
        }
    }
    
    private void SendPingRequest()
    {
        _pingsSendTime[_nextPingId] = NetworkManager.LocalTime.Time;
        PingRequestServerRpc(_nextPingId);
        _nextPingId++;
    }

    [ServerRpc(RequireOwnership = false)]
    private void PingRequestServerRpc(int pingId, ServerRpcParams serverRpcParams = default)
    {
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
               TargetClientIds = new ulong[]{serverRpcParams.Receive.SenderClientId}
            }
        };
        PingResponseClientRpc(pingId, clientRpcParams);
    }

    [ClientRpc]
    private void PingResponseClientRpc(int pingId, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log(NetworkManager.LocalTime.Time - _pingsSendTime[pingId]);
        _pingsSendTime.Remove(pingId);
    }
}
