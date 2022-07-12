// Copyright (c) Facebook, Inc. and its affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-SharedSpaces/tree/main/Assets/SharedSpaces/LICENSE

using UnityEngine;
using Unity.Netcode;

public class SharedSpacesSpawner : MonoBehaviour
{
    public NetworkObject playerPrefab;
    public NetworkObject sessionPrefab;

    void OnEnable()
    {
        DontDestroyOnLoad(this);
    }

    public NetworkObject SpawnPlayer(ulong clientId, Vector3 position, Quaternion rotation)
    {
        NetworkObject player = Instantiate(playerPrefab, position, rotation);
        player.SpawnAsPlayerObject(clientId);

        return player;
    }

    public NetworkObject SpawnSession()
    {
        NetworkObject session = Instantiate(sessionPrefab);
        session.Spawn();

        return session;
    }
}
