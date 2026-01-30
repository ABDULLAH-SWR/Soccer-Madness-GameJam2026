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
    [Tooltip("How long to wait after a goal before resetting")]
    public float goalResetDelay = 2.0f;
    private bool isResetting = false; // Prevents double-goals
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

    [Header("Team Colors")]
    public Color brazilColor;      // We will set this to FCE31F
    public Color argentinaColor;   // We will set this to 00FEE5

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
        Color playerColor;
        Color enemyColor;

        // Logic: Decide who gets which Sprite AND which Color
        if (MatchData.playerChoseBrazil)
        {
            // Player 1 (Red/Left) is Brazil
            playerSprite = brazilSprite;
            playerColor = brazilColor;

            // Player 2 (Blue/Right) is Argentina
            enemySprite = argentinaSprite;
            enemyColor = argentinaColor;
        }
        else
        {
            // Player 1 (Red/Left) is Argentina
            playerSprite = argentinaSprite;
            playerColor = argentinaColor;

            // Player 2 (Blue/Right) is Brazil
            enemySprite = brazilSprite;
            enemyColor = brazilColor;
        }

        // Apply to Left Team (Red List)
        foreach (GameObject p in redPlayers)
        {
            if (p != null)
            {
                p.GetComponent<SpriteRenderer>().sprite = playerSprite;
                SetRingColor(p, playerColor);
            }
        }

        // Apply to Right Team (Blue List)
        foreach (GameObject p in bluePlayers)
        {
            if (p != null)
            {
                p.GetComponent<SpriteRenderer>().sprite = enemySprite;
                SetRingColor(p, enemyColor);
            }
        }
    }

    // Helper function to find the Ring and color it
    void SetRingColor(GameObject player, Color c)
    {
        // Option 1: If the Ring is a child named "Ring"
        Transform ringTrans = player.transform.Find("Ring");
        if (ringTrans != null)
        {
            ringTrans.GetComponent<SpriteRenderer>().color = c;
        }
        else
        {
            // Option 2: If the Ring is just the second SpriteRenderer on the object
            // (Use this if Option 1 doesn't work)
            SpriteRenderer[] sprites = player.GetComponentsInChildren<SpriteRenderer>();
            foreach (var s in sprites)
            {
                // We assume the main body is the one with the player sprite, 
                // so the OTHER one must be the ring.
                if (s.gameObject != player)
                {
                    s.color = c;
                }
            }
        }
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
        // 1. Safety Check: If game is over OR we are already waiting for reset, stop.
        if (!isGameActive || isResetting) return;

        isResetting = true; // Block other goals immediately

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

        // 2. Instead of resetting instantly, start the delay routine
        if (isGameActive)
        {
            StartCoroutine(GoalResetSequence());
        }
    }


    IEnumerator GoalResetSequence()
    {
        // 1. Get Components
        SpriteRenderer ballSr = ball.GetComponent<SpriteRenderer>();
        if (ballSr == null) ballSr = ball.GetComponentInChildren<SpriteRenderer>();

        Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
        TrailRenderer trail = ball.GetComponent<TrailRenderer>();

        // 2. Stop Physics
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // 3. NUCLEAR FIX: Disable the trail component entirely
        if (trail != null) trail.enabled = false;

        // 4. Hide Ball Visuals
        if (ballSr != null) ballSr.enabled = false;

        // 5. Wait for explosion/delay
        yield return new WaitForSeconds(goalResetDelay);

        // 6. Reset & Show
        ResetBall(); // This will re-enable the trail safely

        if (ballSr != null) ballSr.enabled = true;
        isResetting = false;
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
    public void GoToMainMenu()
    {
        // FIX: Reset the score when leaving the game, so the next game starts at 0-0
        MatchData.ResetMatch();

        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
    public void QuitGame() { Application.Quit(); }
    void ResetBall()
    {
        if (ball == null) return;

        Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // 1. MOVE the ball first (while trail is still off/cleared)
        ball.transform.position = Vector3.zero;

        // 2. Handle the Trail
        TrailRenderer trail = ball.GetComponent<TrailRenderer>();
        if (trail != null)
        {
            trail.Clear();          // Erase any old history
            trail.enabled = true;   // Turn it back on for the new round
        }
    }
}