using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LobbyUIBehaviour : NetworkBehaviour 
{
    
    public Button m_PlayButton;
    public Text m_NetworkTypeText;
    public Text m_MissingPlayersCountText;
    public NetworkVariable<int> missingPlayersCount = new NetworkVariable<int>(0);

    private void Start()
    {
        missingPlayersCount.OnValueChanged += OnMissingPlayersCountChanged;
    }

    public void SetConnectedType(string type, bool showConnectButton)
    {
        m_NetworkTypeText.text = type;
        m_PlayButton.gameObject.SetActive(showConnectButton);
    }

    public void AddOnClickPlayListener(UnityAction call)
    {
        m_PlayButton.onClick.AddListener(call);
    }

    public void SetConnectedStatus(LobbyManagerBehaviour.ConnectStatus status, bool showConnectButton)
    {
        m_NetworkTypeText.text = status.ToString();
        m_PlayButton.gameObject.SetActive(showConnectButton);
    }

    private void OnMissingPlayersCountChanged(int prevCount, int newCount)
    {
        m_MissingPlayersCountText.text = "Waiting for " + newCount + " players.";
    }

    private IEnumerator WaitToLoadGameScene(float timeToWait)
    {
        // Note sometimes the timeToWait will be negative on the server or the receiving clients if a message got delayed by the network for a long time. This usually happens only in rare cases. Custom logic could be implemented to deal with that scenario.
        if (timeToWait > 0)
        {
            yield return new WaitForSeconds(timeToWait);
        }
        
        Debug.Log("Ready to load new scene!");
    }
        
    [ClientRpc]
    public void StartGameBeginCountdownClientRpc(double time)
    {  
        var timeToWait = time - NetworkManager.ServerTime.Time;
        StartCoroutine(WaitToLoadGameScene((float)timeToWait));
    }

}
