using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayfabBehaviour : MonoBehaviour
{

    public Text m_InfoText;

    private string RandomCustomId()
    {
        string result = "test_";
        const string glyphs= "abcdefghijklmnopqrstuvwxyz0123456789";
        for(int i=0; i < 4; i++)
        {
            result += glyphs[Random.Range(0, glyphs.Length)];
        }
        return result;
    }

    private void SignInUsingDeviceId()
    {

        /*
        if (Application.platform == RuntimePlatform.Android)
        {
            PlayFabClientAPI.LoginWithAndroidDeviceID(new LoginWithAndroidDeviceIDRequest(),
                result =>
                {
                    PlayfabPersistenceData.AuthEntityToken = result.EntityToken;
                    CheckIfServerOnline();
                }, error =>
                {
                    m_InfoText.text = "Failed to authenticate device.";
                    Debug.Log(error.GenerateErrorReport());
                });
        } else */ if (Application.platform == RuntimePlatform.WindowsEditor)
        {
            var request = new LoginWithCustomIDRequest
            {
                CustomId = RandomCustomId(),
                CreateAccount = true 
            };
            
            PlayFabClientAPI.LoginWithCustomID(request, result =>
                {
                    PlayfabPersistenceData.AuthEntityToken = result.EntityToken;
                    CheckIfServerOnline();
                },
                error =>
                {
                    m_InfoText.text = "Failed to authenticate device.";
                    Debug.Log(error.GenerateErrorReport());
                });
        }
    }
   
    private void CheckIfServerOnline() {
        PlayFabClientAPI.GetTitleData(new GetTitleDataRequest(),
            result => {
                if (result.Data == null || !result.Data.ContainsKey("ServersOnline"))
                {
                    m_InfoText.text = "Received faulty info from servers.";
                }
                else if (result.Data["ServersOnline"].Equals("false"))
                {
                    m_InfoText.text = "Servers are offline.";
                }
                else
                {
                    LoadMainMenu();
                }
            },
            error =>
            {
                m_InfoText.text = "Failed to fetch data.";
                Debug.Log(error.GenerateErrorReport());
            }
        );
    }

    private void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
    
    // Start is called before the first frame update
    void Start()
    {
        m_InfoText.text = "Checking Server Status...";
       SignInUsingDeviceId();
       PlayfabPersistenceData.IsUsingPlayFab = true;
    }
    
}
