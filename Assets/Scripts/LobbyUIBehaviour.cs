using System;
using Unity.Netcode;
using UnityEngine.Events;
using UnityEngine.UI;

public class LobbyUIBehaviour : NetworkBehaviour 
{
    
    public Button m_PlayButton;
    public Button m_DisconnectButton;
    public Text m_NetworkTypeText;
    public Text m_MissingPlayersCountText;
    public NetworkVariable<int> missingPlayersCount = new NetworkVariable<int>(0);

    public InputField m_ServerIpInput;
    public InputField m_ServerPortInput;

    private void Start()
    {
        m_ServerIpInput.text = "127.0.0.1";
        m_ServerPortInput.text = "7777";
    }

    public string ServerIp()
    {
        return m_ServerIpInput.text;
    }
    
    public bool ServerPort(out int port)
    {
        return int.TryParse(m_ServerPortInput.text, out port);
    }
    
    public override void OnNetworkSpawn()
    {
        missingPlayersCount.OnValueChanged += OnMissingPlayersCountChanged;
    }

    public override void OnNetworkDespawn()
    {
        missingPlayersCount.OnValueChanged -= OnMissingPlayersCountChanged;
    }

    public void SetNetworkStatusText(string text)
    {
        m_NetworkTypeText.text = text;
    }

    public void SetPlayButtonVisibility(bool show)
    {
        m_PlayButton.gameObject.SetActive(show);
    }
    
    public void SetDisconnectButtonVisibility(bool show)
    {
        m_DisconnectButton.gameObject.SetActive(show);
    }

    public void AddOnClickPlayListener(UnityAction call)
    {
        m_PlayButton.onClick.AddListener(call);
    }
    
    public void AddOnClickDisconnectListener(UnityAction call)
    {
        m_DisconnectButton.onClick.AddListener(call);
    }

    private void OnMissingPlayersCountChanged(int prevCount, int newCount)
    {
        m_MissingPlayersCountText.text = "Waiting for " + newCount + " players.";
    }

}
