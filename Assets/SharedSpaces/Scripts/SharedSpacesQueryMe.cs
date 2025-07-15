// Copyright (c) Facebook, Inc. and its affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-SharedSpaces/tree/main/Assets/SharedSpaces/LICENSE

using Meta.XR.Samples;
using UnityEngine;
using Unity.Netcode;

[MetaCodeSample("SharedSpaces")]
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
