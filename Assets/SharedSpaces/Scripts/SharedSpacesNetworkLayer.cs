// Copyright (c) Facebook, Inc. and its affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.Transports.Tasks;
using MLAPI.Transports.PhotonRealtime;
using Photon.Realtime;

[RequireComponent(typeof(PhotonRealtimeTransport))]
public class SharedSpacesNetworkLayer : MonoBehaviour, IConnectionCallbacks, IInRoomCallbacks
{
    public enum ClientState
    {
        StartingHost,
        StartingClient,
        MigratingHost,
        MigratingClient,
        RestoringHost,
        RestoringClient,
        SwitchingPhotonRealtimeRoom,
        Connected
    }

    public ClientState clientState { get; private set; }

    public Action<ulong> OnClientConnectedCallback;
    public Action<ulong> OnClientDisconnectedCallback;
    public Func<ulong> OnMasterClientSwitchedCallback;
    public Action StartHostCallback;
    public Action StartClientCallback;
    public Action RestoreHostCallback;
    public Action RestoreClientCallback;

    private PhotonRealtimeTransport photonRealtime;

    private void OnEnable()
    {
        DontDestroyOnLoad(this);
        photonRealtime = GetComponent<PhotonRealtimeTransport>();
    }

    public void Init(string room)
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        photonRealtime.RoomName = room;
        StartCoroutine(StartHost());
    }

    private IEnumerator StartHost()
    {
        SocketTasks startHost = NetworkManager.Singleton.StartHost();
        photonRealtime.Client.AddCallbackTarget(this);

        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        yield return new WaitUntil(() => startHost.IsDone);

        clientState = ClientState.Connected;

        StartHostCallback.Invoke();

        Debug.LogWarning("You are the host.");
    }

    private IEnumerator StartClient()
    {
        SocketTasks startClient = NetworkManager.Singleton.StartClient();
        photonRealtime.Client.AddCallbackTarget(this);

        yield return new WaitUntil(() => startClient.IsDone);

        // It seems that NetworkManager on the client side sometimes doesn't update the ConnectedClients dictionary fast enough
        yield return new WaitUntil(() => NetworkManager.Singleton.ConnectedClients.ContainsKey(NetworkManager.Singleton.LocalClientId));
        yield return new WaitUntil(() => NetworkManager.Singleton.ConnectedClients[NetworkManager.Singleton.LocalClientId].PlayerObject);

        clientState = ClientState.Connected;

        StartClientCallback.Invoke();

        Debug.LogWarning("You are a client.");
    }

    private IEnumerator RestoreHost()
    {
        SocketTasks startHost = NetworkManager.Singleton.StartHost();
        photonRealtime.Client.AddCallbackTarget(this);

        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        yield return new WaitUntil(() => startHost.IsDone);

        clientState = ClientState.Connected;

        RestoreHostCallback.Invoke();

        Debug.LogWarning("You are the host.");
    }

    private IEnumerator RestoreClient()
    {
        SocketTasks startClient = NetworkManager.Singleton.StartClient();
        photonRealtime.Client.AddCallbackTarget(this);

        yield return new WaitUntil(() => startClient.IsDone);

        // It seems that NetworkManager on the client side sometimes doesn't update the ConnectedClients dictionary fast enough
        yield return new WaitUntil(() => NetworkManager.Singleton.ConnectedClients.ContainsKey(NetworkManager.Singleton.LocalClientId));
        yield return new WaitUntil(() => NetworkManager.Singleton.ConnectedClients[NetworkManager.Singleton.LocalClientId].PlayerObject);

        clientState = ClientState.Connected;

        RestoreClientCallback.Invoke();

        Debug.LogWarning("You are a client.");
    }

    public void OnDisconnected(DisconnectCause cause)
    {
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;

        switch (clientState)
        {
            case ClientState.StartingHost:
                // happened because of room name conflict, meaning this 
                // photon room already exist, so join as a client instead
                Debug.LogWarning("HOSTING FAILED. ATTEMPTING TO JOIN AS CLIENT INSTEAD.");
                clientState = ClientState.StartingClient;
                StopCoroutine(StartHost());
                StartCoroutine(StartClient());
                break;

            case ClientState.StartingClient:
                // happened because room may have stopped being hosted while joining it
                Debug.LogWarning("JOINING AS CLIENT FAILED. ATTEMPTING TO HOST INSTEAD.");
                clientState = ClientState.StartingHost;
                StopCoroutine(StartClient());
                StartCoroutine(StartHost());
                break;

            case ClientState.MigratingHost:
                Debug.LogWarning("MIGRATING AS HOST.");
                clientState = ClientState.RestoringHost;
                StartCoroutine(RestoreHost());
                break;

            case ClientState.MigratingClient:
                // there is a possibility that client migration fails while the fallback host
                // is taking over as host, meaning this codepath might be taken more than once
                Debug.LogWarning("MIGRATING AS CLIENT.");
                clientState = ClientState.RestoringClient;
                StartCoroutine(RestoreClient());
                break;

            case ClientState.RestoringHost:
                Debug.LogWarning("RESTORING HOST FAILED. RESTORING AS CLIENT INSTEAD.");
                clientState = ClientState.RestoringClient;
                StopCoroutine(RestoreHost());
                StartCoroutine(RestoreClient());
                break;

            case ClientState.RestoringClient:
                Debug.LogWarning("RESTORING CLIENT FAILED. RESTORING AS HOST INSTEAD.");
                clientState = ClientState.RestoringHost;
                StopCoroutine(RestoreClient());
                StartCoroutine(RestoreHost());
                break;

            case ClientState.SwitchingPhotonRealtimeRoom:
                Debug.LogWarning("SWITCHING ROOM.");
                clientState = ClientState.StartingClient;
                StartCoroutine(StartClient());
                break;

            case ClientState.Connected:
                if (cause == DisconnectCause.ServerTimeout)
                {
                    Debug.LogWarning("SERVER TIMEOUT. RESTORING AS CLIENT.");
                    clientState = ClientState.RestoringClient;
                    StartCoroutine(RestoreClient());
                }

                break;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        OnClientConnectedCallback.Invoke(clientId);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        OnClientDisconnectedCallback.Invoke(clientId);
    }

    public void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.LogWarning("HOST LEFT, MIGRATING...");

        clientState = OnMasterClientSwitchedCallback() == NetworkManager.Singleton.LocalClientId
            ? ClientState.MigratingHost
            : ClientState.MigratingClient;
    }

    public void SwitchPhotonRealtimeRoom(string room)
    {
        photonRealtime.RoomName = room;
        clientState = ClientState.SwitchingPhotonRealtimeRoom;

        if (NetworkManager.Singleton.IsHost) NetworkManager.Singleton.StopHost();
        else                                 NetworkManager.Singleton.StopClient();
    }

    // no need to implement them at the moment
    public void OnConnected() {}
    public void OnConnectedToMaster() {}
    public void OnRegionListReceived(RegionHandler regionHandler) {}
    public void OnCustomAuthenticationResponse(Dictionary<string, object> data) {}
    public void OnCustomAuthenticationFailed(string debugMessage) {}
    public void OnPlayerEnteredRoom(Player newPlayer) {}
    public void OnPlayerLeftRoom(Player otherPlayer) {}
    public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged) {}
    public void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps) {}
}
