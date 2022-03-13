using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameBasicUIBehaviour : NetworkBehaviour 
{

    public Text m_CountdownText;
    public Text m_PingText;
    public float m_UpdatePingEveryS = 1.0f;
    public float m_BadPingLimitMS = 250;

    private float _countdown = 0.0f;

    private bool _awaitingPingResponse = false;
    private double _lastRtt = 0.0f;
    private double _pingSendTime = 0.0f;
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
            if (_awaitingPingResponse == false && _pingCooldown <= 0.0f)
            {
                SendPingRequest();
            }
            else
            {
                _pingCooldown -= Time.deltaTime;
            }

            m_PingText.text = ((int)(_lastRtt)).ToString() + "ms";
            m_PingText.color = Color.Lerp(Color.green, Color.red, 1.0f / (float)m_BadPingLimitMS * (float)_lastRtt);
        }
    }
    
    private void SendPingRequest()
    {
        _awaitingPingResponse = true;
        _pingSendTime = NetworkManager.LocalTime.Time;
        PingRequestServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void PingRequestServerRpc(ServerRpcParams serverRpcParams = default)
    {
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
               TargetClientIds = new ulong[]{serverRpcParams.Receive.SenderClientId}
            }
        };
        PingResponseClientRpc(clientRpcParams);
    }

    [ClientRpc]
    private void PingResponseClientRpc(ClientRpcParams clientRpcParams = default)
    {
        _lastRtt = (NetworkManager.LocalTime.Time - _pingSendTime) * 1000.0;
        _pingCooldown = m_UpdatePingEveryS;
        _awaitingPingResponse = false;
    }
}
