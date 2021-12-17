// Copyright (c) Facebook, Inc. and its affiliates.

using UnityEngine;
using Unity.Netcode;

public class SharedSpacesQueryMe : MonoBehaviour
{
    private SharedSpacesApplication application;

    private void OnEnable()
    {
        application = FindObjectOfType<SharedSpacesApplication>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.GetComponent<NetworkObject>().IsOwner) return;

        application.groupPresenceState.Print();
    }
}
