RuneTown VR Prototype

Ragnarok Online × Unity 6 × Virtual Reality

«以《仙境傳說》（Ragnarok Online）為基礎的非官方 VR 技術研究專案，嘗試將經典 RO 世界重建於 Unity 3D 環境，探索 VR 互動、地圖解析與 RPG 遊戲系統的整合。»

專案狀態： Prototype（原型開發階段）
主要技術： Unity 6、C#、XR Interaction Toolkit、URP

---

🎮 專案簡介

RuneTown VR 最初從 VR 城鎮探索開始，逐步擴充為包含 RO 地圖重建、魔物管理、NPC AI 對話及跨場景傳送 的遊戲原型。

目前主要目標是建立可重複使用的遊戲系統架構，並研究經典 RO 地圖資料與現代 VR 技術的整合方式。

本專案尚非完整可遊玩的 MMORPG。

---

✅ 已完成的功能

1. RO 地圖解析與場景重建

已建立 RO 原始地圖資料匯入流程，將遊戲資料轉換為 Unity 可使用的 3D 場景。

格式| 功能| 狀態
GND| 地形、Mesh、貼圖生成| 已完成原型
GAT| 行走區域、高度及移動資料| 持續優化
RSW| 場景物件配置、光源| 已完成原型
RSM| 3D 模型匯入與資源重用| 已完成原型

已建立的 RO 場景：

- Prontera（普隆德拉）
- Glast Heim Churchyard（古城墓地）

目前支援地形 Chunk 生成、Texture Atlas、模型配置及場景資料載入。

GAT 行走限制、複雜地形碰撞及地圖接縫仍在持續改善。

2. VR 玩家與傳送系統

已建立基礎 XR 玩家架構，包含：

- XR Origin、Continuous Movement、Snap Turn
- VR Ray Interaction 與 Teleportation
- Character Controller
- 雙向 Portal 傳送
- 跨場景切換與地圖資料重新載入

目前主要透過 Unity XR Simulator 進行開發測試。

3. RO 魔物系統

已建立共用魔物架構，支援魔物生成、玩家偵測、動畫及生命週期管理。

已實作的魔物原型：

- Poring（波利）
- Knight of Abyss（深淵騎士）
- Dark Lord（黑暗之王）

核心功能：

- Monster Definition / Monster Manager
- Monster Spawner / Active Monster Tracking
- 玩家偵測與追蹤
- Sprite 動畫及 Animator
- 魔物數量統計與註冊管理

另外開發了 Monster Editor Tools，可自動建立魔物 Prefab、動畫、Animator 及相關資料結構，減少新增魔物的重複工作。

4. NPC 與 AI 對話

已建立 NPC 互動及 LLM 整合原型：

- NPC Profile 與對話狀態管理
- VR Ray 對話選項操作
- NPC 對話 UI
- 本地 LLM API 串接
- 雲端 AI 連線設定與驗證

後續將研究 NPC 對話與任務系統的整合。

---

🏗️ 系統架構

目前專案主要分為五個功能模組：

系統模組| 核心功能
XR Player System| 玩家移動、VR 射線互動、傳送
Ragnarok Map System| GND / GAT / RSW / RSM 解析與地圖載入
World System| Prontera、Glast Heim、Portal、場景切換
Monster System| 魔物生成、管理、動畫、偵測及 Editor Tools
NPC System| NPC Profile、對話 UI、LLM API 整合

整體架構方向：

1. 資料層： 解析 RO 地圖、模型及遊戲資料。
2. 場景層： 將資料轉換為 Unity 地形、模型與可探索場景。
3. 互動層： 整合 VR 玩家移動、NPC 對話及魔物互動。
4. 遊戲邏輯層： 管理魔物、傳送及未來的任務與戰鬥流程。

以上為功能架構示意，非實際資料夾結構。

---

🚀 下一階段開發計畫

以下為尚未完成的功能，將依優先順序逐步實作。

Phase 1｜地圖系統完善

目標：提升 RO 地圖匯入及 VR 探索的穩定性。

- [ ] 改善 GAT 行走判定與地形碰撞
- [ ] 修正 Terrain Chunk 接縫
- [ ] 優化 RSM 模型及材質載入
- [ ] 擴充更多 RO 地圖
- [ ] 改善跨場景載入效能

Phase 2｜魔物戰鬥系統

目標：建立完整的基礎戰鬥循環。

- [ ] Player / Monster HP
- [ ] 攻擊與傷害判定
- [ ] 魔物死亡及重生
- [ ] 掉落物品系統
- [ ] 魔物 AI 行為擴充

Phase 3｜NPC 與任務系統

目標：讓 NPC 對話與遊戲任務產生實際關聯。

- [ ] Quest Manager
- [ ] 任務接取與進度追蹤
- [ ] 擊殺魔物任務
- [ ] NPC 任務獎勵
- [ ] AI NPC 對話與任務整合

Phase 4｜VR 遊戲體驗

目標：完善玩家在 VR 世界中的操作體驗。

- [ ] VR 戰鬥操作
- [ ] 玩家狀態與背包介面
- [ ] VR 物品拾取
- [ ] VR 效能及移動舒適度優化
- [ ] 實體 VR 裝置測試

長期研究方向： 技能系統、裝備系統、多人連線及世界狀態管理。

---

🛠️ 使用技術

技術| 用途
Unity 6 / C#| 遊戲邏輯與系統開發
XR Interaction Toolkit| VR 玩家互動
Unity Input System| 輸入控制
URP| 3D 場景渲染
Unity Editor Scripting| 地圖與魔物工具
LLM API| NPC AI 對話

---

🤝 開發者交流與合作

本專案目前以個人研究與原型開發為主，歡迎對以下技術領域有興趣的開發者交流：

Unity / C#、XR / VR、RO 地圖解析、3D Rendering、Monster AI、NPC LLM Integration。

希望透過技術交流與合作，逐步完善 RO 世界重建與 VR 遊戲系統。

歡迎提出 Issue、技術建議或 Pull Request。

---

⚠️ 專案聲明

本專案為非官方、非商業性的粉絲創作及技術研究原型，與 Ragnarok Online 官方無合作或授權關係。

Ragnarok Online 相關名稱、角色、美術及遊戲素材的權利歸其各自權利人所有。

本 Repository 公開供技術交流與研究參考，不代表第三方素材已取得重新散布或商業使用授權。目前尚未宣告正式的開源授權條款。

---

RuneTown VR Prototype
Reimagining Ragnarok Online through Unity and Virtual Reality.
