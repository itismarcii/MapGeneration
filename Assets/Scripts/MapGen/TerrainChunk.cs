using UnityEngine;

namespace MapGen
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class TerrainChunk : MonoBehaviour
    {
        private MeshFilter _MeshFilter;
        private bool d_MeshFilter = true;

        public MeshFilter Filter
        {
            get
            {
                if (!d_MeshFilter) return _MeshFilter;
            
                _MeshFilter = GetComponent<MeshFilter>();
                d_MeshFilter = false;
                return _MeshFilter;
            }
        }

        public void SetMesh(in Mesh mesh) => Filter.mesh = mesh;
    }
}
