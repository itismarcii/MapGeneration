using System;
using Extra;
using UnityEngine;

namespace MapGen
{
    public static class MapGenerator
    {
        [Serializable]
        public enum ChunkResolution : uint
        {
            _16x16 = 16,
            _32x32 = 32,
            _64x64 = 64,
            _128x128 = 128,
            _256x256 = 256
        }
        
        private const uint MESH_SIZE_THRESHOLD = 65536;
        
        private static ComputeShader _MapGenShader;

        private static int _MeshGenKernelIndex = -1, _MeshGenChunkKernelIndex = -1, _TriangleSetupKernelIndex = -1;
        
        private static readonly int 
            NoiseTextureID = Shader.PropertyToID("HeightTexture"),
            WidthID = Shader.PropertyToID("width"),
            HeightID = Shader.PropertyToID("height"),
            ScaleMultiplierID = Shader.PropertyToID("scale_multiplier"),
            HeightMultiplierID = Shader.PropertyToID("height_multiplier"),
            VertexResultID = Shader.PropertyToID("VertexResult"),
            TriangleResultID = Shader.PropertyToID("TriangleResult"),
            OffsetID = Shader.PropertyToID("offset");
        
        /// <summary>
        /// Generate a mesh from the noise information of the provided noise texture.
        /// </summary>
        /// <param name="noiseTexture">Noise texture which is used to generate the map generation mesh</param>
        /// <param name="heightMultiplier">Height modifier</param>
        /// <returns>Returns a new mesh of the generated noise mesh</returns>
        public static Mesh MeshGenerator(in Texture2D noiseTexture, in float heightMultiplier)
        { 
            _MapGenShader = _MapGenShader ? _MapGenShader : Resources.Load<ComputeShader>("Shader/MapGenerator");
            _MeshGenKernelIndex =
                _MeshGenKernelIndex > -1 ? _MeshGenKernelIndex : _MapGenShader.FindKernel("MapGenerator");

            
            var renderTexture = TextureHelper.GetRenderTexture(noiseTexture);

            var width = noiseTexture.width;
            var height = noiseTexture.height ;
            var count = width * height;

            if (count > MESH_SIZE_THRESHOLD)
            {
                Debug.LogWarning("Texture resolution is to high, use MeshGeneratorChunk instead.");
            }
            
            var vertexBuffer = new ComputeBuffer(count, sizeof(float) * 3);
            var triangleBuffer = new ComputeBuffer( count * 6, sizeof(int));

            _MapGenShader.SetBuffer(_MeshGenKernelIndex, VertexResultID, vertexBuffer);
            _MapGenShader.SetBuffer(_MeshGenKernelIndex, TriangleResultID, triangleBuffer);
            _MapGenShader.SetTexture(_MeshGenKernelIndex, NoiseTextureID, renderTexture);

            _MapGenShader.SetInt(WidthID, width);
            _MapGenShader.SetInt(HeightID, height);
            _MapGenShader.SetFloat(HeightMultiplierID, heightMultiplier);

            _MapGenShader.Dispatch(_MeshGenKernelIndex, width, height, 1);
            
            var mesh = new Mesh();
            var vertices = new Vector3[count];
            var triangles = new int[triangleBuffer.count];
            
            vertexBuffer.GetData(vertices);
            triangleBuffer.GetData(triangles);

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            vertexBuffer.Release();
            triangleBuffer.Release();
            renderTexture.Release();
            
            return mesh;
        }

        /// <summary>
        /// Generate a mesh matrix from the noise information of the provided noise texture.
        /// </summary>
        /// <param name="noiseTexture">Noise texture which is used to generate the map generation mesh</param>
        /// <param name="chunkResolution">Chunk resolution in which the mesh matrix is subdivided</param>
        /// <param name="heightMultiplier">Height modifier</param>
        /// <param name="offsets">Offset parameter output to align the mesh matrix in a more simplified way</param>
        /// <param name="scaling">Scaling of the mesh chunk inside the noise texture. Altering this will scale down the mesh size and increase or decrease the amount of chunk pieces.</param>
        /// <returns>Returns a new mesh matrix of the generated noise texture</returns>
        public static Mesh[,] MeshGeneratorChunk(in Texture2D noiseTexture, in ChunkResolution chunkResolution, in float heightMultiplier, out Vector2[] offsets, in float scaling = 1)
        {
            _MapGenShader = _MapGenShader ? _MapGenShader : Resources.Load<ComputeShader>("Shader/MapGenerator");
            
            _MeshGenChunkKernelIndex = _MeshGenChunkKernelIndex > -1
                ? _MeshGenChunkKernelIndex
                : _MapGenShader.FindKernel("MapGeneratorChunk");
            _TriangleSetupKernelIndex = _TriangleSetupKernelIndex > -1 
                ? _TriangleSetupKernelIndex 
                : _MapGenShader.FindKernel("TriangleSetup");
            
            var renderTexture = TextureHelper.GetRenderTexture(noiseTexture);

            var textureWidth = noiseTexture.width;
            var textureHeight = noiseTexture.height;
            var chunkDimension = (int) chunkResolution;
            
            var count = chunkDimension * chunkDimension;

            var vertexBuffer = new ComputeBuffer(count, sizeof(float) * 3);
            var triangleBuffer = new ComputeBuffer(count * 6, sizeof(int));
            
            var cols = textureWidth <= chunkDimension ? 1 : (int) (textureWidth / chunkDimension);
            var rows = textureHeight <= chunkDimension ? 1 : (int) (textureHeight / chunkDimension);

            cols = (int) (cols / scaling);
            rows = (int) (rows / scaling);
            
            var scale = new Vector2(
                Mathf.Min(1, Mathf.Clamp(textureWidth / (float) chunkDimension, 0.0f, 1.0f)),
                Mathf.Min(1, Mathf.Clamp(textureHeight / (float) chunkDimension, 0.0f, 1.0f)));
            
            var meshMatrix = new Mesh[cols, rows];
            offsets = new Vector2[cols * rows];

            _MapGenShader.SetBuffer(_MeshGenChunkKernelIndex, VertexResultID, vertexBuffer);
            _MapGenShader.SetBuffer(_TriangleSetupKernelIndex, TriangleResultID, triangleBuffer);
            _MapGenShader.SetTexture(_MeshGenChunkKernelIndex, NoiseTextureID, renderTexture);

            _MapGenShader.SetInt(WidthID, chunkDimension);
            _MapGenShader.SetInt(HeightID, chunkDimension);
            _MapGenShader.SetVector(ScaleMultiplierID, scale * scaling);
            _MapGenShader.SetFloat(HeightMultiplierID, heightMultiplier);
            _MapGenShader.Dispatch(_TriangleSetupKernelIndex, chunkDimension, chunkDimension, 1);

            var vertices = new Vector3[count];
            var triangles = new int[triangleBuffer.count];
            triangleBuffer.GetData(triangles);
            
            for (var i = 0; i < cols; i++)
            {
                for (var j = 0; j < rows; j++)
                {
                    var offset = new Vector2(i * (chunkDimension - 1), j * (chunkDimension - 1));

                    _MapGenShader.SetVector(OffsetID, offset);
                    _MapGenShader.Dispatch(_MeshGenChunkKernelIndex, chunkDimension, chunkDimension, 1);
                    
                    vertexBuffer.GetData(vertices);
                    
                    var mesh = new Mesh
                    {
                        vertices = vertices,
                        triangles = triangles
                    };
                    
                    mesh.RecalculateNormals();
                    mesh.RecalculateBounds();

                    meshMatrix[i, j] = mesh;
                    offsets[i + j * cols] = new Vector2(offset.x * scaling, offset.y * scaling);
                }
            }

            vertexBuffer.Release();
            triangleBuffer.Release();
            renderTexture.Release();
            
            return meshMatrix;
        }
    }
}
