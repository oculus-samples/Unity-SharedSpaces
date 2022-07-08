// Copyright (c) Facebook, Inc. and its affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-SharedSpaces/tree/main/Assets/SharedSpaces/LICENSE

using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Oculus.Platform;

public class SharedSpacesInvitePanel : MonoBehaviour
{
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
    private Canvas joinRoomCanvas;
    private InputField inputField;
#endif

    private void OnEnable()
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        joinRoomCanvas = FindObjectOfType<Temp>(true)?.GetComponent<Canvas>();
        inputField = joinRoomCanvas?.GetComponentInChildren<InputField>();
#endif
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.GetComponent<NetworkObject>().IsOwner) return;

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        joinRoomCanvas?.gameObject?.SetActive(true);
        inputField?.ActivateInputField();
#else
        GroupPresence.LaunchInvitePanel(new InviteOptions());
#endif
    }

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
    private void OnTriggerExit(Collider other)
    {
        if (!other.GetComponent<NetworkObject>().IsOwner) return;

        joinRoomCanvas?.gameObject?.SetActive(false);
    }
#endif
}
