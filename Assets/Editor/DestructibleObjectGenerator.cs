using UnityEngine;
using UnityEditor;

using System.IO;

using Stijn.Prototype.Utility;
using Stijn.Prototype.Destruction;

namespace Stijn.Prototype.Editor
{
    public class DestructibleObjectGenerator : EditorWindow
    {
        public ComputeShader ComputeSDF;

        private Mesh _mesh;
        private DestructionUtility.SDFResolution _sdfResolution = DestructionUtility.SDFResolution.Medium;

        [MenuItem("Prototype/Destructible Object Generator")]
        static void Init() => GetWindow<DestructibleObjectGenerator>("Destruction Object Generator");

        private void OnGUI()
        {
            string errorMessage = string.Empty;
            if (!SystemInfo.supportsComputeShaders)
            {
                errorMessage = "This tool requires a GPU that supports compute shaders.";
            }
            else if (ComputeSDF == null)
            {
                errorMessage = "Please assign the ComputeShader.";
            }

            if ( errorMessage.Length > 0)
            {
                EditorGUILayout.HelpBox(errorMessage, MessageType.Error);

                if (GUILayout.Button("Close"))
                {
                    Close();
                }

                return;
            }

            EditorGUILayout.BeginVertical("box");

            _mesh = EditorGUILayout.ObjectField("Mesh", _mesh, typeof(Mesh), false) as Mesh;
            _sdfResolution = (DestructionUtility.SDFResolution)EditorGUILayout.EnumPopup("SDF Resolution", _sdfResolution);

            using (new EditorGUI.DisabledScope( _mesh == null))
            {
                if (GUILayout.Button("Create Destructible Object"))
                {
                    Generate();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void Generate()
        {
            // Prompt the user to save the file.
            string path = EditorUtility.SaveFilePanelInProject("Save As", _mesh.name + "_Destructible", "", "");
            // In case they hit cancel the saving
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            string parentFolder = Path.GetDirectoryName(path);
            string fileName = Path.GetFileNameWithoutExtension(path);
            string folder = Path.Combine(parentFolder, fileName);

            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder(parentFolder, fileName);
            }

            // Get the Texture3D representation of the SDF
            Texture3D sdf = GetSDF();

            // Save the Texture3D asset at path.
            AssetDatabase.CreateAsset(sdf, Path.Combine(folder, "SDF_" + fileName + ".asset"));

            // Create and save the material
            Material material = new Material(Shader.Find("Stijn/DestructionShader"));
            AssetDatabase.CreateAsset(material, Path.Combine( folder, "MAT_" + fileName + ".mat"));

            // Create and save the prefab
            GameObject go = new GameObject("Destructible Object");
            go.layer = LayerMask.NameToLayer("Destructible");
            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.mesh = _mesh;
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            //mr.material = material;
            mr.sharedMaterial = material;
            MeshCollider mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;//Could also be a low poly version...
            // Add destrictible object component and give all the relavent information
            DestructibleObject destrObj = go.AddComponent<DestructibleObject>();
            destrObj.DestructibleMaterial = material;
            Vector3 extents = mf.sharedMesh.bounds.extents;
            destrObj.MaximumSize = Mathf.Max(extents.x, Mathf.Max(extents.y, extents.z));
            mr.sharedMaterial.SetFloat("_SizeMaximum", destrObj.MaximumSize);
            mr.sharedMaterial.SetTexture("_SDF", sdf);

            PrefabUtility.SaveAsPrefabAsset(go, Path.Combine(folder, fileName + ".prefab"));

            // Save changes
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Close the window
            Close();

            // Cleanup
            DestroyImmediate(go);

            // Select the Prefab in the project view
            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(Path.Combine(folder, fileName + ".prefab"));
        }

        private Texture3D GetSDF()
        {
            // Create the voxel texture and get an array of pixels from it
            int resolution = (int)_sdfResolution;
            Texture3D voxels = new Texture3D(resolution, resolution, resolution, TextureFormat.RHalf, false);//TextureFormat.RHalf TextureFormat.RGBAHalf
            voxels.anisoLevel = 1;
            voxels.filterMode = FilterMode.Bilinear;
            voxels.wrapMode = TextureWrapMode.Clamp;
            Color[] pixelArray = voxels.GetPixels(0);
            ComputeBuffer pixelBuffer = new ComputeBuffer(pixelArray.Length, sizeof(float) * 4);
            pixelBuffer.SetData(pixelArray);

            // Optimizing mesh bound and center
            Vector3 center = _mesh.bounds.center;
            Vector3 size = _mesh.bounds.size;

            // Create the triangle array and buffer from the mesh
            Vector3[] meshVertices = _mesh.vertices;
            int[] meshTriangles = _mesh.GetTriangles(0);//Currently only focus on one submesh, not all of them
            Triangle[] triangleArray = new Triangle[meshTriangles.Length / 3];
            for (int t = 0; t < triangleArray.Length; t++)
            {
                // Offset from the center
                triangleArray[t].a = meshVertices[meshTriangles[3 * t + 0]] - _mesh.bounds.center;
                triangleArray[t].b = meshVertices[meshTriangles[3 * t + 1]] - _mesh.bounds.center;
                triangleArray[t].c = meshVertices[meshTriangles[3 * t + 2]] - _mesh.bounds.center;
            }
            ComputeBuffer triangleBuffer = new ComputeBuffer(triangleArray.Length, sizeof(float) * 3 * 3);
            triangleBuffer.SetData(triangleArray);

            // Instantiate the compute shader from resources.
            ComputeShader compute = (ComputeShader)Instantiate(ComputeSDF);
            int kernel = compute.FindKernel("CSMain");

            // Upload the pixel buffer to the GPU.
            compute.SetBuffer(kernel, "pixelBuffer", pixelBuffer);
            compute.SetInt("pixelBufferSize", pixelArray.Length);

            // Upload the triangle buffer to the GPU.
            compute.SetBuffer(kernel, "triangleBuffer", triangleBuffer);
            compute.SetInt("triangleBufferSize", triangleArray.Length);

            // Calculate and upload the other necessary parameters.
            float maxMeshSize = Mathf.Max(Mathf.Max(_mesh.bounds.size.x, _mesh.bounds.size.y), _mesh.bounds.size.z);
            float totalUnitsInTexture = maxMeshSize;
            compute.SetInt("textureSize", resolution);
            compute.SetFloat("totalUnitsInTexture", totalUnitsInTexture);
            compute.SetInt("useIntersectionCounter", 1);

            // Compute the SDF.
            compute.Dispatch(kernel, pixelArray.Length / 256 + 1, 1, 1);

            // Destroy the compute shader and release the triangle buffer.
            DestroyImmediate(compute);
            triangleBuffer.Release();

            // Retrieve the pixel buffer and reapply it to the voxels texture.
            pixelBuffer.GetData(pixelArray);
            pixelBuffer.Release();
            voxels.SetPixels(pixelArray, 0);
            voxels.Apply();

            // Return the voxels texture.
            return voxels;
        }

        private struct Triangle
        {
            public Vector3 a;
            public Vector3 b;
            public Vector3 c;
        }
    }
}
