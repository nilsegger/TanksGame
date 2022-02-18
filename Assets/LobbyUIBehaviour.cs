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
    
}
