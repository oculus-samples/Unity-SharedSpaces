// Copyright (c) Facebook, Inc. and its affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Netcode.Transports.PhotonRealtime;
using Photon.Realtime;
using System.Net.WebSockets;

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
        NetworkManager.Singleton.StartHost();
        photonRealtime.Client.AddCallbackTarget(this);

        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        clientState = ClientState.Connected;

        StartHostCallback.Invoke();

        Debug.LogWarning("You are the host.");
        yield break;
    }

    private IEnumerator StartClient()
    {
        NetworkManager.Singleton.StartClient();
        photonRealtime.Client.AddCallbackTarget(this);

        yield return WaitForLocalPlayerObject();
        if (clientState != ClientState.StartingClient)
            yield break;

        clientState = ClientState.Connected;

        StartClientCallback.Invoke();

        Debug.LogWarning("You are a client.");
    }

    private static WaitUntil WaitForLocalPlayerObject()
    {
        return new WaitUntil(() => NetworkManager.Singleton.LocalClient != null && NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject() != null);
    }

    private IEnumerator RestoreHost()
    {
        NetworkManager.Singleton.StartHost();
        photonRealtime.Client.AddCallbackTarget(this);

        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        clientState = ClientState.Connected;

        RestoreHostCallback.Invoke();

        Debug.LogWarning("You are the host.");
        yield break;
    }

    private IEnumerator RestoreClient()
    {
        NetworkManager.Singleton.StartClient();
        photonRealtime.Client.AddCallbackTarget(this);

        yield return WaitForLocalPlayerObject();
        if (clientState != ClientState.RestoringClient)
            yield break;

        clientState = ClientState.Connected;

        RestoreClientCallback.Invoke();

        Debug.LogWarning("You are a client.");
    }

    public void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"OnDisconnected: {cause}");

        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;

        // For some reason, sometimes another shutdown is coming,
        // but won't progress unless you start the client.
        if (cause is DisconnectCause.DisconnectByClientLogic && NetworkManager.Singleton.ShutdownInProgress)
        {
            NetworkManager.Singleton.StartClient();
            photonRealtime.Client.AddCallbackTarget(this);
            return;
        }

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
        IEnumerator Routine()
        {
            // For some reason, OnClientConnectedCallback is called before OnServerStarted, so wait
            if (NetworkManager.Singleton.IsHost)
                yield return new WaitUntil(() => clientState == ClientState.Connected);
            OnClientConnectedCallback.Invoke(clientId);
        }
        StartCoroutine(Routine());
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

        NetworkManager.Singleton.Shutdown();
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
