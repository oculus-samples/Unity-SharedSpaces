// Copyright (c) Facebook, Inc. and its affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-SharedSpaces/tree/main/Assets/SharedSpaces/LICENSE

using System.Collections;
using UnityEngine;
#if !UNITY_EDITOR && !UNITY_STANDALONE_WIN
using Oculus.Platform;
#endif

public class SharedSpacesGroupPresenceState
{
    public string destination { get; private set; }
    public string lobbySessionID { get; private set; }
    public string matchSessionID { get; private set; }
    public bool isJoinable { get; private set; }
    private bool setError;

    public IEnumerator Set(string dest, string lobbyID, string matchID, bool joinable)
    {
#if !UNITY_EDITOR && !UNITY_STANDALONE_WIN
            setError = true;

            GroupPresenceOptions groupPresenceOptions = new GroupPresenceOptions();
            groupPresenceOptions.SetDestinationApiName(dest);
            groupPresenceOptions.SetLobbySessionId(lobbyID);
            groupPresenceOptions.SetMatchSessionId(matchID);
            groupPresenceOptions.SetIsJoinable(joinable);

            // temporary workaround until bug fix
            // GroupPresence.Set() can sometimes fail. Wait until it is done, and if it
            // failed, try again.
            while (setError)
            {
                bool setCompleted = false;

                GroupPresence.Set(groupPresenceOptions).OnComplete(message =>
                {
                    setError = message.IsError;

                    if (setError)
                    {
                        LogError("Failed to setup Group Presence", message.GetError());
                    }
                    else
                    {
                        lobbySessionID = lobbyID;
                        matchSessionID = matchID;
                        isJoinable = joinable;
                        destination = dest;
                       
                        Debug.Log("Group Presence set successfully");
                        Print();
                    }

                    setCompleted = true;
                });

                yield return new WaitUntil(() => setCompleted);
            }
#else
        destination = dest;
        lobbySessionID = lobbyID;
        matchSessionID = matchID;
        isJoinable = joinable;

        Debug.Log("Group Presence set successfully");
        Print();
        yield return null;
#endif
    }

    public void Print()
    {
        Debug.Log("------GROUP PRESENCE STATE------");
        Debug.Log("Destination:      " + destination);
        Debug.Log("Lobby Session ID: " + lobbySessionID);
        Debug.Log("Match Session ID: " + matchSessionID);
        Debug.Log("Joinable?:        " + isJoinable);
        Debug.Log("--------------------------------");
    }

    private void LogError(string message, Oculus.Platform.Models.Error error)
    {
        Debug.LogError(message);
        Debug.LogError("ERROR MESSAGE:   " + error.Message);
        Debug.LogError("ERROR CODE:      " + error.Code);
        Debug.LogError("ERROR HTTP CODE: " + error.HttpCode);
    }
}
