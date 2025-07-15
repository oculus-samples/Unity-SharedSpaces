// Copyright (c) Facebook, Inc. and its affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-SharedSpaces/tree/main/Assets/SharedSpaces/LICENSE

using Meta.XR.Samples;
using UnityEngine;
using Unity.Netcode;

[MetaCodeSample("SharedSpaces")]
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
