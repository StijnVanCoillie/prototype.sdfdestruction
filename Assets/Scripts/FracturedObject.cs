using UnityEngine;

namespace Stijn.Prototype.Destruction
{
    public class FracturedObject : MonoBehaviour
    {
        public GameObject[] _fractures;

        public void SetFractures(GameObject[] fractures)
        {
            _fractures = fractures;
            for (int i = 0; i < _fractures.Length; ++i)
            {
                _fractures[i].SetActive(false);
            }
        }

        private void OnDisable()
        {
            if (_fractures != null)
            {
                for (int i = 0; i < _fractures.Length; ++i)
                {
                    _fractures[i].SetActive(true);
                }
            }
        }
    }
}
