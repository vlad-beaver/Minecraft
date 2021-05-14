using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScoreElement : MonoBehaviour
{

    public TMP_Text usernameText;
    public TMP_Text graphicsText;
    public TMP_Text gameplayText;
    public TMP_Text xpText;

    public void NewScoreElement (string _username, int _graphics, int _gameplay, int _xp)
    {
        usernameText.text = _username;
        graphicsText.text = _graphics.ToString();
        gameplayText.text = _gameplay.ToString();
        xpText.text = _xp.ToString();
    }

}
