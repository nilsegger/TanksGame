using PlayFab.ClientModels;
using PlayFab.MultiplayerModels;

public static class PlayfabPersistenceData
{
    public static bool IsUsingPlayFab { get; set; } = false;
    public static EntityTokenResponse AuthEntityToken { get; set; }
    public static ServerDetails ServerDetails { get; set; }
}
