// Copyright (c) Facebook, Inc. and its affiliates.

using Unity.Netcode;
using Oculus.Platform;
using System.Collections;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[InitializeOnLoad]
public static class SharedSpacesTelemetry
{
    static SharedSpacesTelemetry()
    {
        Collect();
    }

    [MenuItem("Oculus/Telemetry Settings")]
    static void TelemetrySettings()
    {
        Collect(true);
    }

    static void Collect(bool force = false)
    {
        const string enabledKey = "OculusTelemetryEnabled";
        const string privacyPolicyUrl = "https://www.oculus.com/legal/privacy-policy/";

        if (force || EditorPrefs.HasKey(enabledKey) == false)
        {
            var response = EditorUtility.DisplayDialogComplex(
                "Enable Oculus Telemetry",
                $"Enabling telemetry will transmit data to Oculus about your usage of its samples and tools. This information is used by Oculus to improve our products and better serve our developers. For more information, go to this url: {privacyPolicyUrl}",
                "Enable",
                "Opt out",
                "Open Privacy Policy");

            EditorPrefs.SetBool(enabledKey, response == 0);

            if (response == 2)
            {
                EditorPrefs.DeleteKey(enabledKey);
                EditorUtility.OpenWithDefaultApp(privacyPolicyUrl);
            }
        }

        if (EditorPrefs.GetBool(enabledKey) && SessionState.GetBool("OculusTelemetry-module_loaded-SharedSpaces", false) == false)
        {
            OVRPlugin.SetDeveloperMode(OVRPlugin.Bool.True);
            OVRPlugin.SendEvent("module_loaded", "Unity-SharedSpaces", "integration");
            SessionState.SetBool("OculusTelemetry-module_loaded-SharedSpaces", true);
        }
    }
}
#endif

public class SharedSpacesApplication : MonoBehaviour
{

    public SharedSpacesNetworkLayer networkLayer;
    public SharedSpacesSceneLoader sceneLoader;
    public SharedSpacesSpawner spawner;
    public SharedSpacesVoip voip;
    public SharedSpacesLocalPlayerState localPlayerState;
    public SharedSpacesGroupPresenceState groupPresenceState { get; private set; }

    private SharedSpacesSession session;
    private LaunchType launchType;

    private void OnEnable()
    {
        DontDestroyOnLoad(this);

        networkLayer.OnClientConnectedCallback += OnClientConnected;
        networkLayer.OnClientDisconnectedCallback += OnClientDisconnected;
        networkLayer.OnMasterClientSwitchedCallback = OnMasterClientSwitched;
        networkLayer.StartHostCallback += OnHostStarted;
        networkLayer.StartClientCallback += OnClientStarted;
        networkLayer.RestoreHostCallback += OnHostRestored;
        networkLayer.RestoreClientCallback += OnClientRestored;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsHost)
        {
            session.DetermineFallbackHost(clientId);
            session.SetPhotonVoiceRoom(clientId);
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            session = FindObjectOfType<SharedSpacesSession>();

            if (networkLayer.clientState == SharedSpacesNetworkLayer.ClientState.RestoringClient)
            {
                session.RequestSpawnServerRpc(
                    clientId,
                    localPlayerState.transform.position,
                    localPlayerState.transform.rotation
                );
            }
            else
            {
                session.RequestSpawnServerRpc(
                    clientId,
                    SharedSpacesSpawnPoint.singleton.SpawnPosition,
                    SharedSpacesSpawnPoint.singleton.SpawnRotation
                );
            }
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        session.RedetermineFallbackHost(clientId);
    }

    private ulong OnMasterClientSwitched()
    {
        return SharedSpacesSession.fallbackHostId;
    }

    private void OnHostStarted()
    {
        session = spawner.SpawnSession().GetComponent<SharedSpacesSession>();

        NetworkObject player = spawner.SpawnPlayer(
            NetworkManager.Singleton.LocalClientId,
            SharedSpacesSpawnPoint.singleton.SpawnPosition,
            SharedSpacesSpawnPoint.singleton.SpawnRotation
        );

        voip.StartVoip(player.transform);
    }

    private void OnClientStarted()
    {
        NetworkObject player = NetworkManager.Singleton.ConnectedClients[NetworkManager.Singleton.LocalClientId].PlayerObject;
        voip.StartVoip(player.transform);
    }

    private void OnHostRestored()
    {
        session = spawner.SpawnSession().GetComponent<SharedSpacesSession>();

        NetworkObject player = spawner.SpawnPlayer(
            NetworkManager.Singleton.LocalClientId,
            localPlayerState.transform.position,
            localPlayerState.transform.rotation
        );

        voip.StartVoip(player.transform);
    }

    private void OnClientRestored()
    {
        NetworkObject player = NetworkManager.Singleton.ConnectedClients[NetworkManager.Singleton.LocalClientId].PlayerObject;
        voip.StartVoip(player.transform);
    }

    private void Start()
    {
        StartCoroutine(Init());
    }

    private IEnumerator Init()
    {
        Core.AsyncInitialize().OnComplete(OnOculusPlatformInitialized);

#if !UNITY_EDITOR && !UNITY_STANDALONE_WIN

        yield return new WaitUntil(() => localPlayerState.username != "");
#else
        launchType = LaunchType.Normal;
#endif

        // start in the lobby
        if (launchType == LaunchType.Normal)
        {
            groupPresenceState = new SharedSpacesGroupPresenceState();

            StartCoroutine(groupPresenceState.Set(
                    "Lobby",
                    "Lobby-" + localPlayerState.applicationID,
                    "",
                    true
                )
            );
        }

        yield return new WaitUntil(() => groupPresenceState != null && groupPresenceState.destination != null);

        sceneLoader.LoadScene(groupPresenceState.destination);
        yield return new WaitUntil(() => sceneLoader.sceneLoaded);

        networkLayer.Init(GetPhotonRoomName());
    }

    private void OnOculusPlatformInitialized(Message<Oculus.Platform.Models.PlatformInitialize> message)
    {
        if (message.IsError)
        {
            LogError("Failed to initialize Oculus Platform SDK", message.GetError());
            return;
        }

        Debug.Log("Oculus Platform SDK initialized successfully");

        Entitlements.IsUserEntitledToApplication().OnComplete(msg =>
        {
            if (msg.IsError)
            {
                LogError("You are not entitled to use this app", msg.GetError());
                return;
            }

            launchType = ApplicationLifecycle.GetLaunchDetails().LaunchType;

            GroupPresence.SetJoinIntentReceivedNotificationCallback(OnJoinIntentReceived);
            GroupPresence.SetInvitationsSentNotificationCallback(OnInvitationsSent);

            Users.GetLoggedInUser().OnComplete(OnLoggedInUser);
        });
    }

    private void OnLoggedInUser(Message<Oculus.Platform.Models.User> message)
    {
        if (message.IsError)
        {
            LogError("Cannot get user info", message.GetError());
            return;
        }

        // Workaround.
        // At the moment, Platform.Users.GetLoggedInUser() seems to only be returning the user ID.
        // Display name is blank.
        // Platform.Users.Get(ulong userID) returns the display name.
        Users.Get(message.Data.ID).OnComplete(localPlayerState.Init);
    }

    private void OnJoinIntentReceived(Message<Oculus.Platform.Models.GroupPresenceJoinIntent> message)
    {
        Debug.Log("------JOIN INTENT RECEIVED------");
        Debug.Log("Destination:       " + message.Data.DestinationApiName);
        Debug.Log("Lobby Session ID:  " + message.Data.LobbySessionId);
        Debug.Log("Match Session ID:  " + message.Data.MatchSessionId);
        Debug.Log("Deep Link Message: " + message.Data.DeeplinkMessage);
        Debug.Log("--------------------------------");

        string messageLobbySessionId = message.Data.LobbySessionId;

        // If true, this means lobby session ID is a 128-bit hexadecimal, which was generated automatically
        // by a group launch link. To remain consistent, only get the first 8 hex digits.
        if (!messageLobbySessionId.Contains("Lobby"))
            messageLobbySessionId = "Lobby-" + message.Data.LobbySessionId.Substring(0, 8);

        // no Group Presence yet:
        // app is being launched by this join intent, either
        // through an in-app direct invite, or through a deeplink
        if (groupPresenceState == null)
        {
            string lobbySessionID = message.Data.DestinationApiName == "Lobby"
                ? messageLobbySessionId
                : "Lobby-" + localPlayerState.applicationID;

            groupPresenceState = new SharedSpacesGroupPresenceState();

            StartCoroutine(groupPresenceState.Set(
                    message.Data.DestinationApiName,
                    lobbySessionID,
                    GetMatchSessionID(message.Data.DestinationApiName, messageLobbySessionId),
                    true
                )
            );
        }
        // game was already running, meaning the user already has a Group Presence, and
        // is already either hosting or a client of another host.
        else
        {
            StartCoroutine(SwitchRoom(message.Data.DestinationApiName, messageLobbySessionId, true));
        }
    }

    private void OnInvitationsSent(Message<Oculus.Platform.Models.LaunchInvitePanelFlowResult> message)
    {
        Debug.Log("-------INVITED USERS LIST-------");
        Debug.Log("Size: " + message.Data.InvitedUsers.Count);
        foreach (Oculus.Platform.Models.User user in message.Data.InvitedUsers)
        {
            Debug.Log("Username: " + user.DisplayName);
            Debug.Log("User ID:  " + user.ID);
        }
        Debug.Log("--------------------------------");
    }

    private void LogError(string message, Oculus.Platform.Models.Error error)
    {
        Debug.LogError(message);
        Debug.LogError("ERROR MESSAGE:   " + error.Message);
        Debug.LogError("ERROR CODE:      " + error.Code);
        Debug.LogError("ERROR HTTP CODE: " + error.HttpCode);
    }

    private IEnumerator SwitchRoom(string destination, string lobbySessionID, bool resetSpawnPoint)
    {
        if (resetSpawnPoint)
            SharedSpacesSpawnPoint.Reset();
        else
            SharedSpacesSpawnPoint.Move(destination);

        string lobbySession = destination == "Lobby"
            ? lobbySessionID
            : groupPresenceState.lobbySessionID;

        SharedSpacesSceneLoader.Scenes destination_ = sceneLoader.scenes[destination];

        sceneLoader.LoadScene(destination);
        yield return new WaitUntil(() => sceneLoader.sceneLoaded);

        yield return StartCoroutine(groupPresenceState.Set(
                destination_.ToString(),
                lobbySession,
                GetMatchSessionID(destination_.ToString(), lobbySessionID),
                true
            )
        );

        networkLayer.SwitchPhotonRealtimeRoom(GetPhotonRoomName());
    }

    // only called locally, by owner
    public void OnPortalEnter(string portalName)
    {
        StartCoroutine(SwitchRoom(portalName, groupPresenceState.lobbySessionID, false));
    }

    private string GetPhotonRoomName()
    {
        return groupPresenceState.matchSessionID == ""
            ? groupPresenceState.lobbySessionID
            : groupPresenceState.matchSessionID;
    }

    private string GetMatchSessionID(string destination, string lobbySessionID)
    {
        string matchSessionID = "";

        if (destination != "Lobby")
        {
            matchSessionID = destination == "PurpleRoom"
                ? destination
                : destination + lobbySessionID;
        }

        return matchSessionID;
    }
}
