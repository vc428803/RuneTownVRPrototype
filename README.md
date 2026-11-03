# RuneTown VR Prototype

**Ragnarok Online × Unity 6 × C# × Virtual Reality**

An unofficial, non-commercial Unity VR technical prototype exploring the reconstruction of classic Ragnarok Online environments and the integration of XR interaction, map-data import, scene transitions, monster systems, and AI-assisted NPC dialogue.

**Project Status:** Prototype / Technical Showcase  
**Technologies:** Unity 6, C#, XR Interaction Toolkit, Unity Input System, URP, Unity Editor Scripting

> This repository demonstrates individual technical implementations and development experiments. It is not a complete MMORPG or an officially affiliated Ragnarok Online project.

---

## 🎬 Gameplay Demonstration

### RuneTown VR — Prototype Demo (September 2026)

**[▶ Watch the 57-second Gameplay Demo](./docs/demos/videos/01-rune-town-vr-demo.mp4)**

[Open Video on GitHub](https://github.com/vc428803/RuneTownVRPrototype/blob/main/docs/demos/videos/01-rune-town-vr-demo.mp4)

The recording documents an earlier development-stage build. Features described elsewhere in this README are supported by the codebase and are not necessarily all shown in this single video.

---

## Engineering Highlights

| Area | Implemented Prototype Work | Engineering Focus |
|---|---|---|
| RO Map Reconstruction | GND / GAT / RSW / RSM import workflows | Binary parsing, mesh generation, texture handling |
| XR Navigation & Portals | XR Origin, locomotion, portal triggers, scene transitions | Player persistence, spawn relocation, input recovery |
| Monster Framework | Definitions, spawning, active-monster registry | Data-driven design and lifecycle tracking |
| Monster Editor Tools | Automated creation of monster assets | Unity Editor automation |
| NPC / LLM Integration | NPC profiles, interaction UI, HTTP JSON client | API integration, callbacks and error handling |

## System Architecture & Source Code

### 1. Ragnarok Map Import & Reconstruction

The project investigates how classic RO map resources can be transformed into Unity content, including terrain geometry, walkability information, scene objects and 3D models.

| Format | Responsibility | Status |
|---|---|---|
| GND | Terrain meshes and textures | Prototype implemented |
| GAT | Walkability and height data | Implemented; collision improvements ongoing |
| RSW | World and object placement | Prototype implemented |
| RSM | 3D model resources | Prototype implemented |

Research scenes include **Prontera** and **Glast Heim Churchyard**.

The import pipeline supports terrain reconstruction, texture handling and model placement. Terrain chunk seams, collision handling and import fidelity remain work in progress.

**Source Code**

- [RagnarokMapLoader.cs](./Assets/RuneTown/MapImport/Editor/RagnarokMapLoader.cs) — Map parsing and terrain-related import logic
- [RagnarokModelLoader.cs](./Assets/RuneTown/MapImport/Editor/RagnarokModelLoader.cs) — Model import pipeline
- [RagnarokWalkableDataImporter.cs](./Assets/RuneTown/MapImport/Editor/RagnarokWalkableDataImporter.cs) — Walkability data import

### 2. XR Player & Scene Transitions

The XR prototype integrates player locomotion and interaction with portal-driven scene changes.

The scene transition manager coordinates asynchronous scene loading, target spawn-point selection, player relocation and input-system recovery after transitions.

**Technical Highlights**

- XR Origin and Character Controller integration
- Continuous movement and snap turning
- VR ray interaction and teleportation
- Portal-driven scene transitions
- Persistent player-object handling
- XR Input Action Manager reinitialisation after scene changes

**Source Code**

- [SceneTransitionManager.cs](./Assets/Scripts/Portal/SceneTransitionManager.cs) — Scene loading, spawn relocation and input reinitialisation
- [PortalTrigger.cs](./Assets/Scripts/Portal/PortalTrigger.cs) — Portal activation
- [PersistentPlayer.cs](./Assets/Scripts/Portal/PersistentPlayer.cs) — Player object persistence

Development has primarily been tested using an XR simulator. Physical headset validation remains a future milestone.

### 3. Monster System & Editor Automation

A reusable monster framework supports definitions, spawn configuration, active-monster registration and early detection and animation behaviours.

**Prototype Monsters**

- Poring
- Knight of Abyss
- Dark Lord

The custom Unity Editor tool provides a workflow for creating monster definitions, animations, Animator Controllers, Prefabs and spawn configurations.

This reduces repetitive asset setup when experimenting with additional monster types.

**Source Code**

- [MonsterManager.cs](./Assets/Scripts/Monsters/MonsterManager.cs) — Active-monster registration and counting
- [MonsterSpawner.cs](./Assets/Scripts/Monsters/MonsterSpawner.cs) — Monster spawning workflow
- [MonsterDefinition.cs](./Assets/Scripts/Monsters/MonsterDefinition.cs) — Monster data definitions
- [MonsterCreationWindow.cs](./Assets/Scripts/Editor/MonsterTools/MonsterCreationWindow.cs) — Custom Unity Editor tools

A complete combat loop, including damage, loot and respawn mechanics, is not yet presented as finished functionality.

### 4. NPC Interaction & LLM Integration

An experimental NPC interaction layer combines NPC profiles, VR interaction UI and configurable LLM API communication.

The Unity HTTP client sends JSON requests to an external endpoint and processes dialogue responses or errors through callbacks.

**Technical Highlights**

- NPC profile and dialogue data
- VR interaction interface
- Configurable API endpoint
- JSON request and response handling
- UnityWebRequest-based HTTP communication
- Success and error callbacks
- Mock response mode for development

**Source Code**

- [LLMClient.cs](./Assets/Scripts/LLMClient.cs) — Dialogue API client
- [AiConnectionSettings.cs](./Assets/Scripts/AI/AiConnectionSettings.cs) — Connection configuration
- [NPCProfile.cs](./Assets/Scripts/NPC/NPCProfile.cs) — NPC profile data

NPC-driven quests and a production-ready AI dialogue service remain future development areas.

---

## Technology Stack

| Category | Technologies |
|---|---|
| Game Engine | Unity 6 |
| Programming | C# |
| XR Framework | XR Interaction Toolkit |
| Input | Unity Input System |
| Rendering | Universal Render Pipeline (URP) |
| Map Reconstruction | RO resource parsing, mesh and texture generation |
| Editor Automation | Unity Editor Scripting |
| Gameplay Architecture | Portal, Monster and NPC systems |
| API Integration | UnityWebRequest, JSON, LLM API |

## Current Limitations & Development Roadmap

The following items are planned improvements rather than completed features.

**Map & Rendering**

- Improve GAT walkability and terrain collision
- Resolve terrain chunk seams
- Optimise RSM model and material loading
- Expand RO map coverage
- Improve scene-loading performance

**Combat & Gameplay**

- Player and monster HP
- Damage and attack detection
- Monster death and respawn
- Item drops and inventory integration
- Expanded monster AI

**NPC & Quest Systems**

- Quest Manager
- Quest progression and rewards
- NPC dialogue linked to gameplay state
- AI-assisted quest interactions

**VR Experience**

- VR combat interactions
- Player inventory interface
- Item pickup
- VR comfort and performance optimisation
- Testing on physical XR hardware

Long-term research includes skills, equipment, multiplayer functionality and persistent world state.

---

## Project Scope & Attribution

This project is a **personal, unofficial, non-commercial fan research prototype** intended for technical discussion and portfolio review.

It is not affiliated with or endorsed by Ragnarok Online's rights holders.

Ragnarok Online names, characters, artwork and related assets remain the property of their respective owners.

Public availability of this repository does not imply permission to redistribute or commercially use third-party content. No general open-source licence is granted for this repository.

---

**RuneTown VR Prototype**  
*Reimagining classic Ragnarok Online environments through Unity and Virtual Reality.*
