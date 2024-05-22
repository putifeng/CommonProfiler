using System;
using System.Collections.Generic;
using Core.IO;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class ParticleSystemDetailCount : CommonProfilerInterface
    {

        class ParcitlePack
        {
            public int count;
            public int depth;
            public int index;
        }


        [NonSerialized]
        private Dictionary<string, ParcitlePack> particleCounts = new();

    [NonSerialized] [ShowInInspector] [ReadOnly] public int selectGoCount;
        [NonSerialized] [ShowInInspector] [ReadOnly] public int curSelectNodeParticleSystemCount;
    [NonSerialized] [ShowInInspector] [ReadOnly] public int curSelectNodeParticleCount;

        public bool CanWrite()
        {
            return true;
        }

        public void Clear()
        {
            particleCounts.Clear();
        }

        private List<ParticleSystem> _systems = new();


        public void CalecurStatisics(GameObject gameObject)
        {
            InitPartileSystemCount(string.Empty,gameObject);

        }
    [Button("计算当前选中节点的粒子数量")]
    public void CacleGoSystem()
    {
        if ( UnityEditor.Selection.gameObjects != null)
        {
            curSelectNodeParticleSystemCount = 0;
            selectGoCount = 0;
            curSelectNodeParticleCount =0;
            foreach (var gameObject in UnityEditor.Selection.gameObjects)
            {
                selectGoCount++;
                _systems.Clear();
                gameObject.GetComponentsInChildren(_systems);
                curSelectNodeParticleSystemCount += _systems.Count;

                int count = 0;
                for (int i = 0; i < _systems.Count; i++)
                {
                    object[] invokeArgs = {0, 0.0f, Mathf.Infinity};
                    CommonProfilerSerialHelper.m_CalculateEffectUIDataMethod.Invoke( _systems[i], invokeArgs);
                    count = (int) invokeArgs[0];
                    curSelectNodeParticleCount += count;
                }


            }

        }

    }


        public void OnBeforeSave(ref int startRow, XlsxWriter xlsxWriter)
        {

            int index = 1;
            xlsxWriter.WriteData(startRow, index++,"路径");
            xlsxWriter.WriteData(startRow, index++,"粒子组件数量");
            startRow++;

            List<(string path,ParcitlePack pack)> sortCounts = new();

            foreach(var it in particleCounts)
            {
                sortCounts.Add(new (){ path = it.Key,pack = it.Value} );
            }

            sortCounts.Sort((a, b) =>
            {
                if(a.pack.depth != b.pack.depth)
                    return a.pack.depth - b.pack.depth;
                return a.pack.index - b.pack.index;
            });
            foreach (var valueTuple in sortCounts)
            {
                index = 1;
                xlsxWriter.WriteData(startRow, index++,valueTuple.path);
                xlsxWriter.WriteData(startRow, index++, valueTuple.pack.count);
                startRow++;
            }
        }


    private int InitPartileSystemCount(string parentName,GameObject gameObject,int depth = 1,int depth2 = 1)
    {
        string fullName = string.Empty;
        if (!string.IsNullOrEmpty(parentName))
        {
            fullName = $"{parentName}/{gameObject.name}";
        }
        else
        {
            fullName = gameObject.name;
        }

        int count = 0;
        for (int i = 0; i < gameObject.transform.childCount; i++)
        {
            GameObject o = gameObject.transform.GetChild(i).gameObject;
            {
                if(o.activeInHierarchy)
                    count += InitPartileSystemCount( fullName,o,depth + i,Math.Abs(o.GetInstanceID()));
            }

        }


        if (gameObject.activeInHierarchy && gameObject.GetComponent<ParticleSystem>() != null)
            count++;

        if (count > 0)
        {
            if (!particleCounts.TryGetValue(fullName, out ParcitlePack oldPack))
            {
                ParcitlePack pack = new();
                pack.count = count;
                pack.depth = depth;
                pack.index = depth2;
                particleCounts.Add(fullName,pack);
            }
            else
            {
                oldPack.count = Math.Max(oldPack.count,count);
            }
        }

        return count;
        // ParcitlePack pack = GetOrInitParcitlePack(gameObject.transform);
        // if (gameObject.transform.parent != null)
        // {
        //     ParcitlePack parentPack = GetOrInitParcitlePack(gameObject.transform.parent);
        //     parentPack.childs.Add(pack.instanceId);
        // }
    }

    #if false
    private ParcitlePack GetOrInitParcitlePack(Transform transform)
    {
        if (!particlePaths.TryGetValue(transform.gameObject.GetInstanceID(), out ParcitlePack pack))
        {
            pack = new();
            pack.instanceId = transform.gameObject.GetInstanceID();
            pack.name = transform.name;
            particlePaths.Add(transform.gameObject.GetInstanceID(),pack);
            if (transform.parent != null)
            {
                ParcitlePack parentPack = GetOrInitParcitlePack(transform.parent);
                pack.parentInstanceId = parentPack.instanceId;
            }
        }
        return pack;
    }
#endif

    }
