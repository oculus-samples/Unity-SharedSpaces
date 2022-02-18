// Copyright (c) Facebook, Inc. and its affiliates.

using System.Collections;
using UnityEngine;
#if !UNITY_EDITOR && !UNITY_STANDALONE_WIN
using UnityEngine.Android;
#endif
using Unity.Netcode;
using Photon.Voice.Unity;
using Photon.Voice.Unity.UtilityScripts;
using System.Linq;

public class SharedSpacesVoip : MonoBehaviour
{
    public GameObject sharedSpacesSpeakerPrefab;
    public GameObject sharedSpacesRecorderPrefab;

    private GameObject sharedSpacesRecorder;

    private void OnEnable()
    {
        DontDestroyOnLoad(this);
    }

    private void Start()
    {
#if !UNITY_EDITOR && !UNITY_STANDALONE_WIN
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }
#endif
    }

    public void StartVoip(Transform parent)
    {
        sharedSpacesRecorder = Instantiate(sharedSpacesRecorderPrefab, parent);

        VoiceConnection voiceConnection = sharedSpacesRecorder.GetComponent<VoiceConnection>();
        SharedSpacesPlayerState playerState = FindObjectsOfType<SharedSpacesPlayerState>().First(p => p.IsLocalPlayer);
        voiceConnection.Client.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
        {
            [nameof(NetworkObject.NetworkObjectId)] = (int)playerState.NetworkObjectId,
        });
        voiceConnection.SpeakerFactory = (playerId, voiceId, userData) => CreateSpeaker(playerId, voiceConnection);

        StartCoroutine(JoinPhotonVoiceRoom());
    }

    private IEnumerator JoinPhotonVoiceRoom()
    {
        yield return new WaitUntil(() => SharedSpacesSession.photonVoiceRoom != "" && sharedSpacesRecorder != null);

#if !UNITY_EDITOR && !UNITY_STANDALONE_WIN
        if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
#endif
            ConnectAndJoin connectAndJoin = sharedSpacesRecorder.GetComponent<ConnectAndJoin>();
            connectAndJoin.RoomName = SharedSpacesSession.photonVoiceRoom;
            connectAndJoin.ConnectNow();

#if !UNITY_EDITOR && !UNITY_STANDALONE_WIN
        }
#endif
    }

    private Speaker CreateSpeaker(int playerId, VoiceConnection voiceConnection)
    {
        var actor = voiceConnection.Client.LocalPlayer.Get(playerId);
        Debug.Assert(actor != null, $"Could not find voice client for Player #{playerId}");

        actor.CustomProperties.TryGetValue(nameof(NetworkObject.NetworkObjectId), out var networkId);
        Debug.Assert(networkId != null, $"Could not find network object id for Player #{playerId}");

        NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue((ulong)(int)networkId, out var player);
        Debug.Assert(player != null, $"Could not find player instance for Player #{playerId} network id #{networkId}");
        
        Speaker speaker = Instantiate(sharedSpacesSpeakerPrefab, player.transform).GetComponent<Speaker>();
        float headHeight = speaker.GetComponentInParent<CharacterController>().height;

        speaker.transform.localPosition = new Vector3(0.0f, headHeight, 0.0f);

        return speaker;
    }
}
