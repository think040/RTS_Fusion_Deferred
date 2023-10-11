using System.Collections;
using System.Collections.Generic;

using Unity.Mathematics;
using UnityEngine;

using Utility_JSB;


namespace UserAnimSpace
{
    [CreateAssetMenu(fileName = "Model", menuName = "Model")]
    public class Model : ScriptableObject, ISerializationCallbackReceiver
    {
        public GameObject model;
        private SkinnedMeshRenderer[] skmrs;
        private MeshFilter[] stmfs;
        private MeshRenderer[] stmrs;

        public List<string> _skNames;
        public List<string> _stNames;

        public Dictionary<string, Mesh> dicSkMesh;
        public List<Mesh> _skMesh;
        public Dictionary<string, Mesh> dicStMesh;
        public List<Mesh> _stMesh;

        public int skBoneCount = 0;
        public Dictionary<string, BoneName> dicSkBoneNames;
        public List<BoneName> _boneNames;
        public Dictionary<string, string> dicStBoneNames;
        public List<string> _boneStNames;

        //public List<string> mteNames;
        //public List<Color> colors;

        public List<string> skMteNames;
        public List<Color> skColors;

        public List<string> stMteNames;
        public List<Color> stColors;

        private void OnEnable()
        {
            skmrs = model.GetComponentsInChildren<SkinnedMeshRenderer>();
            stmfs = model.GetComponentsInChildren<MeshFilter>();
            stmrs = new MeshRenderer[stmfs.Length];
            for (int i = 0; i < stmfs.Length; i++)
            {
                stmrs[i] = stmfs[i].GetComponent<MeshRenderer>();
            }


            _skNames = new List<string>();
            _stNames = new List<string>();

            dicSkMesh = new Dictionary<string, Mesh>();
            _skMesh = new List<Mesh>();
            dicStMesh = new Dictionary<string, Mesh>();
            _stMesh = new List<Mesh>();

            dicSkBoneNames = new Dictionary<string, BoneName>();
            _boneNames = new List<BoneName>();

            dicStBoneNames = new Dictionary<string, string>();
            _boneStNames = new List<string>();

            skMteNames = new List<string>();
            skColors = new List<Color>();

            stMteNames = new List<string>();
            stColors = new List<Color>();

            for (int i = 0; i < skmrs.Length; i++)
            {
                string skname = skmrs[i].gameObject.name;

                Transform[] bones = skmrs[i].bones;
                string[] names = new string[bones.Length];
                for (int j = 0; j < bones.Length; j++)
                {
                    names[j] = bones[j].name;
                }

                _skNames.Add(skname);
                dicSkMesh[skname] = skmrs[i].sharedMesh;
                dicSkBoneNames[skname] = new BoneName { names = names };
                _skMesh.Add(skmrs[i].sharedMesh);
                _boneNames.Add(new BoneName { names = names });

                Material[] mtes = skmrs[i].sharedMaterials;
                for (int j = 0; j < mtes.Length; j++)
                {
                    skMteNames.Add(mtes[j].name);
                    skColors.Add(mtes[j].color);
                }
            }

            for (int i = 0; i < stmfs.Length; i++)
            {
                string stname = stmfs[i].gameObject.name;

                _stNames.Add(stname);
                dicStMesh[stname] = stmfs[i].sharedMesh;
                dicStBoneNames[stname] = stmfs[i].gameObject.name;
                _stMesh.Add(stmfs[i].sharedMesh);
                _boneStNames.Add(stmfs[i].gameObject.name);

                Material[] mtes = stmrs[i].sharedMaterials;
                for (int j = 0; j < mtes.Length; j++)
                {
                    stMteNames.Add(mtes[j].name);
                    stColors.Add(mtes[j].color);
                }
            }
        }

        public void OnBeforeSerialize()
        {
            _skMesh.Clear();
            _boneNames.Clear();
            _stMesh.Clear();
            _boneStNames.Clear();

            skBoneCount = 0;
            for (int i = 0; i < _skNames.Count; i++)
            {
                string skName = _skNames[i];
                _skMesh.Add(dicSkMesh[skName]);
                _boneNames.Add(dicSkBoneNames[skName]);
                skBoneCount += dicSkBoneNames[skName].names.Length;
            }

            for (int i = 0; i < _stNames.Count; i++)
            {
                string stName = _stNames[i];
                _stMesh.Add(dicStMesh[stName]);
                _boneStNames.Add(dicStBoneNames[stName]);
            }
        }

        public void OnAfterDeserialize()
        {
            dicSkBoneNames = new Dictionary<string, BoneName>();
            dicStBoneNames = new Dictionary<string, string>();
            dicSkMesh = new Dictionary<string, Mesh>();
            dicStMesh = new Dictionary<string, Mesh>();

            skBoneCount = 0;
            for (int i = 0; i < _skNames.Count; i++)
            {
                string skName = _skNames[i];
                dicSkMesh[skName] = _skMesh[i];
                dicSkBoneNames[skName] = _boneNames[i];
                skBoneCount += _boneNames[i].names.Length;
            }

            for (int i = 0; i < _stNames.Count; i++)
            {
                string stName = _stNames[i];
                dicStMesh[stName] = _stMesh[i];
                dicStBoneNames[stName] = _boneStNames[i];
            }
        }

        [System.Serializable]
        public struct BoneName
        {
            public string[] names;
        }

        //
        public static string MakePathPtoC(Transform parent, Transform child)
        {
            string path = null;

            List<string> strs = new List<string>();
            strs.Add(child.name);

            while (child.parent != null)
            {
                strs.Add(child.parent.name);
                child = child.parent;
                if (parent.name == child.name)
                {
                    break;
                }
            }
            strs.Reverse();


            for (int i = 0; i < strs.Count; i++)
            {
                if (i == 0)
                {
                    path += strs[i];
                }
                else
                {
                    path += ("/" + strs[i]);
                }
            }

            return path;
        }


        [System.Serializable]
        public struct BonePath
        {
            public string[] paths;
        }

    }
}
