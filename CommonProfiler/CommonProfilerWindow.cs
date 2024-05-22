using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public class CommonProfilerWindow : OdinEditorWindow
{
    [NonSerialized] [ShowInInspector][ReadOnly] public List<CommonOneRootProfiler> OneRootProfilers;

    [InfoBox("把需要检测的根节点拖进来,然后点击计算保存,将会计算当前场景运行时数据以及该节点下的粒子/贴图相关数据")]
    [NonSerialized] [ShowInInspector] public GameObject root;

    // [MenuItem("Tools/CommonProfilerWindow (通用性能检测工具)")]
    static void OpenCommonProfiler()
    {
        GetWindow<CommonProfilerWindow>();
    }

    [Button("计算当前帧消耗并保存")]
    public void CacleStatus()
    {
        if (root == null)
        {
            Debug.LogError("先把节点挂进来");
            return;
        }
        if (!Application.isPlaying)
        {
            Debug.LogError("计算前需要运行游戏.");
            return;
        }

        if (OneRootProfilers == null)
        {
            OneRootProfilers = new List<CommonOneRootProfiler>();
            OneRootProfilers.Add(new CommonOneRootProfiler());
        }
            
        OneRootProfilers[0].CalecurStatisics(root);

        SaveData();
    
    }

    // [Button("保存")]
    public void SaveData()
    {
        if (OneRootProfilers == null)
            return;

        CommonProfilerSerialHelper.SaveToXlsx(OneRootProfilers, "Assets/ConsumptionDatas~/ComonProfiler");
        
        
    }
}