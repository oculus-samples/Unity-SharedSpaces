// Copyright (c) Facebook, Inc. and its affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-SharedSpaces/tree/main/Assets/SharedSpaces/LICENSE

using UnityEngine;
using Unity.Netcode;
using Oculus.Platform;

public class SharedSpacesQueryFriends : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.GetComponent<NetworkObject>().IsOwner) return;

        Users.GetLoggedInUserFriends().OnComplete(QueryFriends);
    }

    private void QueryFriends(Message<Oculus.Platform.Models.UserList> users)
    {
        foreach (Oculus.Platform.Models.User user in users.Data)
        {
            Debug.Log("------" + user.DisplayName + "------");
            Debug.Log("Destination:      " + user.PresenceDestinationApiName);
            Debug.Log("Lobby Session ID: " + user.PresenceLobbySessionId);
            Debug.Log("Match Session ID: " + user.PresenceMatchSessionId);
            Debug.Log("--------------------------------");
        }
    }
}
