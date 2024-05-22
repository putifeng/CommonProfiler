
using Core.IO;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

public class UnityStatusProfiler : CommonProfilerInterface
{
    [ReadOnly] 
    [CommonProfilerSerial("运行时三角面片数量", 2)] 
    [CommonProfilerBytesConvertString(CommonProfilerBytesConvertString.ConvertType.Thousand)] 
    [CommonProfilerRecommendedValue("建议 < 150k")] 
    public int runTimeTriangles;

    [ReadOnly] 
    [CommonProfilerSerial("运行时Batches", 4)] 
    [CommonProfilerRecommendedValue("建议 < 200")] 
    public int batches;

    public bool CanWrite()
    {
        return true;
    }

    public void Clear()
    {
        runTimeTriangles = 0;
        batches = 0;
    }
    
    public void CalecurStatisics(GameObject gameObject)
    {
        this.runTimeTriangles = math.max(this.runTimeTriangles,UnityEditor.UnityStats.triangles);
        // this.runTimevertices = UnityEditor.UnityStats.vertices;
        this.batches =  math.max(this.batches,UnityEditor.UnityStats.batches);
    }

    public void OnBeforeSave(ref int startRow, XlsxWriter xlsxWriter)
    {
        
    }
}
