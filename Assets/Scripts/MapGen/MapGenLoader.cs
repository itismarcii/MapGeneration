using System;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MapGen
{
    public class MapGenLoader : MonoBehaviour
    {
        private TerrainChunk _TerrainPrefab;
        private MeshRenderer _MeshRenderer;
        private TerrainChunk[] _TerrainCache = Array.Empty<TerrainChunk>();

        [Header("Mesh Settings")] 
        [SerializeField] private MapGenerator.ChunkResolution _Resolution;
        [SerializeField, Tooltip("Scales the mesh size and influences the chunk amount. Be careful and dont use to small numbers")] 
        private float _Scale;

        [Header("Generation Settings")]
        [SerializeField] private Image _NoiseImage;
        [SerializeField] private float _HeightModifier = 1;

        /// <summary>
        /// Clears all children inside the MapGenLoader game object
        /// </summary>
        public void ClearTerrain()
        {
            foreach (var terrain in _TerrainCache)
            {
                if(!terrain) continue;
#if UNITY_EDITOR
                DestroyImmediate(terrain.gameObject);
#else
                Destory(terrain.gameObject);
#endif
            }
            
            if(transform.childCount <= 0) return;
            
            foreach (var children in gameObject.GetComponentsInChildren<Transform>())
            {
                if (transform == children) continue;
#if UNITY_EDITOR
                DestroyImmediate(children.gameObject);
#else
                Destory(children.gameObject);
#endif
            }
        }
        
        /// <summary>
        /// Loads and generates a noise field of mesh chunks using the TerrainPrefab
        /// </summary>
        public void Load()
        {
            if(!_NoiseImage) return;

            var scaleMemory = transform.localScale;
            transform.localScale = new Vector3(1, 1, 1);
            
            var meshMatrix = MapGenerator.MeshGeneratorChunk(_NoiseImage.sprite.texture, _Resolution, _HeightModifier,
                out var offsets, _Scale);

            if (!_TerrainPrefab) _TerrainPrefab = Resources.Load<TerrainChunk>("Prefab/TerrainChunk");

            var cols = meshMatrix.GetLength(0);
            var rows = meshMatrix.GetLength(1);

            ClearTerrain();

            _TerrainCache = new TerrainChunk[cols * rows];
            
            for (var i = 0; i < cols; i++)
            {
                for (var j = 0; j < rows; j++)
                {
                    var index = i + j * cols;
                    var obj = Instantiate(_TerrainPrefab, transform);
                    var offset = offsets[index];
                    obj.transform.position += new Vector3(offset.x, 0, offset.y);
                    obj.SetMesh(meshMatrix[i, j]);
                    _TerrainCache[index] = obj;
                }
            }

            transform.localScale = scaleMemory;
        }
    }
    
    #if UNITY_EDITOR

    [CustomEditor(typeof(MapGenLoader))]
    public class MapGenLoaderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var mapGenLoader = (MapGenLoader) target;
            if(!mapGenLoader) return;
            
            base.OnInspectorGUI();
            GUILayout.Space(20);
            
            CreateGenerateButton(mapGenLoader);
            CreateClearButton(mapGenLoader);
        }
        
        private void CreateGenerateButton(in MapGenLoader mapGenLoader)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(new GUIContent("Generate Mesh", "Avoid pressing the button too quickly"), new GUIStyle(GUI.skin.button), GUILayout.Width(170),
                    GUILayout.Height(30)))
            {
                mapGenLoader.Load();
            }
            
            GUILayout.EndHorizontal();
        }

        private void CreateClearButton(in MapGenLoader mapGenLoader)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(new GUIContent("Clear Terrain"), new GUIStyle(GUI.skin.button), GUILayout.Width(170),
                    GUILayout.Height(20)))
            {
                mapGenLoader.ClearTerrain();
            }
            
            GUILayout.EndHorizontal();
        }
    }
    
    #endif
}

