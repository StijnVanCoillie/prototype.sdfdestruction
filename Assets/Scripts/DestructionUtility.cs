using UnityEngine;

using UnityEngine.Rendering;

namespace Stijn.Prototype.Utility
{
    // TODO: Use nameId integers instead of strings
    public static class DestructionUtility
    {
        private static ComputeShader _createCS;
        public static ComputeShader CreateComputeShader
        {
            get
            {
                if(_createCS == null)
                {
                    _createCS = (ComputeShader)Resources.Load("CreateEmptySDF");
                }
                return _createCS;
            }
            private set { _createCS = value; }
        }

        private static ComputeShader _damageCS;
        public static ComputeShader DamageComputeShader
        {
            get
            {
                if (_damageCS == null)
                {
                    _damageCS = (ComputeShader)Resources.Load("ApplyDamage");
                }
                return _damageCS;
            }
            private set { _damageCS = value; }
        }

        private static ComputeShader _damageAdvancedCS;
        public static ComputeShader DamageAdvancedComputeShader
        {
            get
            {
                if (_damageAdvancedCS == null)
                {
                    _damageAdvancedCS = (ComputeShader)Resources.Load("ApplyDamageAdvanced");
                }
                return _damageAdvancedCS;
            }
            private set { _damageAdvancedCS = value; }
        }

        private static ComputeShader _sphereTracingCS;
        public static ComputeShader SphereTracingComputeShader
        {
            get
            {
                if (_sphereTracingCS == null)
                {
                    _sphereTracingCS = (ComputeShader)Resources.Load("SphereTracing");
                }
                return _sphereTracingCS;
            }
            private set { _sphereTracingCS = value; }
        }

        private static bool _alreadyDefined = false;

        public static Texture3D CreateTexture(int resolution)
        {
            // Create the voxel texture and get an array of pixels from it
            //Texture3D tex = new Texture3D(resolution, resolution, resolution, TextureFormat.RGBAHalf, false); // In case its a RGBA format and we collect the normal
            //Texture3D tex = new Texture3D(resolution, resolution, resolution, TextureFormat.RHalf, false); // In case its a RHalf format, we only collect the signed distance
            Texture3D tex = new Texture3D(resolution, resolution, resolution, TextureFormat.RFloat, false); // In case its a RHalf format, we only collect the signed distance

            tex.anisoLevel = 1;
            tex.filterMode = FilterMode.Trilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            //_destructionTex.hideFlags = HideFlags.HideAndDontSave;
            Color[] pixelArray = tex.GetPixels(0);
            ComputeBuffer pixelBuffer = new ComputeBuffer(pixelArray.Length, sizeof(float) * 4);
            pixelBuffer.SetData(pixelArray);

            // Instantiate the compute shader.
            int kernel = CreateComputeShader.FindKernel("CSMain");

            // Upload the pixel buffer to the GPU.
            CreateComputeShader.SetBuffer(kernel, "pixelBuffer", pixelBuffer);

            // Compute the SDF.
            CreateComputeShader.Dispatch(kernel, pixelArray.Length / 256 + 1, 1, 1);

            // Retrieve the pixel buffer and reapply it to the voxels texture.
            pixelBuffer.GetData(pixelArray);
            pixelBuffer.Release();
            tex.SetPixels(pixelArray, 0);
            tex.Apply();
            return tex;
        }

        public static Texture3D AddDamage(int resolution, Vector3 hitPoint, float radius, Texture3D tex, float size)
        {
            // Create an array of pixels from the current destruction texture
            Color[] pixelArray = tex.GetPixels(0);
            ComputeBuffer pixelBuffer = new ComputeBuffer(pixelArray.Length, sizeof(float) * 4);
            pixelBuffer.SetData(pixelArray);

            // Instantiate the compute shader.
            int kernel = DamageComputeShader.FindKernel("CSMain");

            // Upload the pixel buffer to the GPU.
            DamageComputeShader.SetBuffer(kernel, "pixelBuffer", pixelBuffer);
            DamageComputeShader.SetInt("pixelBufferSize", pixelArray.Length);

            DamageComputeShader.SetInt("textureSize", resolution);
            DamageComputeShader.SetFloat("totalUnitsInTexture", size);

            //Set damage info
            DamageComputeShader.SetFloat("radiusDamage", radius);
            //DamageComputeShader.SetVector("posDamage", GetTextureSpaceHitpoint(hitPoint));
            DamageComputeShader.SetVector("posDamage", hitPoint);

            // Compute the SDF.
            DamageComputeShader.Dispatch(kernel, pixelArray.Length / 256 + 1, 1, 1);
            //Vector3Int threadGroups = GetThreadGroups(pixelArray.Length);
            //DamageComputeShader.Dispatch(kernel, threadGroups.x, threadGroups.y, threadGroups.z);

            // Retrieve the pixel buffer and reapply it to the voxels texture.
            pixelBuffer.GetData(pixelArray);
            pixelBuffer.Release();
            tex.SetPixels(pixelArray, 0);
            tex.Apply();

            return tex;
        }

        public static Texture3D CreateTextureRFloat(int resolution)
        {
            // Create the voxel texture and get an array of pixels from it
            Texture3D tex = new Texture3D(resolution, resolution, resolution, TextureFormat.RFloat, false); // In case its a RHalf format, we only collect the signed distance

            tex.anisoLevel = 1;
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            //_destructionTex.hideFlags = HideFlags.HideAndDontSave;
            // Create an array of pixels from the current destruction texture
            Unity.Collections.NativeArray<float> floatArray = tex.GetPixelData<float>(0);
            float[] pixelArray = floatArray.ToArray();
            ComputeBuffer pixelBuffer = new ComputeBuffer(pixelArray.Length, sizeof(float));
            pixelBuffer.SetData(pixelArray);

            // Instantiate the compute shader.
            int kernel = CreateComputeShader.FindKernel("CSMain");

            // Upload the pixel buffer to the GPU.
            CreateComputeShader.SetBuffer(kernel, "pixelBuffer", pixelBuffer);

            // Compute the SDF.
            CreateComputeShader.Dispatch(kernel, pixelArray.Length / 256 + 1, 1, 1);

            // Retrieve the pixel buffer and reapply it to the voxels texture.
            pixelBuffer.GetData(pixelArray);
            pixelBuffer.Release();
            tex.SetPixelData(pixelArray, 0);
            tex.Apply();
            return tex;
        }

        public static Texture3D AddDamageRFloat(int resolution, Vector3 hitPoint, float radius, Texture3D tex, float size, Vector3 hitDirection)
        {
            // Create an array of pixels from the current destruction texture
            Unity.Collections.NativeArray<float> floatArray = tex.GetPixelData<float>(0);
            float[] pixelArray = floatArray.ToArray();
            ComputeBuffer pixelBuffer = new ComputeBuffer(pixelArray.Length, sizeof(float));
            pixelBuffer.SetData(pixelArray);

            // Instantiate the compute shader.
            int kernel = DamageComputeShader.FindKernel("CSMain");

            // Upload the pixel buffer to the GPU.
            DamageComputeShader.SetBuffer(kernel, "pixelBuffer", pixelBuffer);
            //DamageComputeShader.SetInt("pixelBufferSize", pixelArray.Length);

            // Upload the other necessary parameters.
            DamageComputeShader.SetInt("textureSize", resolution);//resolution //tex.width
            DamageComputeShader.SetFloat("totalUnitsInTexture", size);

            //Set damage info
            DamageComputeShader.SetFloat("radiusDamage", radius);
            DamageComputeShader.SetVector("posDamage", hitPoint);
            DamageComputeShader.SetVector("dirDamage", hitDirection);

            DamageComputeShader.SetFloat("randomUnit", Random.Range(0f, Mathf.PI * 2));

            // Compute the SDF.
            DamageComputeShader.Dispatch(kernel, pixelArray.Length / 256 + 1, 1, 1);
            //Vector3Int threadGroups = GetThreadGroups(pixelArray.Length);
            //DamageComputeShader.Dispatch(kernel, threadGroups.x, threadGroups.y, threadGroups.z);

            // Retrieve the pixel buffer and reapply it to the voxels texture.
            pixelBuffer.GetData(pixelArray);
            pixelBuffer.Release();
            tex.SetPixelData(pixelArray, 0);
            tex.Apply();

            return tex;
        }

        public static Texture3D AddAdvancedDamageRFloat(int resolution, Vector3 hitPoint, Texture3D damageTex, float damageSize, Texture3D tex, float size)
        {
            Unity.Collections.NativeArray<float> floatArray = tex.GetPixelData<float>(0);//float
            float[] pixelArray = floatArray.ToArray();
            ComputeBuffer pixelBuffer = new ComputeBuffer(pixelArray.Length, sizeof(float));
            pixelBuffer.SetData(pixelArray);

            // Instantiate the compute shader.
            int kernel = DamageAdvancedComputeShader.FindKernel("CSMain");

            // Upload the pixel buffer to the GPU.
            DamageAdvancedComputeShader.SetBuffer(kernel, "pixelBuffer", pixelBuffer);
            DamageAdvancedComputeShader.SetInt("pixelBufferSize", pixelArray.Length);

            DamageAdvancedComputeShader.SetInt("textureSize", resolution);
            DamageAdvancedComputeShader.SetFloat("totalUnitsInTexture", size);

            //Set damage info
            Vector3 min = hitPoint - Vector3.one * (damageSize * 0.5f);
            Vector3 max = hitPoint + Vector3.one * (damageSize * 0.5f);
            DamageAdvancedComputeShader.SetTexture(kernel, "textureDamage", damageTex);
            DamageAdvancedComputeShader.SetInt("textureDamageSize", damageTex.width);
            DamageAdvancedComputeShader.SetFloat("unitsInDamage", damageSize);
            DamageAdvancedComputeShader.SetFloat("maximumDamage", 0.6098416f );//2.99273f
            DamageAdvancedComputeShader.SetVector("boundingBoxPosition", hitPoint);
            DamageAdvancedComputeShader.SetVector("boundingBoxMin", min);
            DamageAdvancedComputeShader.SetVector("boundingBoxMax", max);

            // Compute the SDF.
            DamageAdvancedComputeShader.Dispatch(kernel, pixelArray.Length / 256 + 1, 1, 1); // OLD SCHOOL

            // Retrieve the pixel buffer and reapply it to the voxels texture.
            pixelBuffer.GetData(pixelArray);
            pixelBuffer.Release();
            tex.SetPixelData(pixelArray, 0);
            tex.Apply();

            return tex;
        }

        public static void CheckHitAsync(int resolution, Vector3 hitPoint, Vector3 direction, Matrix4x4 matrix, RenderTargetIdentifier rt, float size, System.Action<AsyncGPUReadbackRequest> callback)
        {
            CommandBuffer cb = new CommandBuffer();
            //cb.SetExecutionFlags(CommandBufferExecutionFlags.AsyncCompute);

            int kernel = SphereTracingComputeShader.FindKernel("CSMain");

            // Set buffer
            ComputeBuffer hitBuffer = new ComputeBuffer(1, sizeof(float) * 4);
            Vector4[] hitInfo = new Vector4[] { new Vector4(0, 0, 0, 0) };
            hitBuffer.SetData(hitInfo);
            cb.SetComputeBufferParam( SphereTracingComputeShader, kernel, "hitInfo", hitBuffer);

            // Callback
            //cb.RequestAsyncReadback(hitBuffer, callback);
            //cb.WaitAllAsyncReadbackRequests();
            //cb.RequestAsyncReadback(hitBuffer, callback);

            //Set sphere tracing info
            cb.SetComputeTextureParam(SphereTracingComputeShader, kernel, "damagedTexture", rt);
            cb.SetComputeIntParam(SphereTracingComputeShader, "textureSize", resolution);
            cb.SetComputeFloatParam(SphereTracingComputeShader, "totalUnitsInTexture", size);
            cb.SetComputeMatrixParam( SphereTracingComputeShader, "InverseTransform", matrix);
            cb.SetComputeVectorParam( SphereTracingComputeShader, "hitPoint", hitPoint);
            cb.SetComputeVectorParam(SphereTracingComputeShader, "hitDirection", direction);

            cb.DispatchCompute(SphereTracingComputeShader, kernel, 1, 1, 1);

            //hitBuffer.Release();

            Graphics.ExecuteCommandBuffer(cb);
            //Graphics.ExecuteCommandBufferAsync(cb, ComputeQueueType.Default);
            cb.Clear();
            //hitBuffer.GetData(hitInfo);
            //hitBuffer.Release();

            //Debug.Log( hitInfo[0]);

            AsyncGPUReadback.Request(hitBuffer, callback);

            hitBuffer.Dispose();
        }

        public static bool CheckHit(int resolution, ref Vector3 hitPoint, Vector3 direction, Matrix4x4 matrix, Texture3D tex, float size)
        {
            ComputeBuffer hitBuffer = new ComputeBuffer(1, sizeof(float) * 4);
            Vector4[] hitInfo = new Vector4[] { new Vector4(0, 0, 0, 0) };
            hitBuffer.SetData(hitInfo);

            // Instantiate the compute shader.
            int kernel = SphereTracingComputeShader.FindKernel("CSMain");

            // Upload the pixel buffer to the GPU.
            SphereTracingComputeShader.SetBuffer(kernel, "hitInfo", hitBuffer);
            if (!_alreadyDefined)
            {
                SphereTracingComputeShader.SetTexture(kernel, "damagedTexture", tex);//testje
                //_alreadyDefined = true;
            }

            // Upload the other necessary parameters.
            SphereTracingComputeShader.SetInt("textureSize", resolution);//resolution //tex.width
            SphereTracingComputeShader.SetFloat("totalUnitsInTexture", size);
            

            //Set sphere tracing info
            SphereTracingComputeShader.SetMatrix("InverseTransform", matrix);
            SphereTracingComputeShader.SetVector("hitPoint", hitPoint);
            SphereTracingComputeShader.SetVector("hitDirection", direction);

            // Compute.
            SphereTracingComputeShader.Dispatch(kernel, 1, 1, 1);

            // Retrieve the hitinfo
            hitBuffer.GetData(hitInfo);
            hitBuffer.Release();

            hitPoint = hitInfo[0];

            return hitInfo[0].w > 0.5f;
        }

        private static Vector3Int GetThreadGroups( int pixels)
        {
            return new Vector3Int(pixels / 256 + 1, 1, 1);
            /*switch (0)
            {
                case 0: // 1D 256
                    return new Vector3Int(pixels / 256 + 1, 1, 1);
                case 1: // 1D 512
                    return new Vector3Int(pixels / 512 + 1, 1, 1);
                case 2: // 1D 1024
                    return new Vector3Int(pixels / 1024 + 1, 1, 1);
                case 3: // 2D 32
                    return new Vector3Int(pixels / 32 + 1, pixels / 32 + 1, 1);//64
                case 4: // 3D 16*8*8
                    int x = pixels / 8;
                    //return new Vector3Int(x + 1, x / 2 + 1, x / 2 + 1);
                    return new Vector3Int(x / 3 + 1, x / 3 + 1, x / 3 + 1);
                default: // 1D 256
                    return new Vector3Int(pixels / 256 + 1, 1, 1);
            }*/
        }

        public enum SDFResolution
        {
            Low = 32,
            Medium = 64,
            High = 128,
            VeryHigh = 256 // Have to adjust the threads for the compute shaders...
        }
    }
}
