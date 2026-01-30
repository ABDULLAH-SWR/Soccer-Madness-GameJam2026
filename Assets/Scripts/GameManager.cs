using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public GameObject deathPrefab;

    [Header("Game Feel (Tweak These!)")]
    [Tooltip("How long to wait between 3, 2, 1")]
    public float countdownDelay = 1.0f;
    [Tooltip("Shake Power: Goal Scored")]
    public float goalShakeAmt = 0.1f;
    [Tooltip("Shake Power: Player Dies")]
    public float curseShakeAmt = 0.2f;

    [Header("UI References")]
    public TextMeshProUGUI redScoreText;
    public TextMeshProUGUI blueScoreText;
    public TextMeshProUGUI roundInfoText;
    public TextMeshProUGUI seriesScoreText;
    public TextMeshProUGUI countdownText;

    [Header("End Game UI")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI winnerText;
    public TextMeshProUGUI buttonText;

    [Header("Game Settings")]
    public GameObject ball;
    public Transform centerPoint;

    [Header("Teams")]
    public int scoreRed = 0;
    public List<GameObject> redPlayers;
    public int scoreBlue = 0;
    public List<GameObject> bluePlayers;

    [Header("Team Flags")]
    public Sprite brazilSprite;
    public Sprite argentinaSprite;

    [Header("Pause UI")]
    public GameObject pausePanel;
    private bool isPaused = false;
    private bool isGameActive = true;

    private string leftTeamName;
    private string rightTeamName;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        Time.timeScale = 1f;

        if (MatchData.playerChoseBrazil)
        {
            leftTeamName = "BRAZIL";
            rightTeamName = "ARGENTINA";
        }
        else
        {
            leftTeamName = "ARGENTINA";
            rightTeamName = "BRAZIL";
        }

        roundInfoText.text = "ROUND " + MatchData.currentRound + "/3";
        // Display current series score at start of round
        UpdateSeriesUI();

        ApplyTeamVisuals();
        StartCoroutine(CountdownRoutine());
    }

    void UpdateSeriesUI()
    {
        seriesScoreText.text = $"{leftTeamName} {MatchData.redRoundWins} - {MatchData.blueRoundWins} {rightTeamName}";
    }

    void ApplyTeamVisuals()
    {
        Sprite playerSprite;
        Sprite enemySprite;

        if (MatchData.playerChoseBrazil)
        {
            playerSprite = brazilSprite;
            enemySprite = argentinaSprite;
        }
        else
        {
            playerSprite = argentinaSprite;
            enemySprite = brazilSprite;
        }

        foreach (GameObject p in redPlayers) if (p != null) p.GetComponent<SpriteRenderer>().sprite = playerSprite;
        foreach (GameObject p in bluePlayers) if (p != null) p.GetComponent<SpriteRenderer>().sprite = enemySprite;
    }

    void Update()
    {
        if (isGameActive && Input.GetKeyDown(KeyCode.Escape)) TogglePause();
    }

    IEnumerator CountdownRoutine()
    {
        Time.timeScale = 0f;
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);

            countdownText.text = "3";
            if (AudioManager.instance) AudioManager.instance.PlaySFX(AudioManager.instance.clickSound);
            yield return new WaitForSecondsRealtime(countdownDelay);

            countdownText.text = "2";
            if (AudioManager.instance) AudioManager.instance.PlaySFX(AudioManager.instance.clickSound);
            yield return new WaitForSecondsRealtime(countdownDelay);

            countdownText.text = "1";
            if (AudioManager.instance) AudioManager.instance.PlaySFX(AudioManager.instance.clickSound);
            yield return new WaitForSecondsRealtime(countdownDelay);

            countdownText.text = "GO!";
            if (AudioManager.instance) AudioManager.instance.PlaySFX(AudioManager.instance.goalSound);
        }
        Time.timeScale = 1f;
        yield return new WaitForSeconds(0.5f);
        if (countdownText != null) countdownText.gameObject.SetActive(false);
    }

    public void TogglePause()
    {
        if (countdownText != null && countdownText.gameObject.activeSelf) return;
        isPaused = !isPaused;
        if (isPaused) { Time.timeScale = 0f; pausePanel.SetActive(true); if (AudioManager.instance) AudioManager.instance.musicSource.volume = 0.2f; }
        else ResumeGame();
    }

    public void ResumeGame()
    {
        isPaused = false;
        pausePanel.SetActive(false);
        if (AudioManager.instance) AudioManager.instance.musicSource.volume = 0.5f;
        StartCoroutine(CountdownRoutine());
    }

    public void GoalScored(bool isRedGoal)
    {
        if (!isGameActive) return;
        if (AudioManager.instance) AudioManager.instance.PlaySFX(AudioManager.instance.goalSound);

        if (CameraShake.instance) CameraShake.instance.Shake(0.2f, goalShakeAmt);

        if (isRedGoal)
        {
            scoreBlue++;
            blueScoreText.text = scoreBlue.ToString();
            ApplyTheCurse(bluePlayers, "Blue");
        }
        else
        {
            scoreRed++;
            redScoreText.text = scoreRed.ToString();
            ApplyTheCurse(redPlayers, "Red");
        }

        if (isGameActive) ResetBall();
    }

    void ApplyTheCurse(List<GameObject> teamList, string victimTeamName)
    {
        if (teamList.Count > 0)
        {
            int index = Random.Range(0, teamList.Count);
            GameObject victim = teamList[index];
            teamList.RemoveAt(index);
            Instantiate(deathPrefab, victim.transform.position, Quaternion.identity);
            Destroy(victim);
            if (AudioManager.instance) AudioManager.instance.PlaySFX(AudioManager.instance.curseSound);
            if (CameraShake.instance) CameraShake.instance.Shake(0.3f, curseShakeAmt);
        }

        if (scoreRed >= 5) { EndRound("Red"); return; }
        if (scoreBlue >= 5) { EndRound("Blue"); return; }
        if (teamList.Count == 0) { EndRound((victimTeamName == "Red") ? "Blue" : "Red"); }
    }

    void EndRound(string roundWinner)
    {
        isGameActive = false;

        // --- STEP 1: UPDATE LOGIC & SCORES BEFORE SHOWING PANEL ---
        if (roundWinner == "Red") MatchData.redRoundWins++;
        else MatchData.blueRoundWins++;

        // Update the visual text immediately so the player sees the new score
        UpdateSeriesUI();

        string winningName = (roundWinner == "Red") ? leftTeamName : rightTeamName;
        Color finalColor;

        if (winningName == "BRAZIL") ColorUtility.TryParseHtmlString("#00DB37", out finalColor);
        else ColorUtility.TryParseHtmlString("#78C3E9", out finalColor);

        // --- STEP 2: CHECK IF MATCH IS OVER ---
        if (MatchData.redRoundWins >= 2 || MatchData.blueRoundWins >= 2)
        {
            // Match Finished (Someone won 2 rounds)
            winnerText.text = winningName + " WINS MATCH!";
            winnerText.color = finalColor;
            buttonText.text = "PLAY AGAIN"; // This button now Resets the whole game
            if (AudioManager.instance) AudioManager.instance.PlaySFX(AudioManager.instance.winSound);
        }
        else
        {
            // Just a Round Finished
            winnerText.text = winningName + " WINS ROUND " + MatchData.currentRound + "!";
            winnerText.color = finalColor;
            buttonText.text = "NEXT ROUND";
            MatchData.currentRound++; // Prepare counter for next round
        }

        // --- STEP 3: FINALLY SHOW THE PANEL ---
        gameOverPanel.SetActive(true);
    }

    public void HandleGameOverButton()
    {
        // Check if the MATCH is truly over (One side reached 2 wins)
        if (MatchData.redRoundWins >= 2 || MatchData.blueRoundWins >= 2)
        {
            // --- THE FIX: FORCE RESET EVERYTHING MANUALLY ---
            MatchData.redRoundWins = 0;
            MatchData.blueRoundWins = 0;
            MatchData.currentRound = 1;

            // Optional: Reset player choices if you want to force character select again
            // MatchData.playerChoseBrazil = true; 

            // Reload the scene to start fresh
            RestartGame();
        }
        else
        {
            // The match is NOT over (Score is 1-0 or 0-1, etc.)
            // Just reload the scene to play the next round
            RestartGame();
        }
    }

    public void RestartGame() { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
    public void GoToMainMenu() { Time.timeScale = 1f; SceneManager.LoadScene("MainMenu"); }
    public void QuitGame() { Application.Quit(); }
    void ResetBall() { if (ball == null) return; Rigidbody2D rb = ball.GetComponent<Rigidbody2D>(); rb.linearVelocity = Vector2.zero; rb.angularVelocity = 0f; TrailRenderer trail = ball.GetComponent<TrailRenderer>(); if (trail != null) trail.Clear(); ball.transform.position = centerPoint.position; }
}