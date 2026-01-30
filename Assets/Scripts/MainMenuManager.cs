using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject teamSelectPanel; // Drag your new Panel here
    public GameObject tutorialPanel;

    // Internal memory to remember if we clicked PvE or PvP
    private bool pendingPvEMode = true;

    void Start()
    {
        if (MatchData.currentRound > 1 || MatchData.redRoundWins > 0 || MatchData.blueRoundWins > 0)
        {
            MatchData.ResetMatch();
        }
        // Check if the player has seen the tutorial before.
        // 0 = No (First Time), 1 = Yes (Returning Player)
        if (PlayerPrefs.GetInt("HasSeenTutorial", 0) == 0)
        {
            OpenTutorial();

            // Now mark it as "Seen" so it doesn't happen again
            PlayerPrefs.SetInt("HasSeenTutorial", 1);
            PlayerPrefs.Save();
        }
    }
    public void OpenTutorial()
    {
        tutorialPanel.SetActive(true);
    }

    public void CloseTutorial()
    {
        tutorialPanel.SetActive(false);
    }
    public void OpenTeamSelection(bool isPvE)
    {
        // 1. Remember which mode the user wants to play
        pendingPvEMode = isPvE;

        // 2. Show the Team Select Screen
        teamSelectPanel.SetActive(true);
    }

    public void SelectTeamAndStart(bool chooseBrazil)
    {
        // 1. Save the Team Choice
        MatchData.playerChoseBrazil = chooseBrazil;

        // 2. Save the Mode Choice
        GameSettings.isPvE = pendingPvEMode;

        // 3. Play Sound (Optional)
        if (AudioManager.instance != null) AudioManager.instance.PlayClickSound();

        // 4. Start the Game!
        SceneManager.LoadScene("SampleScene");
    }

    public void CloseTeamSelection()
    {
        teamSelectPanel.SetActive(false);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}