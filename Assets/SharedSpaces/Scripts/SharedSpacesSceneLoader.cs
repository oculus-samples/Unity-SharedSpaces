// Copyright (c) Facebook, Inc. and its affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-SharedSpaces/tree/main/Assets/SharedSpaces/LICENSE

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SharedSpacesSceneLoader : MonoBehaviour
{
    public enum Scenes
    {
        None,
        Lobby,
        BlueRoom,
        GreenRoom,
        RedRoom,
        PurpleRoom
    }

    public static Scenes currentScene;

    [HideInInspector]
    public bool sceneLoaded = false;

    public Dictionary<string, Scenes> scenes { get; private set; }

    private void OnEnable()
    {
        DontDestroyOnLoad(this);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        scenes = new Dictionary<string, Scenes>
        {
            // portal names
            { "To Lobby", Scenes.Lobby },
            { "To Blue Room", Scenes.BlueRoom },
            { "To Green Room", Scenes.GreenRoom },
            { "To Red Room", Scenes.RedRoom },
            { "To Public Purple Room", Scenes.PurpleRoom },
            // actual names
            { "Lobby", Scenes.Lobby },
            { "BlueRoom", Scenes.BlueRoom },
            { "GreenRoom", Scenes.GreenRoom },
            { "RedRoom", Scenes.RedRoom },
            { "PurpleRoom", Scenes.PurpleRoom }
        };
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        sceneLoaded = true;
        currentScene = (Scenes)scene.buildIndex;
    }

    public void LoadScene(string scene)
    {
        LoadScene(scenes[scene]);
    }

    public void LoadScene(Scenes scene)
    {
        if (scene == currentScene) return;

        sceneLoaded = false;
        SceneManager.LoadSceneAsync((int)scene);
    }
}
