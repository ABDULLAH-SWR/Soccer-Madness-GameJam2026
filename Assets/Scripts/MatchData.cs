public static class MatchData
{
    public static int redRoundWins = 0;
    public static int blueRoundWins = 0;
    public static int currentRound = 1;

    // --- THIS IS THE MISSING LINE ---
    public static bool playerChoseBrazil = true;
    // -------------------------------

    public static void ResetMatch()
    {
        redRoundWins = 0;
        blueRoundWins = 0;
        currentRound = 1;

        // Note: We do NOT reset 'playerChoseBrazil' here.
        // We want to remember the team choice even if the match restarts!
    }
}