// Copyright (c) Facebook, Inc. and its affiliates.

using UnityEngine;
using Unity.Netcode;

public class SharedSpacesPaintShop : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        NetworkObject player = other.GetComponent<NetworkObject>();

        if (!player.IsOwner) return;

        // Naively assumes only collider to trigger it is a SharedSpacesCharacter prefab instance. 
        // If that changes later, add some checks.
        other.GetComponent<SharedSpacesPlayerState>().SetColor();
    }
}
