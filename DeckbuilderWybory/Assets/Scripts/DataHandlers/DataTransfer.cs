
public static class DataTransfer
{
    public static string LobbyName { get; set; }
    public static string LobbyId { get; set; }
    public static int LobbySize { get; set; }
    public static int IsStarted { get; set; }
    public static string PlayerId { get; set; }
    public static string PlayerName { get; set; }
    public static bool IsFirstCardInTurn { get; set; }
    public static bool IsPlayerTurn {  get; set; }

    public static bool TurnEnded { get; set; }

    public static bool EffectActive { get; set; }
}
