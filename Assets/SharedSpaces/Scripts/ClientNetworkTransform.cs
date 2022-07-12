// Copyright (c) Facebook, Inc. and its affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-SharedSpaces/tree/main/Assets/SharedSpaces/LICENSE

using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// Used for syncing a transform with client side changes. This includes host. Pure server as owner isn't supported by this. Please use NetworkTransform
/// for transforms that'll always be owned by the server.
/// </summary>
[DisallowMultipleComponent]
public class ClientNetworkTransform : NetworkTransform
{
    /// <summary>
    /// Used to determine who can write to this transform. Owner client only.
    /// Changing this value alone will not allow you to create a NetworkTransform which can be written to by clients.
    /// We're using RPCs to send updated values from client to server. Netcode doesn't support client side network variable writing.
    /// This imposes state to the server. This is putting trust on your clients. Make sure no security-sensitive features use this transform.
    /// </summary>
    // This is public to make sure that users don't depend on this IsClient && IsOwner check in their code. If this logic changes in the future, we can make it invisible here

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        CanCommitToTransform = IsOwner;
    }

    protected override void Update()
    {
        CanCommitToTransform = IsOwner;
        base.Update();
        if (NetworkManager != null && (NetworkManager.IsConnectedClient || NetworkManager.IsListening))
        {
            if (CanCommitToTransform)
            {
                TryCommitTransformToServer(transform, NetworkManager.LocalTime.Time);
            }
        }
    }
}
