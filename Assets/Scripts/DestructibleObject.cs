using UnityEngine;

using Stijn.Prototype.Utility;

using UnityEngine.Rendering;

//TODO Optimize workflow for a destructible object

namespace Stijn.Prototype.Destruction
{
    public class DestructibleObject : MonoBehaviour
    {
        [HideInInspector]
        public float MaximumSize = 1;

        [SerializeField]
        [Tooltip("Don't change at runtime!")]
        private DestructionUtility.SDFResolution _sdfResolution = DestructionUtility.SDFResolution.High;

        private Texture3D _destructionTex;

        public Material DestructibleMaterial;

        [SerializeField]
        private Texture3D _DamageSDF = null;

        [SerializeField]
        private bool _laserCut = true;

        void Start()
        {
            CreateDestructionTexture();
        }

        private void CreateDestructionTexture()
        {
            //_destructionTex = DestructionUtility.CreateTexture((int)_sdfResolution);  // In case its a RGBA format
            _destructionTex = DestructionUtility.CreateTextureRFloat((int)_sdfResolution); // In case its a RFloat format

            UpdateMaterials();
        }

        public void ChangeTextureResolution( DestructionUtility.SDFResolution resolution)
        {
            if( _sdfResolution != resolution)
            {
                _sdfResolution = resolution;

                CreateDestructionTexture();
            }
        }

        public void RaycastAsync( Vector3 hitPoint, Vector3 dir, System.Action<AsyncGPUReadbackRequest> callback)
        {
            DestructionUtility.CheckHitAsync((int)_sdfResolution, hitPoint, dir, this.transform.worldToLocalMatrix, _destructionTex, MaximumSize, callback);
        }

        public bool Raycast(ref Vector3 hitPoint, Vector3 dir)
        {
            return Raycast_CPU(ref hitPoint, dir);
            //return Raycast_GPU(ref hitPoint, dir);
        }

        public bool Raycast_GPU(ref Vector3 hitPoint, Vector3 dir)
        {
            //Matrix4x4 matrix = Matrix4x4.identity;
            //matrix.SetTRS(this.transform.position, this.transform.rotation, this.transform.localScale);
            //matrix = matrix.inverse;
            return DestructionUtility.CheckHit((int)_sdfResolution, ref hitPoint, dir, this.transform.worldToLocalMatrix, _destructionTex, MaximumSize);
        }

        public bool Raycast_CPU(ref Vector3 hitPoint, Vector3 dir)
        {
            bool b = true;
            float minStep = 0.01f;//0.1

            int loopCount = 0;

            while (b)
            {
                Vector3Int pos = GetTexturePosition(hitPoint);

                if (WithinBounds(pos))
                {
                    if(_laserCut)
                    {
                        return true;
                    }
                    else if( CheckForHit(pos))
                    {
                        return true;
                    }
                    else
                    {
                        hitPoint += dir * minStep; // Expand to reading the sdf to move by that value
                        //Debug.DrawLine(hitPoint, hitPoint + dir * minStep, Color.blue, 10);
                    }
                }
                else
                {
                    //Debug.Log("Not within bounds");
                    return false;
                }

                ++loopCount;
                if (loopCount > 1000)//100
                {
                    //Debug.LogWarning("Loop Problem");
                    return false;
                }
            }

            return false;
        }

        private Vector3Int GetTexturePosition(Vector3 pos)
        {
            pos = this.transform.InverseTransformPoint(pos);
            pos /= MaximumSize;
            pos += new Vector3(1, 1, 1);
            pos *= 0.5f;
            pos *= (int)_sdfResolution;
            return new Vector3Int((int)pos.x, (int)pos.y, (int)pos.z);
        }

        private bool WithinBounds(Vector3Int pos)
        {
            if (pos.x >= 0 && pos.y >= 0 && pos.z >= 0)
            {
                int size = (int)_sdfResolution;
                if (pos.x < size && pos.y < size && pos.z < size)
                {
                    return true;
                }
            }
            return false;
        }

        private bool CheckForHit(Vector3Int pos)
        {
            //return _destructionTex.GetPixel(pos.x, pos.y, pos.z).a > 0; // In case its a RGBA format
            return _destructionTex.GetPixel(pos.x, pos.y, pos.z).r > 0; // In case its a RFloat format
        }

        private Vector3 GetTextureSpaceHitpoint(Vector3 hit)
        {
            return this.transform.InverseTransformPoint(hit);
        }

        public void AddDamage(Vector3 hitPoint, Vector3 hitDirection, float radius)
        {
            //ApplyDamageCPU(GetTextureSpaceHitpoint(hitPoint), radius);
            // Pixel buffer RGBA
            //_destructionTex = DestructionUtility.AddDamage((int)_sdfResolution, GetTextureSpaceHitpoint(hitPoint), radius, _destructionTex, MaximumSize);
            // Pixel buffer float
            _destructionTex = DestructionUtility.AddDamageRFloat((int)_sdfResolution, GetTextureSpaceHitpoint(hitPoint), radius, _destructionTex, MaximumSize, hitDirection);
            //_destructionTex = DestructionUtility.AddAdvancedDamageRFloat((int)_sdfResolution, GetTextureSpaceHitpoint(hitPoint), _DamageSDF, radius*2, _destructionTex, MaximumSize);
            UpdateMaterials();
        }

        private void UpdateMaterials()
        {
            DestructibleMaterial.SetTexture("_SDF_Destruction", _destructionTex);
        }

        public void ResetDestructibleObject()
        {
            _destructionTex = DestructionUtility.CreateTexture((int)_sdfResolution);
            UpdateMaterials();
        }

        private void OnDestroy()
        {
            if (_destructionTex != null)
            {
                Destroy(_destructionTex);
            }
        }

        // CPU Test
        private void ApplyDamageCPU(Vector3 hitPoint, float radius)
        {
            int r = (int)_sdfResolution;
            for ( int x = 0; x < r; ++x)
            {
                for (int y = 0; y < r; ++y)
                {
                    for (int z = 0; z < r; ++z)
                    {
                        Vector3 pos = PositionFromVoxelId( new Vector3Int(x,y,z), r, MaximumSize);

                        float dist = Vector3.Distance( pos, hitPoint);

                        dist -= radius;// Signing of the distance

                        Color c = _destructionTex.GetPixel(x, y, z);
                        if ( dist > 0.1)
                        {
                            Vector3 n = Vector3.Normalize(pos - hitPoint);
                            c.r = n.x;
                            c.g = n.y;
                            c.b = n.z;
                        }

                        c.a = Mathf.Min(c.a, dist / MaximumSize);

                        _destructionTex.SetPixel(x, y, z, c);
                    }
                }
            }
            _destructionTex.Apply(false);
        }

        Vector3 PositionFromVoxelId(Vector3Int id, int textureSize, float totalUnitsInTexture)
        {
            Vector3 pos = (Vector3)id;  // 0:textureSize-1
            pos = pos / ((float)textureSize - 1.0f);  // 0:1
            pos = pos * (totalUnitsInTexture * 2);  // 0:meshSize
            pos = pos - (totalUnitsInTexture * Vector3.one);  // -meshExtent:+meshExtent
            return pos;
        }
    }
}
