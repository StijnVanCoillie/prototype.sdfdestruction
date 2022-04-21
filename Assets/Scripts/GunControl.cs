using UnityEngine;
using Stijn.Prototype.Destruction;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine.Rendering;

namespace Stijn.Prototype.Gun
{
    public class GunControl : MonoBehaviour
    {
        public Approach DestructionApproach = Approach.NewSchool;

        [SerializeField]
        private Transform _transform = null;

        [SerializeField]
        private LineRenderer _lineRenderer;

        [SerializeField]
        private LayerMask _destructionLayerMask;

        [SerializeField]
        private float _maxDistance = 150f;

        //[SerializeField]
        [Range(0.1f, 2f)]
        public float RadiusDamage = 1;

        //[SerializeField]
        [Range(0.0f, 0.5f)]//0.02
        public float Delay = 0.5f;
        private float _delayTimer;

        private Target _target = new Target();
        private RaycastHit _hit;

        [Space(20)]
        [SerializeField]
        private bool _logEnabled = true;

        private DestructibleObject _testke;

        void Update()
        {
            Debug.unityLogger.logEnabled = _logEnabled;

            //UpdateTarget();
            //UpdateLaserGun();
            //ApplyDamage();

            if (Input.GetMouseButton(0))
            {
                if (_delayTimer >= Delay)
                {
                    _delayTimer -= Delay;

                    /*if (Physics.Raycast(_transform.position, _transform.forward, out _hit, _maxDistance, _destructionLayerMask))
                    {
                        if (DestructionApproach == Approach.NewSchool)
                        {
                            DestructibleObject destr = _hit.collider.GetComponent<DestructibleObject>() as DestructibleObject;
                            Vector3 hitPoint = _hit.point;
                            if (destr.Raycast(ref hitPoint, _transform.forward))
                            {
                                destr.AddDamage(hitPoint, RadiusDamage);
                            }
                        }
                        else
                        {
                            _hit.collider.gameObject.SetActive(false);
                        }
                        //return;
                    }*/
                    if( DestructionApproach == Approach.NewSchool)
                    {
                        if (Physics.Raycast(_transform.position, _transform.forward, out _hit, _maxDistance, _destructionLayerMask))
                        {
                            DestructibleObject destr = _hit.collider.GetComponent<DestructibleObject>() as DestructibleObject;
                            Vector3 hitPoint = _hit.point;
                            //_testke = destr;
                            if (destr.Raycast(ref hitPoint, _transform.forward))
                            {
                                destr.AddDamage(hitPoint, _transform.forward, RadiusDamage);//_transform.up
                            }

                            // Async test
                            //Debug.Log("1");
                            //destr.RaycastAsync(hitPoint, _transform.forward, OnCompleteRaycastReadback);
                        }
                    }
                    else
                    {
                        Collider[] cols = Physics.OverlapCapsule(_transform.position, _transform.position + _transform.forward * 20, RadiusDamage*0.5f, _destructionLayerMask);
                        for( int i=0; i < cols.Length; ++i)
                        {
                            cols[i].gameObject.SetActive(false);
                        }
                    }
                }

                _lineRenderer.positionCount = 2;
                _lineRenderer.SetPosition(0, Vector3.zero);
                _lineRenderer.SetPosition(1, new Vector3(0,0,150));
            }
            else
            {
                _lineRenderer.positionCount = 0;
            }

            _delayTimer += Time.deltaTime;

            Debug.unityLogger.logEnabled = true;
        }

        void OnCompleteRaycastReadback( AsyncGPUReadbackRequest request)
        {
            if (_testke == null) return;
            //Debug.Log("2");
            if( !request.hasError)
            {
                //Debug.Log("3");
                //Vector4[] hitInfo = new Vector4[] { new Vector4(0, 0, 0, 0) };
                Vector4[] info = request.GetData<Vector4>().ToArray();
                if( info != null)
                {
                    //Debug.Log(info[0]);
                    if( info[0].w > 0.5)
                    {
                        _testke.AddDamage(info[0], _transform.forward, RadiusDamage);
                    }
                }
            }
        }

        private void UpdateTarget()
        {
            _target.IsHit = false;
            _target.HitPoint = _transform.position + _transform.forward * _maxDistance;
            _target.CanDamage = false;

            if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(0))
            {
                _delayTimer = 0;
                _target.IsHit = true;
                _target.CanDamage = true;
            }
            else if (Input.GetMouseButton(0))
            {
                _target.IsHit = true;
                if( _delayTimer >= Delay)
                {
                    _target.CanDamage = true;
                    _delayTimer -= Delay;
                }
            }

            if(_target.IsHit)
            {
                if (Physics.Raycast(_transform.position, _transform.forward, out _hit, _maxDistance, _destructionLayerMask))
                {
                    _target.HitPoint = _hit.point;
                    _target.GO = _hit.collider.gameObject;
                    _target.Destructible = _hit.collider.GetComponent<DestructibleObject>() as DestructibleObject;
                    //_target.CanDamage = _target.CanDamage && _target.Destructible != null;
                    //_target.CanDamage = true;
                    return;
                }
                _target.CanDamage = false;
            }
        }

        private void ApplyDamage()
        {
            
            if( _target.CanDamage)
            {
                if (DestructionApproach == Approach.NewSchool)
                {
                    if (_target.Destructible.Raycast(ref _target.HitPoint, _transform.forward))
                    {
                        _target.Destructible.AddDamage(_target.HitPoint, _transform.forward, RadiusDamage);
                    }
                    return;
                }

                _target.GO.SetActive(false);
            }
        }

        void UpdateLaserGun()
        {
            if (_target.IsHit)
            {
                _lineRenderer.positionCount = 2;
                _lineRenderer.SetPosition(0, Vector3.zero);
                Vector3 hitPoint = _transform.InverseTransformPoint(_target.HitPoint);
                _lineRenderer.SetPosition(1, hitPoint);
                return;
            }
            
            _lineRenderer.positionCount = 0;
        }

        class Target
        {
            public bool IsHit;
            public Vector3 HitPoint = Vector3.zero;
            public bool CanDamage;
            public DestructibleObject Destructible;
            public GameObject GO;
        }

        public enum Approach
        {
            OldSchool = 0,
            NewSchool = 1
        }
    }
}
