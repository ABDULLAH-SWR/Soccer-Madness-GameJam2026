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
        seriesScoreText.text = $"{leftTeamName} {MatchData.redRoundWins} - {MatchData.blueRoundWins} {rightTeamName}";

        ApplyTeamVisuals();
        StartCoroutine(CountdownRoutine());
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
            yield return new WaitForSecondsRealtime(countdownDelay); // Uses your custom speed

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

        // Shake Camera
        if (CameraShake.instance) CameraShake.instance.Shake(0.2f, goalShakeAmt);

        if (isRedGoal)
        {
            // --- BLUE SCORES ---
            // Ball hit the "Red Goal" (Left Side), so Blue gets a point.
            scoreBlue++;
            blueScoreText.text = scoreBlue.ToString();

            // STRICT RULE: Blue got a point, so Blue loses a player.
            // We don't care who kicked it. Blue is winning, so Blue suffers.
            ApplyTheCurse(bluePlayers, "Blue");
        }
        else
        {
            // --- RED SCORES ---
            // Ball hit the "Blue Goal" (Right Side), so Red gets a point.
            scoreRed++;
            redScoreText.text = scoreRed.ToString();

            // STRICT RULE: Red got a point, so Red loses a player.
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

            // REDUCED SHAKE FOR CURSE
            if (CameraShake.instance) CameraShake.instance.Shake(0.3f, curseShakeAmt);
        }

        if (scoreRed >= 5) { EndRound("Red"); return; }
        if (scoreBlue >= 5) { EndRound("Blue"); return; }
        if (teamList.Count == 0) { EndRound((victimTeamName == "Red") ? "Blue" : "Red"); }
    }

    void EndRound(string roundWinner)
    {
        isGameActive = false;
        gameOverPanel.SetActive(true);

        if (roundWinner == "Red") MatchData.redRoundWins++;
        else MatchData.blueRoundWins++;

        string winningName = (roundWinner == "Red") ? leftTeamName : rightTeamName;
        Color finalColor;

        if (winningName == "BRAZIL") ColorUtility.TryParseHtmlString("#00DB37", out finalColor);
        else ColorUtility.TryParseHtmlString("#78C3E9", out finalColor);

        if (MatchData.redRoundWins >= 2)
        {
            winnerText.text = winningName + " WINS MATCH!";
            winnerText.color = finalColor;
            //buttonText.text = "MAIN MENU";
            if (AudioManager.instance) AudioManager.instance.PlaySFX(AudioManager.instance.winSound);
        }
        else if (MatchData.blueRoundWins >= 2)
        {
            winnerText.text = winningName + " WINS MATCH!";
            winnerText.color = finalColor;
            //buttonText.text = "MAIN MENU";
            if (AudioManager.instance) AudioManager.instance.PlaySFX(AudioManager.instance.winSound);
        }
        else
        {
            winnerText.text = winningName + " WINS ROUND " + MatchData.currentRound + "!";
            winnerText.color = finalColor;
            buttonText.text = "NEXT ROUND";
            MatchData.currentRound++;
        }
    }

    public void HandleGameOverButton() { if (MatchData.redRoundWins >= 2 || MatchData.blueRoundWins >= 2) { MatchData.ResetMatch(); GoToMainMenu(); } else { RestartGame(); } }
    public void RestartGame() { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
    public void GoToMainMenu() { Time.timeScale = 1f; SceneManager.LoadScene("MainMenu"); }
    public void QuitGame() { Application.Quit(); }
    void ResetBall() { if (ball == null) return; Rigidbody2D rb = ball.GetComponent<Rigidbody2D>(); rb.linearVelocity = Vector2.zero; rb.angularVelocity = 0f; TrailRenderer trail = ball.GetComponent<TrailRenderer>(); if (trail != null) trail.Clear(); ball.transform.position = centerPoint.position; }
}