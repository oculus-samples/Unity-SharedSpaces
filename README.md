![Showcase Banner](./Media/banner.png "SharedSpaces")

# SharedSpaces

SharedSpaces was built by the VR Developer Tools team to demonstrate how you can quickly get people together in VR using the Oculus Social Platform APIs.  This version was built for the Unity engine using [Photon Realtime](https://github.com/Unity-Technologies/multiplayer-community-contributions/tree/main/Transports/com.community.netcode.transport.photon-realtime) as the transport layer and [Unity Netcode for GameObjects](https://github.com/Unity-Technologies/com.unity.netcode.gameobjects). Other versions are available, in particular one built for the [Unreal Engine](https://github.com/oculus-samples/Unreal-SharedSpaces).

This codebase is available both as a reference and as a template for multiplayer VR games. The [Oculus License](LICENSE) applies to the SDK and supporting material. The MIT License applies to only certain, clearly marked documents. If an individual file does not indicate which license it is subject to, then the Oculus License applies.

See the [CONTRIBUTING](CONTRIBUTING.md) file for how to help out.

## Getting started

First, ensure you have Git LFS installed by running this command:
```sh
git lfs install
```

Then, clone this repo using the "Code" button above, or this command:
```sh
git clone https://github.com/oculus-samples/Unity-SharedSpaces.git
```

To run the showcase, open the project folder in *Unity 2020.3.15f1* or newer. Load the [Assets/SharedSpaces/Scenes/Startup](Assets/SharedSpaces/Scenes/Startup.unity) scene.

After loading the scene, you may encounter this pop-up:

<div style="text-align: center; padding: 10pt;"><img src="./Media/tmp_essentials.png" width="650"></div>

Click "Import TMP Essentials" to import the necessary TextMesh Pro assets.

## Setting up Photon

To get the sample working, you will need to configure the NetDriver with your own Photon account. Their base plan is free.
- Visit [photonengine.com](https://www.photonengine.com) and [create an account](https://doc.photonengine.com/en-us/realtime/current/getting-started/obtain-your-app-id)
- From your Photon dashboard, click “Create A New App”
- Fill out the form making sure to set type to “Photon Realtime”. Then click Create.

Your new app will now show on your Photon dashboard. Click the App ID to reveal the full string and copy the value.

Paste your App ID in [Assets/Photon/Resources/PhotonAppSettings](Assets/Photon/Resources/PhotonAppSettings.asset).

The Photon Realtime transport should now work. You can check the dashboard in your Photon account to verify there is network traffic.

## Where are the Oculus and Photon packages?

In order to keep the project organized, the [Oculus Integration](https://assetstore.unity.com/packages/tools/integration/oculus-integration-82022) and [Photon Voice 2](https://assetstore.unity.com/packages/tools/audio/photon-voice-2-130518) packages are stored in the [Packages](./Packages) folder. To update them, import their updated Asset Store packages, then copy them into their respective `Packages` folders.

The *Oculus Integration* package is released under the *[Oculus SDK License Agreement](./Packages/Oculus/LICENSE.txt)*.

The *Photon Voice 2* package is released under the *[License Agreement for Exit Games Photon](./Packages/Photon/Photon/license.txt)*.

Also, the Photon Realtime package is referenced in [Packages/manifest.json](./Packages/manifest.json) as `com.mlapi.contrib.transport.photon-realtime`.

***

<div style="width: 60%; padding: 10pt;">
<table>
<tr style="background-color:#EEEEEE;">
<td>
A. <a href="#A">Overview of SharedSpaces</a><br/>
B. <a href="#B">SharedSpaces in Action</a><br/>
C. <a href="#C">Oculus Application Configuration</a><br/>
&nbsp;&nbsp;&nbsp;1. <a href="#C1">Application Identifier</a><br/>
&nbsp;&nbsp;&nbsp;2. <a href="#C2">Destinations</a><br/>
</td>
</tr>
</table>
</div>

# A. <a id="A">Overview of SharedSpaces</a>

<div style="margin: auto; width: 60%; padding: 10pt;">
<table>
<tr style="background-color:#FFEEEE;">
	<td style="border:0px;"><b>Oculus</b></td>
	<td style="border:0px;">Group presence with <i>destination</i>, <i>lobby</i> and <i>match</i> ids.</td>
</tr>
<tr style="background-color:#EEFFEE;">
	<td style="border:0px;"><b>Photon Realtime</b></td>
	<td style="border:0px;">Transport via a <i>room</i> named after the <i>lobby</i> or <i>match</i> id.</td>
</tr>
<tr style="background-color:#EEEEFF;">
	<td style="border:0px;"><b>Netcode for GameObjects</b></td>
	<td style="border:0px;">Replication between <i>room members</i> with <i>the master client</i> as host.</td>
</tr>
</table>
</div>

SharedSpaces networking is divided into three layers.  The Oculus layer provides presence information needed to find and connect with friends.  The Photon layer provides the transport layer for sending messages to other players.  And the Netcode for GameObjects layer handles the replication of game objects.

In this overview we will explore each of these layers and show how we connected them together to make a simple multiplayer application which allows people to connect and play together, without the need for a dedicated server.

## *A Private Lobby Connected to Rooms*
<div style="text-align: center; padding: 10pt;"><img src="./Media/layout.png" align="middle" width="600"></div>

SharedSpaces is made of a few connected levels, known as destinations.  In the center is your personal lobby with doors leading to the surrounding matches.  The matches on the left are private and are reachable from your own lobby only.  The match on the right is public, reachable from any lobby.

## *Social Layer - Destination, Lobby & Match Session IDs*

<div style="text-align: center; padding: 10pt;"><img src="./Media/presence.png" align="middle" width="750"></div>

We use this layout as a direct representation of the new group presence apis.  To get you to a SharedSpaces destination, we first set your destination and a pair of session identifiers in your group presence: one for your lobby session id, which should not change very often, and one for your match session id, only set when you join a match.

The destinations are specific areas of your application that are defined on the [Oculus dashboard](https://developer.oculus.com/manage) under **Platform Services > Destinations**. The lobby session id represents a tight group of people that want to stay together between games and possibly play as part of the same team during matches.  The match session id is shared by people currently playing a match together, whether they are on the same team or not.

<div style="text-align: center; padding: 10pt;"><img src="./Media/invitation_to_lobby.png" align="middle" width="600"></div>

When you first launch SharedSpaces, you start in your own private lobby for which we create a unique id. To form a group to be with before and after matches, you invite people to share your lobby.  If they accept the invitation, their lobby session id will be updated to be the same as yours, and whenever you will be in  the lobby at the same time, you will be together in the same space.

You can think of the lobby as the base camp for your group.  Different groups always go back to their respective lobbies after matches.

<div style="text-align: center; padding: 10pt;"><img src="./Media/private_room.png" align="middle" width="700"></div>

Members of your group are free to travel at any time between your lobby and their private matches. This only affects the match session ids of their group presence.

<div style="text-align: center; padding: 10pt;"><img src="./Media/invitation_to_match.png" align="middle" width="700"></div>

You can also grant access to your private match to anyone.  You invite them from that match, and they join you when they accept the invitation.  In SharedSpaces, accepting an invitation to a match only affects your match session id, not your lobby session id.  

<div style="text-align: center; padding: 10pt;"><img src="./Media/respective_lobbies.png" align="middle" width="650"></div>

As a consequence, when they leave the match through the lobby door, users effectively go back to their separate lobbies if they are not members of the same group.

<div style="text-align: center; padding: 10pt;"><img src="./Media/public_room.png" align="middle" width="650"></div>

SharedSpaces also has the purple room to represent a public match that is reachable from all lobbies. Again, anybody is free to go from their lobby to the purple room at any time, and it only affects their match session id.  It is a space where you can meet people from outside your group without a prior invitation.

## *Transport Layer - Photon Rooms*

To connect users, Photon has the concept of room.  People in the same match or lobby instance will be in the same Photon room in order for data to flow between them.  The transport layer is responsible for routing packets between your users who are most likely behind network firewalls.

<div style="text-align: center; padding: 10pt;"><img src="./Media/session_to_room.png" align="middle" width="650"></div>

Photon rooms have *unique names*.  The name of the room that we will use comes directly from the social layer: we either use your match session id, if you have one, or your lobby session id, otherwise.

A key feature of the Photon room system is that it keeps track of the oldest member in the room, called the “master client”, here identified with stars.

<div style="text-align: center; padding: 10pt;"><img src="./Media/photon_join_or_create.png" align="middle" width="650"></div>

Let’s look at Alice, Bob and Charlie entering the Purple room.  Charlie is first to join, so the room is created for him and he is marked as its **master client**.  Alice and Bob join shortly after and they are added as **normal clients**.

<div style="text-align: center; padding: 10pt;"><img src="./Media/photon_notification.png" align="middle" width="650"></div>

If Charlie, as the master client, leaves the room, a new master client is selected and all remaining clients are notified of that change.  This is a key feature for the next networking layer.

## *Game Replication Layer - Netcode for GameObjects*

You can find extensive documentation on Netcode for GameObjects on the [Unity Documentation site](https://docs-multiplayer.unity3d.com/docs/tutorials/helloworld/helloworldintro).

For some applications, like SharedSpaces, we can host the server on one of the headsets as a listen-server.  In that mode, the game acts both as a server and as its first connected client.  It will accept connections from the other players.

So for each room, we need to select one of the users to be the listen-server.  This decision comes from the transport layer: the **master client** of the corresponding Photon room will be our host.

<div style="text-align: center; padding: 10pt;"><img src="./Media/photon_to_ue4_1.png" align="middle" width="650"></div>

When the player hosting leaves, we perform a host migration.  Here we can see that Alice is leaving the purple room.  Photon picks Bob as the new master client. The remaining members of the room are notified and they reestablish their Unity connections.

<div style="text-align: center; padding: 10pt;"><img src="./Media/photon_to_ue4_2.png" align="middle" width="650"></div>

We end up with two Photon rooms, the Purple room is now hosted by Bob, with Charlie and Donna connected to him. Alice just left the room through the door to her lobby, but since she is the only one there, she becomes both the master client and host of her group lobby.


# B. <a id="B">SharedSpaces in Action</a>

Let’s have a look at SharedSpaces in action.

<div style="text-align: center; padding: 10pt;">
	<img src="./Media/screenshots/1a.jpg" width="250">
	<img src="./Media/screenshots/1b.jpg" width="250">
</div>

When Alice starts SharedSpaces, she starts alone in her private lobby.  She is the master client and host of the lobby, as indicated by the star next to her name.

<div style="text-align: center; padding: 10pt;">
	<img src="./Media/screenshots/2a.jpg" width="250">
	<img src="./Media/screenshots/2b_id.jpg" width="250">
	<img src="./Media/screenshots/2c.jpg" width="250">
	<img src="./Media/screenshots/2d.jpg" width="250">
</div>

Alice wants Bob to form a group with her so that they can be together between matches. To do that, she steps on the invite panel switch and she sends him an invitation from her lobby. By accepting, SharedSpaces starts on Bob’s headset with a deeplink message that will let him join Alice in game.  From now on, Bob will have the same lobby session id as Alice and they will share the same lobby.

<div style="text-align: center; padding: 10pt;">
	<img src="./Media/screenshots/3a.jpg" width="250">
	<img src="./Media/screenshots/3b.jpg" width="250">
	<img src="./Media/screenshots/3c.jpg" width="250">
	<img src="./Media/screenshots/3d.jpg" width="250">
</div>

Bob goes through the blue door to start a private match, followed by Alice. They end up in the same Blue Room and they now have the same match session id that corresponds to their private room.  Since Bob was there first, he is the one hosting the room and Alice is connected to him.

<div style="text-align: center; padding: 10pt;">
	<img src="./Media/screenshots/4a_id.jpg" width="250">
	<img src="./Media/screenshots/4b_id.jpg" width="250">
	<img src="./Media/screenshots/4c.jpg" width="250">
	<img src="./Media/screenshots/4d.jpg" width="250">
</div>

Alice decides to invite her friend Charlie to join their match, and he happens to be in his own lobby when he accepts the invitation.  Charlie has his match session id updated with the private match id, but on the other hand he still retains his own lobby session id.  He is still part of a different group.

<div style="text-align: center; padding: 10pt;">
	<img src="./Media/screenshots/5a.jpg" width="250">
	<img src="./Media/screenshots/5b.jpg" width="250">
	<img src="./Media/screenshots/5c.jpg" width="250">
</div>

When Bob leaves the blue room, Photon notifies Alice and Charlie that the master client has changed. A host migration is needed: Alice opens a new listen-server, since she is the new master client of the blue room, and Charlie connects to her.

As for Bob, he started hosting his group lobby.

<div style="text-align: center; padding: 10pt;">
	<img src="./Media/screenshots/6a.jpg" width="250">
	<img src="./Media/screenshots/6b.jpg" width="250">
</div>
<div style="text-align: center; padding: 10pt;">
	<img src="./Media/screenshots/6c_id.jpg" width="250">
	<img src="./Media/screenshots/6d_id.jpg" width="250">
	<img src="./Media/screenshots/6e_id.jpg" width="250">
</div>

Now when Charlie leaves the blue room, he does not join Bob. They are not part of the same group since they do have different lobby session ids. Instead, he goes back to his own separate lobby.  This can be checked by stepping on the roster panel switch and you will see your different groups explicitly listed.

<div style="text-align: center; padding: 10pt;">
	<img src="./Media/screenshots/7a.jpg" width="250">
	<img src="./Media/screenshots/7b.jpg" width="250">
</div>

In the case of Alice, therefore, going back to lobby means that she will rejoin Bob who is waiting for her.

<div style="text-align: center; padding: 10pt;">
	<img src="./Media/screenshots/8a_id.jpg" width="250">
	<img src="./Media/screenshots/8b_id.jpg" width="250">
	<img src="./Media/screenshots/8c.jpg" width="250">
	<img src="./Media/screenshots/8d.jpg" width="250">
</div>

To have Charlie join their group, Alice or Bob simply need to send him an invitation from their lobby. Again, by accepting an invitation to lobby, you also accept to join a group. Charlie’s lobby session id is updated and the three of them will now share the same lobby between matches.


# C. <a id="C">Oculus Application Configuration</a>

To build and run your own copy of SharedSpaces, you will need to create an application for it on the [Oculus developer dashboard](https://developer.oculus.com/).

## 1. <a id="C1">Application Identifier</a>

<div style="text-align: center; padding: 10pt;">
	<img src="./Media/dashboard/dashboard_app.png"  width="800">
</div>

You Oculus application identfier must be placed in [Assets/Resources/OculusPlatformSettings.asset](Assets/Resources/OculusPlatformSettings.asset).

The identifier (__App ID__) can be found in the _API_ section.

<div style="text-align: center; padding: 10pt;">
	<img src="./Media/dashboard/dashboard_api.png"  width="800">
</div>

## 2. <a id="C2">Destinations</a>

You need to recreate the SharedSpaces destinations in your own application.  Destinations can be found under __Platform Services__.

<div style="text-align: center; padding: 10pt;">
	<img src="./Media/dashboard/dashboard_platform_services.png"  width="800">
</div>

You need to recreate the SharedSpaces destinations in your own application.  Destinations can be found under __Platform Services__.

<div style="text-align: center; padding: 10pt;">
	<img src="./Media/dashboard/dashboard_destinations.png"  width="800">
</div>

SharedSpaces has five destinations: a Lobby, three private rooms (the red, green and blue rooms), and one public room (the purple room).  Here are the settings for each of them.

| API Name | Deeplink Message | Display Name | Description |
| :--- | :--- | :--- | :--- |
| [Lobby](./Media/dashboard/dashboard_destination_lobby.png) | {"is_lobby":"true","map":"Lobby"} | Lobby | The Lobby |
| [RedRoom](./Media/dashboard/dashboard_destination_redroom.png) | {"map":"RedRoom"} | Red Room | The Red Room |
| [GreenRoom](./Media/dashboard/dashboard_destination_greenroom.png) | {"map":"GreenRoom"} | Green Room | The Green Room |
| [BlueRoom](./Media/dashboard/dashboard_destination_blueroom.png) | {"map":"BlueRoom"} | Blue Room | The Blue Room |
| [PurpleRoom](./Media/dashboard/dashboard_destination_purpleroom.png) | {"map":"PurpleRoom","public_room_name":"ThePurpleRoom"} | Purple Room | The Purple room |

In addition to these settings, you need to set __Deeplink Type__ to __Enabled__ and add an image for your destination.  In the case of SharedSpaces, the destination is __Audience__ is set to __Everyone__.

## 3. <a id="D3">Data Use Checkup</a>

You will need to request access to platform data needed by SharedSpaces. Under __Data Use Checkup__, add the following items and submit for certification.

+  User ID
+  User Profile
+  Deep Linking
+  Friends
+  Invites
