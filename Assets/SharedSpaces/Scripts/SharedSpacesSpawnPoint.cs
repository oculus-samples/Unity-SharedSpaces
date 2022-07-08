// Copyright (c) Facebook, Inc. and its affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-SharedSpaces/tree/main/Assets/SharedSpaces/LICENSE

using UnityEngine;

public class SharedSpacesSpawnPoint : MonoBehaviour
{
    public static SharedSpacesSpawnPoint singleton;

    private void OnEnable()
    {
        if (singleton && singleton != this)
        {
            Destroy(gameObject);
            return;
        }

        singleton = this;
        Reset();

        DontDestroyOnLoad(this);
    }

    [SerializeField] Transform fromBlueRoom;
    [SerializeField] Transform fromGreenRoom;
    [SerializeField] Transform fromRedRoom;
    [SerializeField] Transform fromPurpleRoom;
    [SerializeField] Transform fromLobby;

    public Vector3 SpawnPosition { get; private set; }
    public Quaternion SpawnRotation { get; private set; }

    public static void Move(string room)
    {
        var target = (room, SharedSpacesSceneLoader.currentScene) switch
        {
            ("To Lobby", SharedSpacesSceneLoader.Scenes.BlueRoom) => singleton.fromBlueRoom,
            ("To Lobby", SharedSpacesSceneLoader.Scenes.GreenRoom) => singleton.fromGreenRoom,
            ("To Lobby", SharedSpacesSceneLoader.Scenes.RedRoom) => singleton.fromRedRoom,
            ("To Lobby", _) => singleton.fromPurpleRoom,
            _ => singleton.fromLobby,
        };
        singleton.SpawnPosition = target.position;
        singleton.SpawnRotation = target.rotation;
    }

    public static void Reset()
    {
        singleton.SpawnPosition = new Vector3(0.0f, 0.24f, 0.0f);
        singleton.SpawnRotation = Quaternion.Euler(0.0f, 270.0f, 0.0f);
    }
}
