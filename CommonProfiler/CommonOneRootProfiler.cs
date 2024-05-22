using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ParticleTool;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using GameMain;
using Unity.Mathematics;
using Unity.VisualScripting;

[Serializable]
public class CommonOneRootProfiler : CommonProfilerInterface
{
    [ReadOnly]
    [CommonProfilerSerial("资源路径", 1)]
    public string path;

    [ReadOnly]
    [CommonProfilerSerial("运行时三角面片数量", 2)]
    [CommonProfilerBytesConvertString(CommonProfilerBytesConvertString.ConvertType.Thousand)]
    [CommonProfilerRecommendedValue("建议 < 150k")]
    public int runTimeTriangles;

    [ReadOnly]
    [CommonProfilerSerial("运行时Batches", 4)]
    [CommonProfilerRecommendedValue("建议 < 200")]
    public int batches;

    [ReadOnly]
    [CommonProfilerSerial("贴图数量", 5)]
    // [CommonProfilerRecommendedValue("建议 < 50")]
    public int textureCount;

    [ReadOnly]
    [CommonProfilerSerial("贴图大小(MB) ", 6)]
    [CommonProfilerBytesConvertString]
    [CommonProfilerRecommendedValue("建议 < 50MB")]
    public int textureCountSize;

    [ReadOnly]
    [CommonProfilerSerial("粒子所有组件数量 ", 7)]
    [CommonProfilerRecommendedValue("建议 < 200")]
    public int particleComponentCount;

    [ReadOnly]
    [CommonProfilerSerial("粒子所有组件数量(组件被激活的)", 8)]
    [CommonProfilerRecommendedValue("建议 < 200")]
    public int particleComponentCount_Active;

    [ReadOnly]
    [CommonProfilerSerial("粒子数量", 9)]
    [CommonProfilerRecommendedValue("建议 < 500")]
    public int particleCount;

    // [ReadOnly]
    // [CommonProfilerSerial("运行时顶点数量", 3)]
    // [CommonProfilerRecommendedValue("建议 < 400k")]
    // [CommonProfilerBytesConvertString(CommonProfilerBytesConvertString.ConvertType.Thousand)]
    // int runTimevertices;

    [NonSerialized]
    Dictionary<Texture,TextureBindGameObject> textureBinds = new ();


    public bool CanWrite()
    {
        return true;
    }

    public void Clear()
    {
        runTimeTriangles = 0;
        batches = 0;
        particleCount = 0;
        particleComponentCount = 0;
        particleComponentCount_Active = 0;
        textureCount = 0;
        textureCountSize = 0;


    }

    public void CalecurStatisics(GameObject gameObject)
    {
        this.path = CommonProfilerSerialHelper.GetFullName(gameObject);
        this.runTimeTriangles = math.max(runTimeTriangles,UnityEditor.UnityStats.triangles);
        // this.runTimevertices = UnityEditor.UnityStats.vertices;
        this.batches = math.max(batches,UnityEditor.UnityStats.batches);

        List<UnityEngine.ParticleSystem> allParticleSystems = new List<ParticleSystem>();
        gameObject.GetComponentsInChildren(true, allParticleSystems);

        particleCount = 0;

        int pCount = 0;
        int actCount = 0;
        foreach (var ps in allParticleSystems)
        {
            int count = 0;
            object[] invokeArgs = {count, 0.0f, Mathf.Infinity};
            CommonProfilerSerialHelper.m_CalculateEffectUIDataMethod.Invoke(ps, invokeArgs);
            count = (int) invokeArgs[0];
            pCount += count;
            if (ps.gameObject.activeInHierarchy)
            {
                actCount++;
            }

        }
        particleCount =  math.max(particleCount,pCount);

        this.particleComponentCount =math.max(particleComponentCount, allParticleSystems.Count);
        this.particleComponentCount_Active =math.max(particleComponentCount_Active, actCount);

        List<UnityEngine.Renderer> allRenderers = new List<Renderer>();
        gameObject.GetComponentsInChildren(false, allRenderers);

        int sumSize = 0;
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
                            textureBindGameObject.GameObjects.Add(CommonProfilerSerialHelper.GetFullName( it.gameObject));

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

    public void OnBeforeSave(ref int startRow,Core.IO.XlsxWriter xlsxWriter)
    {
        startRow += 5;
        int index = 1;
        xlsxWriter.WriteData(startRow, index++,"贴图文件路径");
        xlsxWriter.WriteData(startRow, index++,"贴图大小(MB)");
        xlsxWriter.WriteData(startRow, index++,"贴图使用节点");

        startRow++;
        List<TextureBindGameObject> binds = new List<TextureBindGameObject>();
        foreach(var it in textureBinds)
            binds.Add(it.Value);

        binds.Sort((a,b) =>
        {
            return b.size - a.size;
        });

        foreach(var it in binds)
        {
            TextureBindGameObject bindGameObject = it;
            index = 1;
            xlsxWriter.WriteData(startRow, index++, AssetDatabase.GetAssetPath(bindGameObject.Texture) );
            xlsxWriter.WriteData(startRow, index++, CommonProfilerSerialHelper.ConvertToString(bindGameObject.size,
                CommonProfilerBytesConvertString.BytesDefault) );
            for (int i = 0; i < bindGameObject.GameObjects.Count; i++)
            {
                xlsxWriter.WriteData(startRow++, index,bindGameObject.GameObjects[i] );
            }
            index ++;
            startRow++;
        }

    }
}


class TextureBindGameObject
{
    public Texture Texture;
    public List<string> GameObjects = new List<string>();
    public int size;
}
public interface CommonProfilerInterface
{
    public bool CanWrite();
    public void Clear();
    public void CalecurStatisics(GameObject gameObject);
    public void OnBeforeSave(ref int startRow, Core.IO.XlsxWriter xlsxWriter);
}

public class CommonProfilerSerial : Attribute
{
    public string docFieldName;
    public int order;

    public CommonProfilerSerial(string docFieldName, int order = 1)
    {
        this.order = order;
        this.docFieldName = docFieldName;
    }
}

public class CommonProfilerBytesConvertString : Attribute
{
    public float NumberOfRetention;
    public float ConvertBase;

    public static readonly CommonProfilerBytesConvertString BytesDefault = new (1024f * 1024f,4);

    public static readonly CommonProfilerBytesConvertString ThousandDefault = new (1000,4);

    public enum ConvertType
    {
        Bytes,
        Thousand
    }

    public CommonProfilerBytesConvertString(ConvertType type = ConvertType.Bytes) : this(type == ConvertType.Bytes ? BytesDefault : ThousandDefault)
    {

    }

    public CommonProfilerBytesConvertString(CommonProfilerBytesConvertString other)
    {
        NumberOfRetention = other.NumberOfRetention;
        ConvertBase = other.ConvertBase;
    }

    public CommonProfilerBytesConvertString(float convertBase , int numberOfRetention)
    {
        NumberOfRetention = Mathf.Pow(10,numberOfRetention);
        ConvertBase = convertBase;
    }
}

public class CommonProfilerRecommendedValue : Attribute
{
    public string RecommendedValue;

    public CommonProfilerRecommendedValue(string recommendedValue)
    {
        RecommendedValue = recommendedValue;
    }
}

public class CommonSaveTools : IDisposable
{
    public string savePath;
    private Core.IO.XlsxWriter xlsxWriter;
    private DateTime startTime;
    private string timerStr;
    private Dictionary<Type, SheetData> SheetDatas = new();
    class SheetData
    {
        public int starRow;
        public Type type;
    }

    public CommonSaveTools( string genDataPath)
    {
        startTime = DateTime.Now;
        timerStr = startTime.ToString("yyyy/MM/dd HH:mm:ss");
        string exName = timerStr.Replace("/", "_").Replace(" ", "_").Replace(":", "_");
        savePath = $"{genDataPath}_{exName}.xlsx";

        SheetDatas.Clear();
        xlsxWriter = new Core.IO.XlsxWriter(savePath);
    }

    public void WriteData<T>(IEnumerable<T> datas) where T : class, CommonProfilerInterface
    {
        using var iter = datas.GetEnumerator();
        while (iter.MoveNext() && iter.Current != null)
        {
            if(iter.Current.CanWrite())
                WriteData(iter.Current);
        }
    }

    public void WriteData<T>(T data) where T : class, CommonProfilerInterface
    {
        try
        {
            xlsxWriter.ChangeSheetName(typeof(T).Name);
            var serialPacks = CommonProfilerSerialHelper.GetSerialPacks<T>();

            bool isNew = false;
            var sheetData = SheetDatas.GetValue(typeof(T));
            if (sheetData == null)
            {
                sheetData = new SheetData();
                sheetData.type = typeof(T);
                sheetData.starRow = 1;
                isNew = true;
                SheetDatas.Add(typeof(T), sheetData);
            }

            if (isNew)
            {
                int index = 1;
                xlsxWriter.WriteData(sheetData.starRow, 1, $"生成时间{timerStr}");
                sheetData.starRow++;
                for (int i = 0; i < serialPacks.Count; i++)
                {
                    xlsxWriter.WriteData(sheetData.starRow, i + 1, serialPacks[i].particleConsumptionSerial.docFieldName);
                }

                sheetData.starRow++;
                for (int i = 0; i < serialPacks.Count; i++)
                {
                    if (serialPacks[i].ProfilerRecommendedValue != null)
                    {
                        xlsxWriter.WriteData(sheetData.starRow, i + 1,
                            serialPacks[i].ProfilerRecommendedValue.RecommendedValue);
                    }
                }

            }

            sheetData.starRow++;
            for (int j = 0; j < serialPacks.Count; j++)
            {
                var val = serialPacks[j].fieldInfo.GetValue(data);
                if (serialPacks[j].convertString != null)
                {
                    val = CommonProfilerSerialHelper.ConvertToString((int) val, serialPacks[j].convertString);
                }

                xlsxWriter.WriteData(0 + sheetData.starRow, j + 1, val);
            }

            data.OnBeforeSave(ref sheetData.starRow, xlsxWriter);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }


    public void Dispose()
    {
        try
        {
            xlsxWriter.SaveData();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        EditorUtility.DisplayDialog("信息", "保存成功", "输出文件" + savePath);
    }
}

public class CommonProfilerSerialHelper
{
    public static MethodInfo m_CalculateEffectUIDataMethod;


    private static Dictionary<Type, List<CommonProfilerSerialPack>> caches = new Dictionary<Type, List<CommonProfilerSerialPack>>();

    static CommonProfilerSerialHelper()
    {
        m_CalculateEffectUIDataMethod = typeof(ParticleSystem).GetMethod("CalculateEffectUIData",
            BindingFlags.Instance | BindingFlags.NonPublic);
    }

    public static void GetCertainMaterialTexturePaths(Action<Texture> texturesHandler, Material _mat)
    {
        if (_mat == null)
            return;

        Shader shader = _mat.shader;
        for (int i = 0; i < ShaderUtil.GetPropertyCount(shader); ++i)
        {
            if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
            {
                string propertyName = ShaderUtil.GetPropertyName(shader, i);
                Texture tex = _mat.GetTexture(propertyName);
                if (tex != null)
                    texturesHandler.Invoke(tex);
            }
        }
    }

    public static int GetStorageMemorySize(Texture texture)
    {
        return (int) InvokeInternalAPI("UnityEditor.TextureUtil", "GetStorageMemorySize", texture);
    }

    public static object InvokeInternalAPI(string type, string method, params object[] parameters)
    {
        var assembly = typeof(UnityEditor.AssetDatabase).Assembly;
        var custom = assembly.GetType(type);
        var methodInfo = custom.GetMethod(method, BindingFlags.Public | BindingFlags.Static);
        return methodInfo != null ? methodInfo.Invoke(null, parameters) : 0;
    }

    public static List<CommonProfilerSerialPack> GetSerialPacks<T>() where T : CommonProfilerInterface
    {
        var ret = caches.GetValue(typeof(T));
        if (ret != null)
            return ret;

        List<CommonProfilerSerialPack> serialPacks = new List<CommonProfilerSerialPack>();

        caches.Add(typeof(T),serialPacks);
        Type consumptionType = typeof(T);

        FieldInfo[] fieldInfos = consumptionType.GetFields();

        for (int i = 0; i < fieldInfos.Length; i++)
        {
            CommonProfilerSerial consumptionSerial =
                fieldInfos[i].GetCustomAttribute<CommonProfilerSerial>();

            if (consumptionSerial == null)
                continue;


            serialPacks.Add(new CommonProfilerSerialPack()
            {
                fieldInfo = fieldInfos[i],
                particleConsumptionSerial = consumptionSerial,
                ProfilerRecommendedValue = fieldInfos[i].GetCustomAttribute<CommonProfilerRecommendedValue>(),
                convertString = fieldInfos[i].GetCustomAttribute<CommonProfilerBytesConvertString>()
            });
        }

        serialPacks.Sort((a, b)
            => a.particleConsumptionSerial.order - b.particleConsumptionSerial.order);

        return serialPacks;
    }

    public static float ConvertToString(int val)
    {
        return ConvertToString(val,CommonProfilerBytesConvertString.ThousandDefault);
    }

    public static float ConvertToString(int val,CommonProfilerBytesConvertString convertString )
    {
        float v = (val / convertString.ConvertBase);
        v *= convertString.NumberOfRetention;
        v = (int) v;
        v = v /  convertString.NumberOfRetention;
        return v;
    }

    public static string GetFullName(GameObject gameObject)
    {
        string name = gameObject.name;
        Transform transform = gameObject.transform.parent;
        while (transform != null)
        {
            name = $"{transform.name}/{name}";
            transform = transform.parent;
        }

        return name;
    }

    public static void SaveToXlsx<T>(IEnumerable<T> datas, string genDataPath) where T : class,CommonProfilerInterface
    {
        DateTime startTime = DateTime.Now;
        string timerStr = startTime.ToString("yyyy/MM/dd HH:mm:ss");
        string exName = timerStr.Replace("/", "_").Replace(" ", "_").Replace(":", "_");
        string savePath = $"{genDataPath}_{exName}.xlsx";

        try
        {
            Core.IO.XlsxWriter xlsxWriter = new Core.IO.XlsxWriter(savePath, typeof(T).Name);

            var serialPacks = GetSerialPacks<T>();

            int index = 1;
            int startRow = 1;
            xlsxWriter.WriteData(startRow, 1, $"生成时间{timerStr}");
            startRow++;
            for (int i = 0; i < serialPacks.Count; i++)
            {
                xlsxWriter.WriteData(startRow, i + 1, serialPacks[i].particleConsumptionSerial.docFieldName);
            }

            startRow++;
            for (int i = 0; i < serialPacks.Count; i++)
            {
                if (serialPacks[i].ProfilerRecommendedValue != null)
                {
                    xlsxWriter.WriteData(startRow, i + 1, serialPacks[i].ProfilerRecommendedValue.RecommendedValue);
                }

            }

            // int i = 0;
            // for (int i = 0; i < datas.Count; i++)
            using (var iter = datas.GetEnumerator())
            {
                while (iter.MoveNext() && iter.Current != null)
                {
                    T data = iter.Current;
                    // T data = datas[i];
                    startRow++;
                    for (int j = 0; j < serialPacks.Count; j++)
                    {
                        var val = serialPacks[j].fieldInfo.GetValue(data);
                        if (serialPacks[j].convertString != null)
                        {
                            val = ConvertToString((int) val, serialPacks[j].convertString);
                        }

                        xlsxWriter.WriteData(0 + startRow, j + 1, val);
                    }

                    data.OnBeforeSave(ref startRow, xlsxWriter);

                    // i++;
                }
            }

            xlsxWriter.SaveData();
            UnityEngine.Debug.Log("保存成功 " + savePath);

            EditorUtility.DisplayDialog("信息", "保存成功", "输出文件" + savePath);

        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"保存失败 {savePath} 写入异常: " + e);
        }
    }

    public class CommonProfilerSerialPack
    {
        public CommonProfilerBytesConvertString convertString;
        public FieldInfo fieldInfo;
        public CommonProfilerSerial particleConsumptionSerial;
        public CommonProfilerRecommendedValue ProfilerRecommendedValue;
    }


}
