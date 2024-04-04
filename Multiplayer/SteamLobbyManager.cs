using Steamworks;
using UnityEngine;

public delegate void LobbyJoinCreateCallback(bool success, string message);
public delegate void LobbyPlayerPresenceChangedEvent();
public delegate void LobbyAbandonedEvent();

public class SteamLobbyManager
{
    private enum LobbyRequestType
    {
        None = 0,
        CreateLobby = 1,
        JoinLobby = 2
    }

    private class LobbyRequest
    {
        public LobbyRequest(string lobbyName, string lobbyPassword, LobbyRequestType requestType, LobbyJoinCreateCallback joinCreateCallback)
        {
            LobbyName = lobbyName;
            LobbyPassword = lobbyPassword;
            RequestType = requestType;
            JoinCreateCallback = joinCreateCallback;
        }

        public string LobbyName { get; set; }
        public string LobbyPassword { get; set; }
        public LobbyRequestType RequestType { get; set; }
        public LobbyJoinCreateCallback JoinCreateCallback { get; set; }
    }

    private static LobbyRequest lobbyRequest = null;
    private static CSteamID currentLobbyId;

    public static CSteamID CurrentLobbyId { get { return currentLobbyId; } }
    public static event LobbyPlayerPresenceChangedEvent OnLobbyPlayerPresenceChanged;
    public static event LobbyAbandonedEvent OnLobbyAbandoned;


    public static void Initialize()
    {
        Callback<LobbyMatchList_t>.Create(RequestLobbyListCallback);
        Callback<LobbyCreated_t>.Create(CreateLobbyCallback);
        Callback<LobbyEnter_t>.Create(JoinLobbyCallback);
        Callback<LobbyDataUpdate_t>.Create(LobbyDataUpdateCallback);
        Callback<LobbyChatUpdate_t>.Create(LobbyChatUpdateCallback);
    }

    public static void CreateLobby(string lobbyName, string lobbyPassword, LobbyJoinCreateCallback callback)
    {
        lobbyRequest = new(lobbyName, lobbyPassword, LobbyRequestType.CreateLobby, callback);

        SteamMatchmaking.AddRequestLobbyListResultCountFilter(1);
        SteamMatchmaking.AddRequestLobbyListStringFilter("LobbyName", lobbyName, ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.RequestLobbyList();
    }

    public static void JoinLobby(string lobbyName, string lobbyPassword, LobbyJoinCreateCallback callback)
    {
        lobbyRequest = new(lobbyName, lobbyPassword, LobbyRequestType.JoinLobby, callback);

        SteamMatchmaking.AddRequestLobbyListResultCountFilter(1);
        SteamMatchmaking.AddRequestLobbyListStringFilter("LobbyName", lobbyName, ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.AddRequestLobbyListStringFilter("LobbyPass", lobbyPassword, ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.RequestLobbyList();
    }

    public static void LeaveLobby()
    {
        if (!currentLobbyId.IsValid())
            return;

        GlobalNetworkManager.FishySteamworks.StopConnection(false);
        bool isLobbyOwner = SteamUser.GetSteamID() == SteamMatchmaking.GetLobbyOwner(currentLobbyId);
        if (isLobbyOwner)
            GlobalNetworkManager.FishySteamworks.StopConnection(true);

        if (isLobbyOwner)
            SteamMatchmaking.SetLobbyData(currentLobbyId, "Abandoned", "1");

        SteamMatchmaking.LeaveLobby(currentLobbyId);

        currentLobbyId.Clear();
    }

    private static void RequestLobbyListCallback(LobbyMatchList_t callback)
    {
        // Ignore unwanted callbacks
        if (lobbyRequest == null)
            return;

        switch (lobbyRequest.RequestType)
        {
            case LobbyRequestType.CreateLobby:
                if (callback.m_nLobbiesMatching > 0)
                {
                    lobbyRequest?.JoinCreateCallback(false, $"A lobby with the name '{lobbyRequest.LobbyName}' already exists. Please use a different lobby name.");
                    lobbyRequest = null;
                    return;
                }

                SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, 5);

                break;
            case LobbyRequestType.JoinLobby:
                if (callback.m_nLobbiesMatching == 0)
                {
                    lobbyRequest?.JoinCreateCallback(false, "No lobby with matching name and password found");
                    lobbyRequest = null;
                    return;
                }

                SteamMatchmaking.JoinLobby(SteamMatchmaking.GetLobbyByIndex(0));
                break;
            default:
                break;
        }
    }

    private static void CreateLobbyCallback(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            lobbyRequest?.JoinCreateCallback(false, $"Lobby creation failed. Result: {EResult.k_EResultOK}");
            lobbyRequest = null;
            return;
        }

        currentLobbyId = new(callback.m_ulSteamIDLobby);
        SteamMatchmaking.SetLobbyData(currentLobbyId, "LobbyName", lobbyRequest.LobbyName);
        SteamMatchmaking.SetLobbyData(currentLobbyId, "LobbyPass", lobbyRequest.LobbyPassword);
        GlobalNetworkManager.FishySteamworks.SetClientAddress(currentLobbyId.ToString());
        GlobalNetworkManager.FishySteamworks.StartConnection(true);

        lobbyRequest?.JoinCreateCallback(true, $"Successfully created lobby and started server");
        lobbyRequest = null;
    }

    private static void JoinLobbyCallback(LobbyEnter_t callback)
    {
        if (callback.m_bLocked || (EChatRoomEnterResponse)callback.m_EChatRoomEnterResponse != EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
        {
            lobbyRequest?.JoinCreateCallback(false, "Failed to join lobby.");
            lobbyRequest = null;
            return;
        }

        currentLobbyId = new(callback.m_ulSteamIDLobby);
        GlobalNetworkManager.FishySteamworks.SetClientAddress(currentLobbyId.ToString());
        GlobalNetworkManager.FishySteamworks.StartConnection(false);

        //SteamMatchmaking.SendLobbyChatMsg(CurrentLobbyId, )

        lobbyRequest?.JoinCreateCallback(true, "Successfully joined lobby");
        lobbyRequest = null;
    }

    private static void LobbyDataUpdateCallback(LobbyDataUpdate_t callback)
    {
        if (!currentLobbyId.IsValid())
            return;

        if (SteamMatchmaking.GetLobbyData(currentLobbyId, "Abandoned") == "1")
            OnLobbyAbandoned?.Invoke();
    }

    private static void LobbyChatUpdateCallback(LobbyChatUpdate_t callback)
    {
        OnLobbyPlayerPresenceChanged?.Invoke();
    }
}
