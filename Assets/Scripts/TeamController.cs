using UnityEngine;
using System.Collections.Generic;

public class TeamController : MonoBehaviour
{
    [Header("Settings")]
    public bool isTeamRed = true;
    public float switchBias = 2.0f;

    [Header("Controls")]
    public KeyCode moveUp = KeyCode.W;
    public KeyCode moveDown = KeyCode.S;
    public KeyCode moveLeft = KeyCode.A;
    public KeyCode moveRight = KeyCode.D;
    public KeyCode kickKey = KeyCode.Space;

    [Header("References")]
    public Transform ball;

    private List<PlayerController> myPlayers = new List<PlayerController>();
    private PlayerController activePlayer;

    void Start()
    {
        if (ball == null)
        {
            GameObject b = GameObject.FindGameObjectWithTag("Ball");
            if (b != null) ball = b.transform;
        }

        // Find initial players
        RefreshPlayerList();
    }

    void RefreshPlayerList()
    {
        myPlayers.Clear();
        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject p in allPlayers)
        {
            PlayerController pc = p.GetComponent<PlayerController>();
            if (pc != null && pc.isTeamRed == this.isTeamRed)
            {
                myPlayers.Add(pc);
            }
        }

        if (myPlayers.Count > 0 && activePlayer == null)
            activePlayer = myPlayers[0];
    }

    void Update()
    {
        if (ball == null) return;

        // 1. CLEANUP: Remove dead players before doing anything!
        // We loop backwards to safely remove items
        for (int i = myPlayers.Count - 1; i >= 0; i--)
        {
            if (myPlayers[i] == null)
            {
                myPlayers.RemoveAt(i);
            }
        }

        // 2. SAFETY CHECK: If everyone is dead, stop.
        if (myPlayers.Count == 0) return;

        // 3. FAILSAFE: If active player died, pick a new random survivor immediately
        if (activePlayer == null)
        {
            activePlayer = myPlayers[0];
        }

        // 4. LOGIC: Decide who is best
        UpdateActivePlayer();

        // 5. INPUT: Gather keys
        Vector2 input = Vector2.zero;
        if (Input.GetKey(moveUp)) input.y = +1;
        if (Input.GetKey(moveDown)) input.y = -1;
        if (Input.GetKey(moveLeft)) input.x = -1;
        if (Input.GetKey(moveRight)) input.x = +1;
        bool isKicking = Input.GetKey(kickKey);

        // 6. STRICT CONTROL LOOP
        foreach (PlayerController p in myPlayers)
        {
            if (p == null) continue; // Double safety check

            if (p == activePlayer)
            {
                p.ReceiveInput(input, isKicking);
                p.isSelected = true;
            }
            else
            {
                p.ReceiveInput(Vector2.zero, false);
                p.isSelected = false;
            }
        }
    }

    void UpdateActivePlayer()
    {
        if (activePlayer == null) return;

        PlayerController bestCandidate = activePlayer;
        float distToCurrent = Vector2.Distance(activePlayer.transform.position, ball.position);

        foreach (PlayerController p in myPlayers)
        {
            if (p == activePlayer || p == null) continue;

            float distToCandidate = Vector2.Distance(p.transform.position, ball.position);

            if (distToCandidate < distToCurrent - switchBias)
            {
                bestCandidate = p;
                distToCurrent = distToCandidate;
            }
        }

        activePlayer = bestCandidate;
    }
}