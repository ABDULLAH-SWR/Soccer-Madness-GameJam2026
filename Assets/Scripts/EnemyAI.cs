using UnityEngine;
using System.Collections.Generic;

public class EnemyAI : MonoBehaviour
{
    [Header("Settings")]
    public bool isTeamRed = false; // Usually False (Blue Team)
    public float reactionSpeed = 5.0f; // Lower = Lazier AI
    public float kickDistance = 1.5f; // How close to get before shooting

    [Header("References")]
    public Transform ball;
    public Transform myGoal;   // To defend
    public Transform enemyGoal; // To attack

    private List<PlayerController> myPlayers = new List<PlayerController>();
    private TeamController manualController; // Reference to the keyboard controller

    void Start()
    {
        // 1. Check Game Mode
        // If we are in PvP mode, DISABLE this AI immediately.
        if (GameSettings.isPvE == false)
        {
            this.enabled = false;
            return;
        }

        // 2. If PvE, Disable the manual keyboard controller for this team
        manualController = GetComponent<TeamController>();
        if (manualController != null)
        {
            manualController.enabled = false;
        }

        // 3. Find Ball
        if (ball == null)
        {
            GameObject b = GameObject.FindGameObjectWithTag("Ball");
            if (b != null) ball = b.transform;
        }

        // 4. Find Goals (Assuming Standard Names)
        if (isTeamRed)
        {
            GameObject g1 = GameObject.Find("Goal_Left"); // Defend
            GameObject g2 = GameObject.Find("Goal_Right"); // Attack
            if (g1) myGoal = g1.transform;
            if (g2) enemyGoal = g2.transform;
        }
        else // Blue Team (Standard AI)
        {
            GameObject g1 = GameObject.Find("Goal_Right"); // Defend
            GameObject g2 = GameObject.Find("Goal_Left"); // Attack
            if (g1) myGoal = g1.transform;
            if (g2) enemyGoal = g2.transform;
        }

        // 5. Find Players
        RefreshPlayerList();
    }

    void RefreshPlayerList()
    {
        myPlayers.Clear();
        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject p in allPlayers)
        {
            PlayerController pc = p.GetComponent<PlayerController>();
            // Strict Team Check
            if (pc != null && pc.isTeamRed == this.isTeamRed)
            {
                myPlayers.Add(pc);
            }
        }
    }

    void Update()
    {
        if (ball == null) return;

        // Safety Cleanup (Same as TeamController)
        for (int i = myPlayers.Count - 1; i >= 0; i--)
        {
            if (myPlayers[i] == null) myPlayers.RemoveAt(i);
        }
        if (myPlayers.Count == 0) return;

        // --- AI LOGIC ---

        // 1. Find the Closest Player to the Ball
        PlayerController closestPlayer = GetClosestPlayerToBall();

        // 2. Command the Squad
        foreach (PlayerController p in myPlayers)
        {
            if (p == closestPlayer)
            {
                // This is the "Chosen One" - He chases the ball
                ControlActivePlayer(p);
                p.isSelected = true; // Show Ring
            }
            else
            {
                // Everyone else stops (or you could add formation logic here)
                p.ReceiveInput(Vector2.zero, false);
                p.isSelected = false;
            }
        }
    }

    PlayerController GetClosestPlayerToBall()
    {
        PlayerController best = null;
        float minDst = Mathf.Infinity;

        foreach (PlayerController p in myPlayers)
        {
            if (p == null) continue;
            float dst = Vector2.Distance(p.transform.position, ball.position);
            if (dst < minDst)
            {
                minDst = dst;
                best = p;
            }
        }
        return best;
    }

    void ControlActivePlayer(PlayerController p)
    {
        Vector2 direction = Vector2.zero;
        bool shouldKick = false;

        float distToBall = Vector2.Distance(p.transform.position, ball.position);

        // A. MOVEMENT LOGIC
        if (distToBall > 0.5f)
        {
            // Move towards the ball
            direction = (ball.position - p.transform.position).normalized;
        }

        // B. KICK LOGIC
        // If we are close to the ball...
        if (distToBall < kickDistance)
        {
            // ...and the ball is in front of us...
            // (Simple check: Just try to kick if close)
            shouldKick = true;
        }

        // Send commands to the PlayerController
        p.ReceiveInput(direction, shouldKick);
    }
}