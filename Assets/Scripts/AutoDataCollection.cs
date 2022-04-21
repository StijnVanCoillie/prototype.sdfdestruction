using System.Collections;
using UnityEngine;

using Stijn.Prototype.Destruction;

using System.Diagnostics;

using Stijn.Prototype.Utility;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Stijn.Prototype.Data
{
    public class AutoDataCollection : MonoBehaviour
    {
        public readonly float WaitBeforeTesting = 10f;
        public readonly float SecondsBetweenTesting = 2f;

        [SerializeField]
        private int _iterations = 10;

        [SerializeField]
        private DestructionUtility.SDFResolution[] _resolutions = null;

        [SerializeField]
        private DestructibleObject[] _destructibleObjects = null;

        [SerializeField]
        private Transform _gun = null;

        [SerializeField]
        private LayerMask _destructionLayerMask;

        //[SerializeField]
        [Range(0.1f, 2f)]
        public float RadiusDamage = 1;

        [SerializeField]
        private bool _collectData = false;
        Stopwatch _stopwatch;

        void Start()
        {
            if (!_collectData) return;

            StartCoroutine(CollectData());
        }

        IEnumerator CollectData()
        {
            _stopwatch = new Stopwatch();

            for (int i = 0; i < _destructibleObjects.Length; ++i)
            {
                _destructibleObjects[i].gameObject.SetActive(false);
            }

            string[][] totalData = new string[_destructibleObjects.Length * _resolutions.Length][];
            string[] data = new string[_iterations];
            string[] titles = new string[_destructibleObjects.Length * _resolutions.Length];

            int count = 0;
            for ( int i=0; i < _destructibleObjects.Length; ++i)
            {
                for( int j=0; j < _resolutions.Length; ++j)
                {
                    titles[count] = _destructibleObjects[i].gameObject.name + " " + _resolutions[j].ToString();
                    ++count;
                }
            }

            yield return new WaitForSeconds(WaitBeforeTesting);

            count = 0;
            for (int i = 0; i < _destructibleObjects.Length; ++i)
            {
                _destructibleObjects[i].gameObject.SetActive(true);

                for( int r = 0; r < _resolutions.Length; ++r)
                {
                    _destructibleObjects[i].ChangeTextureResolution(_resolutions[r]);
                    yield return new WaitForSeconds(SecondsBetweenTesting);

                    for (int j = 0; j < _iterations + 1; ++j)
                    {
                        _stopwatch.Start();
                        // Add damage
                        AddDamage();
                        //
                        _stopwatch.Stop();

                        if (j > 0)
                        {
                            data[j - 1] = _stopwatch.ElapsedTicks.ToString();
                        }

                        _destructibleObjects[i].ResetDestructibleObject();

                        _stopwatch.Reset();
                        yield return new WaitForSeconds(SecondsBetweenTesting);
                    }

                    totalData[count] = data.Clone() as string[];
                    ++count;
                }

                _destructibleObjects[i].gameObject.SetActive(false);
            }

            DataUtility.WriteCsvFile(totalData, titles);

#if UNITY_EDITOR
            EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
        }

        private void AddDamage()
        {
            RaycastHit hit;
            if (Physics.Raycast(_gun.position, _gun.forward, out hit, 10f, _destructionLayerMask))
            {
                DestructibleObject destr = hit.collider.GetComponent<DestructibleObject>() as DestructibleObject;
                Vector3 hitPoint = hit.point;
                if (destr.Raycast(ref hitPoint, _gun.forward))
                {
                    destr.AddDamage(hitPoint, _gun.forward, RadiusDamage);
                }
            }
        }

#if UNITY_EDITOR
        [MenuItem("CONTEXT/AutoDataCollection/Calculate total time")]
        static void CalculateTotalTime(MenuCommand command)
        {
            AutoDataCollection destrObj = (AutoDataCollection)command.context;

            float t = destrObj.WaitBeforeTesting;
            t += (destrObj._iterations +1) * destrObj.SecondsBetweenTesting * destrObj._destructibleObjects.Length * destrObj._resolutions.Length;
            UnityEngine.Debug.Log("Total time to run this test: " + t.ToString("0.0") + " seconds.");
        }


        private void OnDrawGizmos()
        {
            if (_gun == null) return;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(_gun.position, _gun.position + _gun.forward * 10f);
        }
#endif
    }
}