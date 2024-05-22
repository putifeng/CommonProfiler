
using System.Collections.Generic;
using Core.IO;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

public class BattleFightProfiler : CommonProfilerInterface
{
    [ReadOnly]
    [CommonProfilerSerial("节点", 1)]
    public string path;

    [ReadOnly]
    [CommonProfilerSerial("贴图数量", 5)]
    public int textureCount;

    [ReadOnly]
    [CommonProfilerSerial("贴图大小(MB) ", 6)]
    [CommonProfilerBytesConvertString]
    // [CommonProfilerRecommendedValue("建议 < 50MB")]
    public int textureCountSize;

    [ReadOnly]
    [CommonProfilerSerial("所有粒子组件数量 ", 7)]
    // [CommonProfilerRecommendedValue("建议 < 200")]
    public int particleComponentCount;

    [ReadOnly]
    [CommonProfilerSerial("粒子数量", 8)]
    // [CommonProfilerRecommendedValue("建议 < 500")]
    public int particleCount;

    public void OnBeforeSave(ref int startRow, XlsxWriter xlsxWriter)
    {

    }

    public bool CanWrite()
    {
        return textureCount > 0 ||
            textureCountSize > 0 ||
            particleComponentCount > 0||
            particleCount > 0;
    }

    public void Clear()
    {
        path = string.Empty;
        textureCount = 0;
        textureCountSize = 0;
        particleComponentCount = 0;
        particleCount = 0;
    }

    public void CalecurStatisics(GameObject gameObject)
    {
        this.path = CommonProfilerSerialHelper.GetFullName(gameObject);

        List<UnityEngine.ParticleSystem> allParticleSystems = new List<ParticleSystem>();
        gameObject.GetComponentsInChildren(true, allParticleSystems);

        int pCount = 0;
        foreach (var ps in allParticleSystems)
        {
            int count = 0;
            object[] invokeArgs = {count, 0.0f, Mathf.Infinity};
            CommonProfilerSerialHelper.m_CalculateEffectUIDataMethod.Invoke(ps, invokeArgs);
            count = (int) invokeArgs[0];
            pCount += count;
        }

        particleCount = math.max(particleCount, pCount);
        this.particleComponentCount = math.max(particleComponentCount,allParticleSystems.Count);

        List<UnityEngine.Renderer> allRenderers = new List<Renderer>();
        gameObject.GetComponentsInChildren(false, allRenderers);

        int sumSize = 0;

        Dictionary<Texture,TextureBindGameObject> textureBinds = new ();

        textureBinds.Clear();
        foreach (var it in allRenderers)
        {
            if (it.sharedMaterials != null)
            {
                for (int i = 0; i < it.sharedMaterials.Length; i++)
                {
                    CommonProfilerSerialHelper.GetCertainMaterialTexturePaths(
                        (tex) =>
                        {
                            TextureBindGameObject textureBindGameObject = textureBinds.GetValue(tex);
                            if (textureBindGameObject == null)
                            {
                                textureBindGameObject = new TextureBindGameObject();
                                textureBindGameObject.Texture = tex;
                                textureBinds.Add(textureBindGameObject.Texture,textureBindGameObject);
                            }
                            textureBindGameObject.GameObjects.Add(CommonProfilerSerialHelper.GetFullName(it.gameObject));

                        }
                        , it.sharedMaterials[i]);
                }
            }
        }

        foreach (var it in textureBinds)
        {
            var texture = it.Key;
            int textSize = CommonProfilerSerialHelper.GetStorageMemorySize(texture);

            it.Value.size = textSize;
            sumSize += textSize;
        }
        textureCount = math.max(textureBinds.Count,textureCount);
        textureCountSize = math.max(sumSize,textureCountSize);
    }

}
