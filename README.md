![Showcase Banner](./Documentation/Media/banner.png "SharedSpaces")

# SharedSpaces

SharedSpaces, developed by the VR Developer Tools team, showcases how to quickly gather people in VR using Oculus Social Platform APIs. This version is built for the Unity engine, utilizing [Photon Realtime](https://github.com/Unity-Technologies/multiplayer-community-contributions/tree/main/Transports/com.community.netcode.transport.photon-realtime) as the transport layer and [Unity Netcode for GameObjects](https://github.com/Unity-Technologies/com.unity.netcode.gameobjects). There is also a version for the [Unreal Engine](https://github.com/oculus-samples/Unreal-SharedSpaces).

This codebase serves as both a reference and a template for multiplayer VR games.

## Project Description

SharedSpaces networking consists of three layers. The Oculus layer provides presence information to find and connect with friends. The Photon layer handles message transport between players. The Netcode for GameObjects layer manages game object replication.

This overview explores each layer and demonstrates how they integrate to create a simple multiplayer application, allowing people to connect and play without a dedicated server.

## How to Run the Project in Unity

First, ensure Git LFS is installed by running:
```sh
git lfs install
```

Next, clone this repository using the "Code" button above or this command:
```sh
git clone https://github.com/oculus-samples/Unity-SharedSpaces.git
```

To run the showcase, open the project folder in *Unity 6000.0.59f2* or newer. Load the [Assets/SharedSpaces/Scenes/Startup](Assets/SharedSpaces/Scenes/Startup.unity) scene.

Upon loading the scene, you may see this pop-up:

<div style="text-align: center; padding: 10pt;"><img src="./Documentation/Media/tmp_essentials.png" width="650"></div>

Click "Import TMP Essentials" to import the necessary TextMesh Pro assets.

### Setting up Photon

To get the sample working, configure the NetDriver with your Photon account. Their basic plan is free.
- Visit [photonengine.com](https://www.photonengine.com) and [create an account](https://doc.photonengine.com/realtime/current/getting-started/obtain-your-app-id).
- From your Photon dashboard, click "Create A New App."
- Fill out the form, setting the type to "Photon Realtime," then click Create.

Your new app will appear on your Photon dashboard. Click the App ID to reveal the full string and copy it.

Paste your App ID in [Assets/Photon/Resources/PhotonAppSettings](Assets/Photon/Resources/PhotonAppSettings.asset).

The Photon Realtime transport should now work. Verify network traffic on your Photon account dashboard.

## Dependencies

### Where are the Meta and Photon packages?

The Meta packages are referenced in the [Packages/manifest.json](./Packages/manifest.json) file; update them using the Package Manager within the Unity Editor.

To keep the project organized, the [Photon Voice 2](https://assetstore.unity.com/packages/tools/audio/photon-voice-2-130518) package is stored in the [Packages](./Packages) folder. To update, import their updated Asset Store packages, then copy them into their respective `Packages` folders.

The *Photon Voice 2* package is released under the *[License Agreement for Exit Games Photon](./Packages/Photon/Photon/license.txt)*.

The Photon Realtime package is referenced in [Packages/manifest.json](./Packages/manifest.json) as `com.mlapi.contrib.transport.photon-realtime`.

## Getting the Code

First, ensure Git LFS is installed by running:
```sh
git lfs install
```

Then, clone this repository using the "Code" button above or this command:
```sh
git clone https://github.com/oculus-samples/Unity-SharedSpaces.git
```

## Documentation

- [Configuration](./Documentation/Configuration.md)
- [Shared Spaces Overview](./Documentation/SharedSpacesOverview.md)
- [Shared Spaces In Action](./Documentation/SharedSpacesInAction.md)

## License

The majority of Shared Spaces is licensed under [MIT LICENSE](./LICENSE), however files from [Photon SDK](./Packages/Photon/Photon/license.txt) are licensed under their respective licensing terms.

## Contribution

See the [CONTRIBUTING](CONTRIBUTING.md) file for how to help out.

## AI coding agents

This repo is wired up for AI coding agents — `AGENTS.md`, `.vscode/extensions.json`, `.mcp.json`, `.cursor/rules/`, and a few client-specific dotfiles surface the **Meta Horizon** VS Code/Cursor extension, the `hzdb` MCP server, and the Meta Quest skill set automatically.

Full toolchain, including Meta Quest skills and per-client install instructions: [github.com/meta-quest/agentic-tools](https://github.com/meta-quest/agentic-tools).
