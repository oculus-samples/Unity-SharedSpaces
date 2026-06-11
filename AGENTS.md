# Agent Instructions — SharedSpaces (Unity)

A Unity multiplayer VR showcase demonstrating how to gather players using the Oculus/Meta Social Platform APIs as the presence layer, Photon Realtime as the network transport, and Unity Netcode for GameObjects for replication.

## Source-of-truth files (read these first, do not duplicate their contents in this file)

For setup, build steps, SDK versions, and project layout, read:

- `README.md` — official setup and Photon configuration steps
- `Documentation/Configuration.md`, `Documentation/SharedSpacesOverview.md`, `Documentation/SharedSpacesInAction.md` — deeper architecture and configuration docs
- `ProjectSettings/ProjectVersion.txt` — Unity editor version
- `Packages/manifest.json` — Unity package versions; Photon Realtime is referenced here as `com.mlapi.contrib.transport.photon-realtime`
- `Packages/Photon/` — vendored Photon Voice 2 (from the Unity Asset Store; has its own license)
- `Assets/Photon/Resources/PhotonAppSettings.asset` — where you paste your Photon App ID
- `.gitattributes` — Git LFS configuration
- `LICENSE` — license terms

## Quest / Horizon-specific notes

- Networking has three layered systems (Oculus Social presence + Photon transport + Netcode replication). When debugging "players can't see each other", check each layer independently before assuming a code bug.
- Photon Voice 2 is vendored inside `Packages/Photon/` rather than declared in `manifest.json` — to update it, re-import the Asset Store package and copy it back into `Packages/`.
- `PhotonAppSettings.asset` is checked in without a working App ID; missing/invalid IDs surface as connection errors, not crashes — verify before chasing transport bugs.
- There is a separate Unreal version of this showcase at `oculus-samples/Unreal-SharedSpaces`; do not confuse the two when researching APIs.

## Meta Quest tooling

This repository is part of the Meta Quest / Horizon OS ecosystem (a sample, library, template, or related project — the bespoke intro above describes which). Use that intro and the source-of-truth files it references for project-specific decisions; don't restate or invent facts from memory.

When the user asks anything about Quest device behavior, build / deploy / debug / capture flows, on-device performance, or Horizon OS APIs, reach for these tools instead of generic Unity answers:

- **`hzdb`** — Quest-aware ADB wrapper (device list, install / launch / stop, logs, screenshots, Perfetto traces, on-device docs search). Already wired up as an MCP server via `.mcp.json`, `.vscode/mcp.json`, and `.cursor/mcp.json`. Also runnable directly: `npx -y @meta-quest/hzdb <subcommand>`.
- **Meta Quest Agentic Tools** — the full skill set, including Unity-specific skills: [github.com/meta-quest/agentic-tools](https://github.com/meta-quest/agentic-tools). Install per your client (Claude Code: `/plugin install meta-vr@meta-quest`; Gemini CLI: `gemini extensions install https://github.com/meta-quest/agentic-tools`; Cursor / VS Code: install the **Meta Horizon** extension from the Marketplace).

A few behavior expectations:

- **Read this repo's files first.** Before answering anything project-specific, read `README.md` and whichever source-of-truth files the intro above points at. Don't restate their contents in chat — quote or link instead.
- **Use `hzdb` for device-side work.** Anything that touches an attached Quest (install, launch, logs, screenshot, capture, manifest inspection) goes through `hzdb`, not raw `adb`.
- **Check live Horizon OS docs before answering API questions.** `hzdb docs search "..."` queries the live docs; training data on Horizon OS APIs goes stale fast.
- **Don't fabricate SDK / engine versions.** If a version isn't visible in this repo's files, say so rather than guessing.
