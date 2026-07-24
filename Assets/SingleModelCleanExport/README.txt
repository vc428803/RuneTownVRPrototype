Single Model Clean Export
=========================

Source Model: 프론테라\성_기둥.rsm

包含：
- Clean Prefab
- Mesh Assets
- 實際使用的 Texture

不包含：
- Reference Scripts
- Reference Shader
- Packages
- Addressables

匯入 URP 專案後，請建立 URP/Lit 或 URP/Unlit Material，
將 Textures 資料夾中的貼圖指定為 Base Map，
再套用到 Prefab 裡的 Mesh Renderer。
