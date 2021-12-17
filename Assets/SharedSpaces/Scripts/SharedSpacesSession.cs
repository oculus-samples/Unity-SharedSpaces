// Copyright (c) Facebook, Inc. and its affiliates.

using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class SharedSpacesSession : NetworkBehaviour
{
    public static ulong fallbackHostId { get; private set; }
    public static string photonVoiceRoom { get; private set; }

    private SharedSpacesSpawner spawner;
    private SharedSpacesLocalPlayerState localPlayerState;

    private void Awake()
    {
        fallbackHostId = ulong.MaxValue;
        spawner = FindObjectOfType<SharedSpacesSpawner>();
        localPlayerState = FindObjectOfType<SharedSpacesLocalPlayerState>();
        StartCoroutine(InitPhotonRoomName());
    }

    public void DetermineFallbackHost(ulong clientId)
    {
        // if the new client that joined has a smaller id, 
        // make them the new fallback host
        if (clientId < fallbackHostId)
        {
            // broadcast to all clients
            SetFallbackHostClientRpc(clientId);
        }
        // this new client that joined didn't change the fallback host information.
        // just send the current fallback host information to this new client.
        else
        {
            // only broadcast to new client
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientId }
                }
            };

            SetFallbackHostClientRpc(fallbackHostId, clientRpcParams);
        }
    }

    public void RedetermineFallbackHost(ulong clientId)
    {
        // if true, not the fallback host that left
        if (clientId != fallbackHostId) return;

        // reset fallback host id
        fallbackHostId = ulong.MaxValue;

        // if true, only original host is left
        if (NetworkManager.Singleton.ConnectedClients.Count < 2) return;

        // the fallbackhost left, pick another client to be the host
        foreach (ulong id in NetworkManager.Singleton.ConnectedClients.Keys)
        {
            if (id == NetworkManager.Singleton.ServerClientId)
                continue;

            if (id < fallbackHostId)
                fallbackHostId = id;
        }

        // broadcast new fallback host to all clients
        SetFallbackHostClientRpc(fallbackHostId);
    }

    public void SetPhotonVoiceRoom(ulong clientId)
    {
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };

        SetPhotonVoiceRoomClientRpc(photonVoiceRoom, clientRpcParams);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestSpawnServerRpc(ulong clientId, Vector3 position, Quaternion rotation)
    {
        spawner.SpawnPlayer(clientId, position, rotation);
    }

    [ClientRpc]
    private void SetFallbackHostClientRpc(ulong fallbackHostId_, ClientRpcParams clientRpcParams = default)
    {
        fallbackHostId = fallbackHostId_;

        Debug.Log("------FALLBACK HOST STATE-------");
        Debug.Log("Client ID: " + fallbackHostId.ToString());

        if (fallbackHostId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.LogWarning("You are the new fallback host");
        }
        Debug.Log("--------------------------------");
    }

    [ClientRpc]
    private void SetPhotonVoiceRoomClientRpc(string photonVoiceRoom_, ClientRpcParams clientRpcParams)
    {
        Debug.Log("PHOTON VOICE ROOM TO JOIN: " + photonVoiceRoom_);
        photonVoiceRoom = photonVoiceRoom_;
    }

    private IEnumerator InitPhotonRoomName()
    {
        photonVoiceRoom = "";

        if (NetworkManager.Singleton.IsHost)
        {
            yield return new WaitUntil(() => localPlayerState.username != "");
            photonVoiceRoom = localPlayerState.username;
        }
    }
}
