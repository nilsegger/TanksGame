using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LobbyUIBehaviour : MonoBehaviour 
{
    
    public Button m_PlayButton;
    public Button m_DisconnectButton;
    public Button m_StartServer;
    public Button m_ServerStartGame;
    public Text m_NetworkTypeText;
    public Text m_MissingPlayersCountText;

    public InputField m_ServerIpInput;
    public InputField m_ServerPortInput;

    private void Start()
    {
        #if UNITY_EDITOR
            m_ServerIpInput.text = "127.0.0.1";
            m_ServerPortInput.text = "7777";
        #endif
    }

    public string ServerIp()
    {
        return m_ServerIpInput.text;
    }
    
    public bool ServerPort(out int port)
    {
        return int.TryParse(m_ServerPortInput.text, out port);
    }

    public void SetNetworkStatusText(string text)
    {
        m_NetworkTypeText.text = text;
    }

    public void SetPlayButtonVisibility(bool show)
    {
        m_PlayButton.gameObject.SetActive(show);
        m_ServerIpInput.gameObject.SetActive(show);
        m_ServerPortInput.gameObject.SetActive(show);
    }
    
    public void SetStartServerBtnVisibility(bool show)
    {
        m_StartServer.gameObject.SetActive(show);
    }
    
    public void SetStartGameVisibility(bool show)
    {
        m_ServerStartGame.gameObject.SetActive(show);
    }
        
    public void SetDisconnectButtonVisibility(bool show)
    {
        // m_DisconnectButton.gameObject.SetActive(show);
    }

    public void AddOnClickPlayListener(UnityAction call)
    {
        m_PlayButton.onClick.AddListener(call);
    }
    
    public void AddOnClickDisconnectListener(UnityAction call)
    {
        m_DisconnectButton.onClick.AddListener(call);
    }

    public void SetMissingPlayersCount(int missingCount)
    {
        m_MissingPlayersCountText.text = "Waiting for " + missingCount + " players.";
    }

}
