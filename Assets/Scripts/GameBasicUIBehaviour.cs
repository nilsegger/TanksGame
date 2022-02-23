using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameBasicUIBehaviour : NetworkBehaviour 
{

    public Text m_CountdownText;

    private float _countdown = 0.0f;

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
    }
}
