// Copyright (c) Facebook, Inc. and its affiliates.

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
