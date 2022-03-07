using System.Collections;
using PlayFab;
using PlayFab.MultiplayerModels;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MatchMakingBehaviour : MonoBehaviour
{

    public Button m_StartMatchmakingButton;
    public Text m_InfoText;

    public float m_TicketStateCheckCooldown = 5.0f;
    
    // Start is called before the first frame update
    void Start()
    {
        m_StartMatchmakingButton.onClick.AddListener(StartMatchMaking);    
    }

    private void DisplayInfo(string text, bool showButton)
    {
        m_StartMatchmakingButton.gameObject.SetActive(showButton);
        m_InfoText.text = text;
    }

    private void StartMatchMaking()
    {
        DisplayInfo("Starting matchmaking...", false);
        
        PlayFabMultiplayerAPI.CreateMatchmakingTicket(
            new CreateMatchmakingTicketRequest
            {
                // The ticket creator specifies their own player attributes.
                Creator = new MatchmakingPlayer
                {
                    Entity = new EntityKey
                    {
                        Id = PlayfabPersistenceData.AuthEntityToken.Entity.Id,
                        Type = PlayfabPersistenceData.AuthEntityToken.Entity.Type,
                    },

                    // Here we specify the creator's attributes.
                    Attributes = new MatchmakingPlayerAttributes
                    {
                        DataObject = new
                        {
                            Skill = 1,
                            Latencies = new []
                            {
                                new {region = "NorthEurope", latency = 0}
                            }
                        },
                    },
                },

                // Cancel matchmaking if a match is not found after 120 seconds.
                GiveUpAfterSeconds = 120,

                // The name of the queue to submit the ticket into.
                QueueName = "Casual",
            },

            // Callbacks for handling success and error.
            this.OnMatchmakingTicketCreated,
            this.OnMatchmakingError);
    }

    private void OnMatchmakingTicketCreated(CreateMatchmakingTicketResult result)
    {
       DisplayInfo("Joined Queue, Trying to find match", false); 
       UpdateTicketState(result.TicketId);
    }
    
    private void OnMatchmakingError(PlayFabError playFabError)
    {
       DisplayInfo(playFabError.GenerateErrorReport(), true); 
       Debug.Log(playFabError.GenerateErrorReport());
    }

    private void UpdateTicketState(string ticket)
    {
        PlayFabMultiplayerAPI.GetMatchmakingTicket(new GetMatchmakingTicketRequest
        {
            TicketId = ticket,
            QueueName = "Casual"
        }, result =>
        {
            DecideNextActionFromTicketState(ticket, result);
        }, error =>
        {
            DisplayInfo("Failed to get ticket state", true);  
            Debug.Log(error.GenerateErrorReport());
        }); 
    }

    private void DecideNextActionFromTicketState(string ticket, GetMatchmakingTicketResult result)
    {
        if (result.Status.Equals("Canceled"))
        {
            DisplayInfo("Ticket has been canceled.", true);
        } else if (result.Status.Equals("Matched"))
        {
            DisplayInfo("Joining match " + result.MatchId, false);
            TryToJoinMatch(result.MatchId);
        }
        else
        {
            DisplayInfo(result.Status, false);
            StartCoroutine(WaitToCheckTicketState(ticket));
        }
    }

    private IEnumerator WaitToCheckTicketState(string ticket)
    {
        yield return new WaitForSeconds(m_TicketStateCheckCooldown);
        UpdateTicketState(ticket);
    }

    private void TryToJoinMatch(string matchId)
    {
       PlayFabMultiplayerAPI.GetMatch(new GetMatchRequest
       {
           MatchId = matchId,
           QueueName = "Casual"
       }, result =>
       {
           PlayfabPersistenceData.ServerDetails = result.ServerDetails;
           DisplayInfo("Loading Lobby...", true);
           SceneManager.LoadScene("LobbyScene");
       }, error =>
       {
           DisplayInfo("Failed to get match", true);
           Debug.Log(error.GenerateErrorReport()); 
       }); 
    }

    
}
