using System;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

#if UNITY_EDITOR
#endif

namespace Assets.Scripts.MapEditor
{
    [ExecuteInEditMode]
    public class RoMapChunk : MonoBehaviour
    {
        public RoMapData MapData;
        
        public MeshRenderer MeshRenderer;
        public MeshFilter MeshFilter;
        public MeshCollider Collider;
        public Material Material;
        public Material ShadowMaterial;

        public RectInt ChunkBounds;

        public Mesh Mesh;

        public bool NeedsUVUpdate;

        //private bool needRebuild;
        private float delayToRebuild = 5f;

        public void Initialize(
                RoMapData mapData,
                Material material,
                Material shadowMaterial,
                RectInt bounds,
                float tileSize,
                bool paintEmptyTilesBlack)
            {
                // 修改原因：暫時加入初始化階段診斷訊息，
                // 用來定位第一次 Chunk Mesh 測試發生 NullReferenceException 的實際位置。
                Debug.Log("[RoMapChunk] Initialize 1 - Start");

                ChunkBounds = bounds;
                MapData = mapData;

                Debug.Log($"[RoMapChunk] Initialize 2 - MapData={(MapData != null)}, Atlas={(MapData?.Atlas != null)}, Material={(material != null)}");

                MeshRenderer = gameObject.GetOrAddComponent<MeshRenderer>();
                MeshFilter = gameObject.GetOrAddComponent<MeshFilter>();

                Debug.Log("[RoMapChunk] Initialize 3 - Renderer / Filter ready");

                Material = material;
                ShadowMaterial = material;

                if (!MapData.IsWalkTable)
                {
                    MeshRenderer.shadowCastingMode = ShadowCastingMode.TwoSided;
                }
                else
                {
                    MeshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                    MeshRenderer.receiveShadows = false;
                }

                MeshRenderer.lightProbeUsage = LightProbeUsage.Off;

                Debug.Log("[RoMapChunk] Initialize 4 - Before Collider");

                Collider = gameObject.AddComponent<MeshCollider>();

                MeshRenderer.material = Material;

                Debug.Log("[RoMapChunk] Initialize 5 - Before RebuildMeshData");

                RebuildMeshData(tileSize, paintEmptyTilesBlack);

                Debug.Log("[RoMapChunk] Initialize 6 - After RebuildMeshData");

                RebuildUvs();

                Debug.Log("[RoMapChunk] Initialize 7 - Finished");
            }
        
        public int GetLayerMaskForChunk()
        {
            var layer = 1 << LayerMask.NameToLayer("Ground");
            if (MapData.IsWalkTable)
                layer = 1 << LayerMask.NameToLayer("WalkMap");

            return layer;
        }


        private int GetLayerForChunk()
        {
            var layer = LayerMask.NameToLayer("Ground");
            if (MapData.IsWalkTable)
                layer = LayerMask.NameToLayer("WalkMap");

            return layer;
        }

        public bool GetMeshIntersection(Ray ray, out RaycastHit rayHit, int layerMask)
        {
            if (Physics.Raycast(ray, out var hit, 300f, layerMask))
            {
                rayHit = hit;
                return true;
            }

            rayHit = new RaycastHit() { };
            return false;
        }

        public void UpdateMaterial(Material mat, Material shadow)
        {
            Material = mat;
            MeshRenderer.sharedMaterial = mat;

            ShadowMaterial = shadow;
        }

        public void RebuildUvs()
        {
            if (Mesh == null || MapData.IsWalkTable)
                return;

            try
            {

                Unwrapping.GenerateSecondaryUVSet(Mesh, MeshBuilder.GetUnwrapParam());
                Mesh.UploadMeshData(false);

                //Debug.Log(Mesh.uv2);

                MeshFilter.sharedMesh = Mesh;
                MeshRenderer.material = Material;

                NeedsUVUpdate = false;

                EditorUtility.SetDirty(Mesh);

            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to rebuild UVs on {name} due to exception: {e}" );
            }
        }

        private void RemoveOldChildren()
        {
            while (gameObject.transform.childCount > 0)
                DestroyImmediate(gameObject.transform.GetChild(0).gameObject);
        }

        private void MakeShadowlessChild(Mesh mesh)
        {
            RemoveOldChildren();

            var child = new GameObject("ShadowChild");
            child.transform.parent = gameObject.transform;
            child.transform.localPosition = Vector3.zero;
            child.layer = GetLayerForChunk();

            var mf = child.AddComponent<MeshFilter>();
            var mr = child.AddComponent<MeshRenderer>();
            var collider = child.AddComponent<MeshCollider>();

            mr.material = ShadowMaterial;
            mr.receiveShadows = true;
            mr.scaleInLightmap = 0.2f;
            mr.shadowCastingMode = ShadowCastingMode.TwoSided;

            mf.sharedMesh = mesh;
            collider.sharedMesh = mesh;
        }

        public void RebuildMeshData(float tileSize, bool paintEmptyTilesBlack)
        {
            //var verts = new List<Vector3>();
            //var normals = new List<Vector3>();
            //var uvs = new List<Vector2>();
            //var tris = new List<int>();
            //var colors = new List<Color>();

            gameObject.layer = GetLayerForChunk();
            Material.mainTexture = MapData.Atlas;

            var mesh = new MeshBuilder();
            var shadow = new MeshBuilder();

            var cellData = MapData.GetCellData();

            var sharedData = MapData.SharedMeshData;
            sharedData.RebuildArea(ChunkBounds, tileSize, paintEmptyTilesBlack);

            for (var x = ChunkBounds.xMin; x < ChunkBounds.xMax; x++)
            {
                for (var y = ChunkBounds.yMin; y < ChunkBounds.yMax; y++)
                {
                    var pos = x + y * MapData.Width;
                    if (pos >= cellData.Length)
                        Debug.LogError($"{x} {y} {ChunkBounds} {ChunkBounds.yMax}");
                    var cell = cellData[pos];

                    var x1 = x - ChunkBounds.xMin;
                    var y1 = y - ChunkBounds.yMin;

                    if (true) //always draw top cell, no matter what
                    {
                        var m = mesh;
                        if (paintEmptyTilesBlack && cell.Top.IsUnlit)
                            m = shadow;
                        
                        var offset = Vector3.zero;
                        if (MapData.IsWalkTable)
                            offset += new Vector3(0f, 0.01f, 0f);

                        var tVerts = sharedData.GetTileVertices(new Vector2Int(x, y), transform.position - offset);
                        var tNormals = sharedData.GetTileNormals(new Vector2Int(x, y));// topNormals[x1 + y1 * ChunkBounds.width];
                        var tColors = sharedData.GetTileColors(new Vector2Int(x, y));
                        
                        m.StartTriangle();

                        m.AddVertices(tVerts);
                        m.AddUVs(MapData.TranslateTileTextureUVs(cell.Top));
                        if(!MapData.IsWalkTable)
                            m.AddColors(tColors);
                        m.AddNormals(tNormals);
                        m.AddTriangles(new[] { 0, 1, 3, 3, 2, 0 });
                    }

                    if (cell.Right.Enabled && x + 1 < MapData.Width && !MapData.IsWalkTable)
                    {
                        var m = mesh;
                        if (paintEmptyTilesBlack && cell.Right.IsUnlit)
                            m = shadow;

                        var neighbor = cellData[x + 1 + y * MapData.Width];
                        
                        var r1 = new Vector3((x1 + 1) * tileSize, cell.Heights[1] * RoMapData.YScale, (y1 + 1) * tileSize) ;
                        var r2 = new Vector3((x1 + 1) * tileSize, neighbor.Heights[0] * RoMapData.YScale, (y1 + 1) * tileSize);
                        var r3 = new Vector3((x1 + 1) * tileSize, cell.Heights[3] * RoMapData.YScale, (y1 + 0) * tileSize);
                        var r4 = new Vector3((x1 + 1) * tileSize, neighbor.Heights[2] * RoMapData.YScale, (y1 + 0) * tileSize);

                        var rnormal = VectorHelper.CalcQuadNormal(r1, r2, r3, r4);

                        m.StartTriangle();
                        m.AddVertices(new[] { r1, r2, r3, r4 });
                        m.AddNormals(new[] { rnormal, rnormal, rnormal, rnormal });
                        m.AddUVs(MapData.TranslateTileTextureUVs(cell.Right));
                        if(!MapData.IsWalkTable)
                            m.AddColors(sharedData.GetAveragedRightColors(new Vector2Int(x, y)));
                        m.AddTriangles(new[] { 0, 1, 3, 3, 2, 0 });
                    }

                    if (cell.Front.Enabled && y - 1 >= 0 && !MapData.IsWalkTable)
                    {
                        var m = mesh;
                        if (paintEmptyTilesBlack && cell.Front.IsUnlit)
                            m = shadow;
                        
                        var neighbor = cellData[x + (y - 1) * MapData.Width];

                        var f1 = new Vector3((x1 + 0) * tileSize, cell.Heights[2] * RoMapData.YScale, (y1 + 0) * tileSize);
                        var f2 = new Vector3((x1 + 1) * tileSize, cell.Heights[3] * RoMapData.YScale, (y1 + 0) * tileSize);
                        var f3 = new Vector3((x1 + 0) * tileSize, neighbor.Heights[0] * RoMapData.YScale, (y1 + 0) * tileSize);
                        var f4 = new Vector3((x1 + 1) * tileSize, neighbor.Heights[1] * RoMapData.YScale, (y1 + 0) * tileSize);

                        var fNormal = VectorHelper.CalcQuadNormal(f1, f2, f3, f4);

                        m.StartTriangle();
                        m.AddVertices(new[] { f1, f2, f3, f4 });
                        m.AddNormals(new[] { fNormal, fNormal, fNormal, fNormal });
                        m.AddUVs(MapData.TranslateTileTextureUVs(cell.Front));
                        if(!MapData.IsWalkTable)
                            m.AddColors(sharedData.GetAveragedFrontColors(new Vector2Int(x, y)));
                        m.AddTriangles(new[] { 0, 1, 3, 3, 2, 0 });
                    }
                }
            }

            //if (!mesh.HasMesh())
            //{
            //    Debug.Log($"Chunk {name} is empty.");
            //    return;
            //}


            //needRebuild = true;
            delayToRebuild = Random.Range(4f, 8f);

            if (MapData.IsWalkTable)
                Mesh = mesh.Build(name, false, 2);
            else
                Mesh = mesh.Build(name, false, 1);
            
            if (shadow.HasMesh())
                MakeShadowlessChild(shadow.Build(name + " shadow"));
            else
                RemoveOldChildren();

            MeshFilter.sharedMesh = Mesh;
            Collider.sharedMesh = Mesh;

            // 修改原因：確認 WalkMap Mesh 在 RebuildMeshData 完成當下
            // 是否真的建立成功並指定給 MeshFilter / MeshCollider，
            // 用來定位 Mesh 是生成失敗，還是生成後才遺失。
            Debug.Log(
                $"[RoMapChunk] Mesh assignment debug: " +
                $"Object={gameObject.name}, " +
                $"IsWalkTable={MapData.IsWalkTable}, " +
                $"Layer={gameObject.layer}, " +
                $"MeshNull={(Mesh == null)}, " +
                $"VertexCount={(Mesh != null ? Mesh.vertexCount : -1)}, " +
                $"FilterMeshNull={(MeshFilter.sharedMesh == null)}, " +
                $"ColliderMeshNull={(Collider.sharedMesh == null)}"
            );

            NeedsUVUpdate = true;
        }


    }
}
