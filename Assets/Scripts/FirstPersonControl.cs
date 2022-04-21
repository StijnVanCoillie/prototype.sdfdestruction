using UnityEngine;
using System.Collections;

namespace Stijn.Prototype.Character
{
    public class FirstPersonControl : MonoBehaviour
    {
        [SerializeField]
        private float _speed = 5;
        [SerializeField]
        private float _rotationSpeed = 360;

        private Transform _cam;

        IEnumerator Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            _cam = Camera.main.transform;

            this.enabled = false;

            yield return new WaitForSeconds(2);
            this.enabled = true;
        }

        void Update()
        {
            Vector3 v = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

            this.transform.Rotate(Vector3.up, Input.GetAxis("Mouse X") * _rotationSpeed * Time.deltaTime);

            _cam.Rotate(Vector3.right, Input.GetAxis("Mouse Y") * -_rotationSpeed * Time.deltaTime);
            Vector3 r = _cam.eulerAngles;

            r.x = ClampAngle(r.x, -40, 40);
            r.z = 0;

            this.transform.Translate(v * _speed * Time.deltaTime, Space.Self);
            _cam.eulerAngles = r;
        }

        float ClampAngle(float angle, float from, float to)
        {
            if (angle < 0f)
            {
                angle = 360 + angle;
            }
            else if (angle > 180f)
            {
                return Mathf.Max(angle, 360 + from);
            }
            return Mathf.Min(angle, to);
        }
    }
}
