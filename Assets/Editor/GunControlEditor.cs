using UnityEngine;
using UnityEditor;

using Stijn.Prototype.Gun;

namespace Stijn.Prototype.Editor
{
    public class GunControlEditor : EditorWindow
    {
        private GunControl _gunControl;

        private SerializedObject _so;

        private SerializedProperty _approachProperty;
        private SerializedProperty _radiusDamageProperty;
        private SerializedProperty _delayProperty;

        [MenuItem("Prototype/Easy Gun Control Menu")]
        static void Init() => GetWindow<GunControlEditor>("Gun Control");

        private void OnEnable()
        {
            SceneView.duringSceneGui += this.OnSceneGUI;

            FindGunControl();
        }

        void OnDisable()
        {
            SceneView.duringSceneGui -= this.OnSceneGUI;
        }

        private void OnGUI()
        {
            FindGunControl();

            if (!CanDisplay()) return;

            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(_approachProperty, new GUIContent(""));
            EditorGUILayout.Space(3);

            EditorGUILayout.LabelField("Radius Damage:");
            EditorGUILayout.Slider(_radiusDamageProperty, 0.1f, 2f, new GUIContent(""));

            EditorGUILayout.LabelField("Delay:");
            EditorGUILayout.Slider(_delayProperty, 0.0f, 0.5f, new GUIContent(""));//0.02

            EditorGUILayout.Space(5);

            _so.ApplyModifiedProperties();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            FindGunControl();

            if (!CanDisplay()) return;

            GUILayout.Window(2, new Rect(10, 30, 80, 46), (id) =>
            {
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.Space(5);
                EditorGUILayout.PropertyField(_approachProperty, new GUIContent(""));
                EditorGUILayout.Space(3);

                EditorGUILayout.LabelField("Radius Damage:");
                EditorGUILayout.Slider(_radiusDamageProperty, 0.1f, 2f, new GUIContent(""));

                EditorGUILayout.LabelField("Delay:");
                EditorGUILayout.Slider(_delayProperty, 0.02f, 0.5f, new GUIContent(""));

                EditorGUILayout.Space(5);

                if (EditorGUI.EndChangeCheck())
                {
                    Repaint();
                }
            }, "Gun");

            _so.ApplyModifiedProperties();
        }

        void FindGunControl()
        {
            if (!CanDisplay())
            {
                if (_gunControl == null)
                {
                    GunControl[] guns = FindObjectsOfType<GunControl>() as GunControl[];
                    for (int i = 0; i < guns.Length; ++i)
                    {
                        if (guns[i].gameObject.activeInHierarchy)
                        {
                            _gunControl = guns[i];
                            break;
                        }
                    }
                }

                if (_gunControl)
                {
                    _so = new SerializedObject(_gunControl);

                    _approachProperty = _so.FindProperty("DestructionApproach");
                    _radiusDamageProperty = _so.FindProperty("RadiusDamage");
                    _delayProperty = _so.FindProperty("Delay");
                }
            }
        }

        private bool CanDisplay()
        {
            return _gunControl != null && _so != null && _approachProperty != null && _radiusDamageProperty != null && _delayProperty != null;
        }
    }
}
