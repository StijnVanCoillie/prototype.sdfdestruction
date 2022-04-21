using UnityEngine;
using UnityEditor;

using Stijn.Prototype.Destruction;

namespace Stijn.Prototype.Editor
{
    public class DestructionHelper : MonoBehaviour
    {
        [MenuItem("CONTEXT/MeshFilter/Show Bounds")]
        static void ShowBounds(MenuCommand command)
        {
            MeshFilter mf = (MeshFilter)command.context;
            ShowBounds(mf);
        }

        static void ShowBounds( MeshFilter mf)
        {
            Bounds b = mf.sharedMesh.bounds;

            Debug.Log("Center: " + b.center.ToString("0.0000000"));
            Debug.Log("Size: " + b.size.ToString("0.0000000"));

            Debug.Log(b.ToString("0.0000000"));
        }

        [MenuItem("CONTEXT/Light/Calculate Light Direction")]
        static void CalculateLightDirection(MenuCommand command)
        {
            Light l = (Light)command.context;
            Debug.Log(l.transform.forward.ToString("0.0000"));
        }

        [MenuItem("Prototype/Fractured Object")]
        static void FracturedObject()
        {
            GameObject obj = Selection.activeGameObject;
            if( obj == null)
            {
                Debug.LogWarning("No GameObject is selected.");
                return;
            }
            else if (Selection.assetGUIDs.Length > 0)
            {
                Debug.LogWarning("Only GameObject from the scene.");
                return;
            }

            obj.layer = LayerMask.NameToLayer("Destructible");
            GameObject[] fractures = new GameObject[obj.transform.childCount-1];
            for( int i=0; i < obj.transform.childCount; ++i)
            {
                Transform t = obj.transform.GetChild(i);
                t.gameObject.layer = LayerMask.NameToLayer("Destructible");

                if( t.GetComponent<MeshCollider>() != null)
                {
                    DestroyImmediate(t.GetComponent<MeshCollider>());
                }
                t.gameObject.AddComponent<MeshCollider>();

                if( i > 0)
                {
                    fractures[i - 1] = t.gameObject;
                }
            }

            if (obj.transform.GetChild(0).GetComponent<FracturedObject>() != null)
            {
                DestroyImmediate(obj.transform.GetChild(0).GetComponent<FracturedObject>());
            }
            FracturedObject fo = obj.transform.GetChild(0).gameObject.AddComponent<FracturedObject>() as FracturedObject;
            fo.SetFractures(fractures);

            EditorUtility.SetDirty(obj);

            Debug.Log("Complete fractured object: "+obj.name);
        }
    }
}