using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public class BattleProfilerWindow : OdinEditorWindow
{
    [NonSerialized] [ShowInInspector][ReadOnly] public Dictionary<int,BattleFightProfiler> RootProfilers = new();

    // [NonSerialized] [ShowInInspector][ReadOnly] public UnityStatusProfiler UnityStatusProfiler = new();
    [NonSerialized] [ShowInInspector][ReadOnly] public CommonOneRootProfiler UnityStatusProfiler = new();

    [NonSerialized] [ShowInInspector]public ParticleSystemDetailCount ParticleSystemDetail = new();


    // [InfoBox("把需要检测的根节点拖进来,然后点击计算保存,将会计算当前场景运行时数据以及该节点下的粒子/贴图相关数据")]
    [NonSerialized] [ShowInInspector] [ReadOnly] public GameObject root;



    public string battleRoot = "BattleRoot(Clone)";

    [ReadOnly]
    public bool isCacling = false;

    [MenuItem("Tools/BattleProfilerWindow (战斗性能检测工具)")]
    static void OpenCommonProfiler()
    {
        BattleProfilerWindow window = GetWindow<BattleProfilerWindow>();
        window.RootProfilers.Clear();
        window.root = GameObject.Find(window.battleRoot);

    }

    protected override void OnEnable()
    {
        base.OnEnable();
        EditorApplication.update += OnUpdate;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        EditorApplication.update -= OnUpdate;
    }

    public void OnUpdate()
    {
        if (isCacling)
        {
            CacleStatus();
        }

    }

    [DisableIf("@this.isCacling == true")]
    [Button("开始检测")]
    public void StartCacle()
    {
        isCacling = true;
        RootProfilers.Clear();
        UnityStatusProfiler.Clear();
        ParticleSystemDetail.Clear();
    }

    [DisableIf("@this.isCacling == false")]
    [Button("停止检测")]
    public void Stop()
    {
        isCacling = false;
    }

    [DisableIf("@this.isCacling == false")]
    [Button("停止检测并保存")]
    public void StopAndSave()
    {
        if (isCacling)
        {
            SaveData();
            isCacling = false;
        }

    }

    // [Button("计算当前帧消耗并保存")]
    public void CacleStatus()
    {
        if(root == null)
            root = GameObject.Find(battleRoot);

        if (root == null)
        {
            Debug.LogError($"找不到 {battleRoot}.");
            return;
        }
        if (!Application.isPlaying)
        {
            Debug.LogError("计算前需要运行游戏.");
            return;
        }

        for (int i = 0; i < root.transform.childCount;i++)
        {
            var trans = root.transform.GetChild(i);
            int key = trans.gameObject.GetInstanceID();
            var profiler = RootProfilers.GetValue(key);
            if (profiler == null)
            {
                profiler = new BattleFightProfiler();
                RootProfilers.Add(key,profiler);
            }

            profiler.CalecurStatisics(trans.gameObject);
        }

        UnityStatusProfiler.CalecurStatisics(root);
        ParticleSystemDetail.CalecurStatisics(root);

    }

    [Button("重置数据")]
    private void ClearProfiler()
    {
        UnityStatusProfiler?.Clear();
        ParticleSystemDetail?.Clear();
    }
    private void SaveData()
    {
        if (RootProfilers == null)
            return;

        using (var saveTool = new CommonSaveTools("Assets/ConsumptionDatas~/BattleFightProfiler"))
        {
            saveTool.WriteData(UnityStatusProfiler);
            saveTool.WriteData(RootProfilers.Values);
            saveTool.WriteData(ParticleSystemDetail);
        }




        // CommonProfilerSerialHelper.SaveToXlsx(RootProfilers.Values, "Assets/ConsumptionDatas~/ComonProfiler");
    }
}
