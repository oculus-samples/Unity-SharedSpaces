// Copyright (c) Facebook, Inc. and its affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-SharedSpaces/tree/main/Assets/SharedSpaces/LICENSE

using UnityEngine;
using TMPro;
using Unity.Netcode;

public class SharedSpacesPortal : MonoBehaviour
{
    public TMP_Text room;

    private SharedSpacesApplication application;

    private void Start()
    {
        application = FindObjectOfType<SharedSpacesApplication>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.GetComponent<NetworkObject>().IsOwner) return;

        application.OnPortalEnter(room.text);
    }
}
