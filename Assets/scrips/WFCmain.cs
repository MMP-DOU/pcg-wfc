using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using scrips;
using UnityEngine;

public class WFCmain : MonoBehaviour
{
    [SerializeField]private Vector3Int size;//网格尺寸
    [SerializeField]private int unitSize = 1;//单元格大小

    [SerializeField]private string meshPath = "GameRes/{0}";//资源路径
    [SerializeField]private string jsonPath = "GameRes/Json/{0}";//原型Json路径
    
    [SerializeField]private GameObject meshPrefab;//模型预制体
    [SerializeField]private WFCmodel wfcmodel;//wfc模型组件
    [SerializeField] private int seed;//wfc种子
    private List<GameObject> meshes = new List<GameObject>();//所有网格的实例
    private Vector3Int currentCoord;//当前坐标
    public bool UpdateWithEachFrame = false;
    
    
    
    void Start()
    {
        Test();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    #region 主要逻辑

    /// <summary>
    /// 初始化wfc并生成网格
    /// </summary>
    private void Test()
    {
        ClearMesh();
        Random.InitState(seed.GetHashCode());
        //加载原型数据包
        var prototypes = new Dictionary<string, Dictionary<string, object>>();
        prototypes = LoadPrototypesFromJson();
        
        if (wfcmodel != null)
        {
            Destroy(wfcmodel.gameObject);
        }

        
        GameObject WfcModel = new GameObject("WfcModel");
        WfcModel.transform.SetParent(transform);
        WfcModel.transform.localPosition = Vector3.zero;
        wfcmodel =  WfcModel.AddComponent<WFCmodel>();
        wfcmodel.WFC_Init(size,prototypes);
        // 应用自定义约束
        ApplyCustomConstraints();
        if (UpdateWithEachFrame)
        {
            StartCoroutine(UpdateEveryFrame());
        }
        else
        {
            FuckingUpdate();
        }
    }

    #endregion

    #region 自定义约束

    public void ApplyCustomConstraints()
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int z = 0; z < size.z; z++)
                {
                    Vector3Int coord = new Vector3Int(x, y, z);
                    var prototypes = wfcmodel.GetPossiblePrototype(coord);

                    //约束顶层，上面的邻居没有p-1的统统鲨了
                    if (y == size.y - 1)
                    {
                        List<string>RemoveList = new List<string>();
                        foreach (var prototype in prototypes)
                        {
                            var neighbors = (JArray)prototype.Value[WFCmodel.NEIGHBOR];
                            var neighborsOnPY = (JArray)neighbors[WFCmodel.pY];

                            if (!neighborsOnPY.Any(token => token.Value<string>() == "p-1"))
                            {
                                RemoveList.Add(prototype.Key);
                            }
                        }

                        foreach (var protoName in RemoveList)
                        {
                            Debug.Log($"清理{coord}顶层");
                            prototypes.Remove(protoName);
                            if (!wfcmodel.Instack.Contains(coord))
                            {
                                wfcmodel.stack.Push(coord);
                                wfcmodel.Instack.Add(coord);
                            }
                            Debug.Log("剩余"+prototypes.Count);
                        }
                    }
                    
                    //约束除了底部之外的所有层
                    if (y > 0)
                    {
                        List<string>RemoveList = new List<string>();
                        foreach (var prototype in prototypes)
                        {   
                            var constraints = prototype.Value[WFCmodel.CONSTRAINT_TO]?.ToString();
                            if (constraints == WFCmodel.CONSTRAINT_BOTTOM)
                            {
                                RemoveList.Add(prototype.Key);
                            }
                        }

                        foreach (var protoName in RemoveList)
                        {
                            Debug.Log($"清理{coord}底层之外所有层");
                            prototypes.Remove(protoName);
                            if (!wfcmodel.Instack.Contains(coord))
                            {
                                wfcmodel.stack.Push(coord);
                                wfcmodel.Instack.Add(coord);
                            }
                            Debug.Log("剩余"+prototypes.Count);
                        }
                    }
                    
                    //约束顶层以外的所有层
                    if (y<size.y - 1)
                    {
                        List<string>RemoveList = new List<string>();
                        foreach (var prototype in prototypes)
                        {   
                            var constraints = prototype.Value[WFCmodel.CONSTRAINT_TO]?.ToString();
                            if (constraints == WFCmodel.CONSTRAINT_TOP)
                            {
                                RemoveList.Add(prototype.Key);
                            }
                        }

                        foreach (var protoName in RemoveList)
                        {
                            Debug.Log($"清理{coord}顶层之外所有层");
                            prototypes.Remove(protoName);
                            if (!wfcmodel.Instack.Contains(coord))
                            {
                                wfcmodel.stack.Push(coord);
                                wfcmodel.Instack.Add(coord);
                            }
                            
                        }
                        Debug.Log("剩余"+prototypes.Count);
                    }
                    
                    //约束底层
                    if (y == 0)
                    {
                        List<string>RemoveList = new List<string>();
                        foreach (var prototype in prototypes)
                        {   
                            var neighbors = (JArray)prototype.Value[WFCmodel.NEIGHBOR];
                            var neighborsOnNY = (JArray)neighbors[WFCmodel.nY];
                            if (!neighborsOnNY.Any(token => token.Value<string>() == "p-1"))
                            {
                                RemoveList.Add(prototype.Key);
                            }
                        }

                        foreach (var protoName in RemoveList)
                        {
                            Debug.Log($"清理{coord}底层");
                            prototypes.Remove(protoName);
                            if (!wfcmodel.Instack.Contains(coord))
                            {
                                wfcmodel.stack.Push(coord);
                                wfcmodel.Instack.Add(coord);
                            }
                            
                        }
                        Debug.Log("剩余"+prototypes.Count);
                    }
                    
                    //约束x正方向
                    if (x == size.x-1)
                    {
                        List<string>RemoveList = new List<string>();
                        foreach (var prototype in prototypes)
                        {   
                            var neighbors = (JArray)prototype.Value[WFCmodel.NEIGHBOR];
                            var neighborsOnPX = (JArray)neighbors[WFCmodel.pX];
                            if (!neighborsOnPX.Any(token => token.Value<string>() == "p-1"))
                            {
                                RemoveList.Add(prototype.Key);
                            }
                        }

                        foreach (var protoName in RemoveList)
                        {
                            Debug.Log($"清理{coord}+x层");
                            prototypes.Remove(protoName);
                            if (!wfcmodel.Instack.Contains(coord))
                            {
                                wfcmodel.stack.Push(coord);
                                wfcmodel.Instack.Add(coord);
                            }
                            
                        }
                        Debug.Log("剩余"+prototypes.Count);
                    }
                    
                    //约束x负方向
                    if (x == 0)
                    {
                        List<string>RemoveList = new List<string>();
                        foreach (var prototype in prototypes)
                        {   
                            var neighbors = (JArray)prototype.Value[WFCmodel.NEIGHBOR];
                            var neighborsOnNX = (JArray)neighbors[WFCmodel.nX];
                            if (!neighborsOnNX.Any(token => token.Value<string>() == "p-1"))
                            {
                                RemoveList.Add(prototype.Key);
                            }
                        }

                        foreach (var protoName in RemoveList)
                        {
                            Debug.Log($"清理{coord}-x层");
                            prototypes.Remove(protoName);
                            if (!wfcmodel.Instack.Contains(coord))
                            {
                                wfcmodel.stack.Push(coord);
                                wfcmodel.Instack.Add(coord);
                            }
                            
                        }
                        Debug.Log("剩余"+prototypes.Count);
                    }
                    
                    //约束z正方向
                    if (z == size.z-1)
                    {
                        List<string>RemoveList = new List<string>();
                        foreach (var prototype in prototypes)
                        {   
                            var neighbors = (JArray)prototype.Value[WFCmodel.NEIGHBOR];
                            var neighborsOnPZ = (JArray)neighbors[WFCmodel.pZ];
                            if (!neighborsOnPZ.Any(token => token.Value<string>() == "p-1"))
                            {
                                RemoveList.Add(prototype.Key);
                            }
                        }

                        foreach (var protoName in RemoveList)
                        {
                            Debug.Log($"清理{protoName}+z层");
                            prototypes.Remove(protoName);
                            if (!wfcmodel.Instack.Contains(coord))
                            {
                                wfcmodel.stack.Push(coord);
                                wfcmodel.Instack.Add(coord);
                            }
                            
                        }
                        Debug.Log("剩余"+prototypes.Count);
                    }
                    
                    //约束z负方向
                    if (z == 0)
                    {
                        List<string>RemoveList = new List<string>();
                        foreach (var prototype in prototypes)
                        {   
                            var neighbors = (JArray)prototype.Value[WFCmodel.NEIGHBOR];
                            var neighborsOnNZ = (JArray)neighbors[WFCmodel.nZ];
                            if (!neighborsOnNZ.Any(token => token.Value<string>() == "p-1"))
                            {
                                RemoveList.Add(prototype.Key);
                            }
                        }

                        foreach (var protoName in RemoveList)
                        {
                            Debug.Log($"清理{protoName}-z层");
                            prototypes.Remove(protoName);
                            if (!wfcmodel.Instack.Contains(coord))
                            {
                                wfcmodel.stack.Push(coord);
                                wfcmodel.Instack.Add(coord);
                            }
                            
                        }
                        Debug.Log("剩余"+prototypes.Count);
                    }
                    
                    Debug.Log(prototypes.Count);
                }
            }
        }
        
        wfcmodel.Propagate(null,false);
    }

    #endregion
    #region 迭代方法

    /// <summary>
    /// 每帧可视化迭代
    /// </summary>
    /// <returns></returns>
    private IEnumerator UpdateEveryFrame()
    {
        int iterationCount = 0;
        while (!wfcmodel.IsCollapse())
        {
            wfcmodel.Iterat();
            iterationCount++;
            if (iterationCount > 2)
            {
                iterationCount = 0;
                ClearMesh();
                GenerateMesh();
                yield return null;
            }
            
        }
        ClearMesh();
        GenerateMesh();
    }

    /// <summary>
    /// 一次性生成
    /// </summary>
    private void FuckingUpdate()
    {
        while (!wfcmodel.IsCollapse())
        {
            wfcmodel.Iterat();
        }
        
        GenerateMesh();
        if (meshes.Count == 0)
        {
            seed++;
            if(seed == 7) return;
            Test();
        }
    }
    
    #endregion
    
    #region Json解析

    /// <summary>
    /// 从文件加载
    /// </summary>
    /// <returns></returns>
    private Dictionary<string, Dictionary<string, object>> LoadPrototypesFromJson()
    {
        Dictionary<string, Dictionary<string, object>> prototypes = new Dictionary<string, Dictionary<string, object>>();
        
        string path = string.Format(jsonPath,"prototype_data");
        TextAsset PrototypesText = Resources.Load<TextAsset>(path);
        string json = PrototypesText.text;
        
        return AnalysisPrototypeJson(json);
    }
    /// <summary>
    /// 解析Json
    /// </summary>
    /// <param name="jsonText"></param>
    /// <returns></returns>
    private Dictionary<string, Dictionary<string, object>> AnalysisPrototypeJson(string jsonText)
    {
        Dictionary<string, Dictionary<string, object>> prototypes = new Dictionary<string, Dictionary<string, object>>();
        prototypes = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(jsonText);
        return prototypes;
    }
    #endregion

    #region 网格生成/清理

    /// <summary>
    /// 生成网格
    /// </summary>
    private void GenerateMesh()
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int z = 0; z < size.z; z++)
                {
                    var prototypes = wfcmodel.GetPossiblePrototype(new Vector3Int(x, y, z));
                    foreach (var prototype in prototypes)
                    {
                        Dictionary<string, object> attribute = prototype.Value;
                        string meshName = attribute[WFCmodel.MESH_NAME]?.ToString();
                        float rot = System.Convert.ToInt32(attribute[WFCmodel.MESH_ROT]);
                        
                        if(meshName == "-1") continue;

                        GameObject meshprf = Instantiate(meshPrefab, this.transform);
                        meshes.Add(meshprf);
                        
                        //把网格整上
                        ModuleData moduleScrip = meshprf.GetComponent<ModuleData>();
                        if (moduleScrip != null)
                        {
                            string path = string.Format(meshPath,meshName);
                            Mesh mesh = Resources.Load<Mesh>(path);
                            moduleScrip.mesh = mesh;
                            Match match = Regex.Match(prototype.Value[WFCmodel.MESH_NAME].ToString(), @"wfc_module_(\d+)");//
                            int number = int.Parse(match.Groups[1].Value);
                            Material material = Resources.Load<Material>($"meterial/me {number}");
                            moduleScrip.SetMaterial(material);
                            moduleScrip.prototype = new Dictionary<string, Dictionary<string, object>> {{prototype.Key, prototype.Value}};
                        }
                        //设置位置和旋转
                        meshprf.transform.rotation = Quaternion.Euler(0,(90*rot),0);
                        meshprf.transform.position = new Vector3(x*unitSize,y*unitSize,z*unitSize);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 销毁所有网格
    /// </summary>
    private void ClearMesh()
    {
        foreach (var mesh in meshes)
        {
            if (mesh != null)
            {
                Destroy(mesh);
            }
        }
        meshes.Clear();
    }
    #endregion
}
