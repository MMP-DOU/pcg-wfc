using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace scrips
{
    public class WFCmodel:MonoBehaviour
    {
        #region 单个原型的属性
        //原型属性的键
        public const string MESH_NAME = "mesh_name";
        public const string MESH_ROT = "mesh_rotation";
        public const string NEIGHBOR = "valid_neighbours";
        public const string CONSTRAINT_TO = "constrain_to";
        public const string CONSTRAINT_FROM = "constrain_from";
        public const string CONSTRAINT_TOP = "constrain_top";
        public const string CONSTRAINT_BOTTOM = "constrain_bottom";
        public const string WEIGHT = "weight";

        //方向到索引,用来访问邻居数组
        public const int pX = 0;
        public const int pZ = 1;
        public const int nX = 2;
        public const int nZ = 3;
        public const int pY = 4;
        public const int nY = 5;
        
        #endregion

        #region 重要变量

        /// <summary>
        /// 方向向量到索引的映射
        /// </summary>
        public Dictionary<Vector3Int, int> directionToIndex;
        /// <summary>
        /// 波函数容器
        /// x坐标<y坐标<z坐标<单元格字典<原型名,原型字典<原型名,属性>>>>>
        /// </summary>
        public List<List<List<Dictionary<string, Dictionary<string, object>>>>> wfc;
        
        /// <summary>
        /// 世界盒子体积
        /// </summary>
        public Vector3Int size;

        /// <summary>
        /// 待处理坐标的栈
        /// </summary>
        public Stack<Vector3Int> stack;
        public HashSet<Vector3Int> Instack;
        
        #endregion

        #region 初始化方法

        void Awake()
        {
            directionToIndex = new Dictionary<Vector3Int, int>();
            //之后可以通过方向直接转换成对应的方向索引，方便在一维的邻居列表里找到邻居（一般是六个面的邻居）
            directionToIndex[Vector3Int.right] = pX;
            directionToIndex[Vector3Int.left] = nX;
            directionToIndex[Vector3Int.up] = pY;
            directionToIndex[Vector3Int.down] = nY;
            directionToIndex[Vector3Int.forward] = pZ;
            directionToIndex[Vector3Int.back] = nZ;
            
            wfc = new List<List<List<Dictionary<string, Dictionary<string, object>>>>>();
            stack = new Stack<Vector3Int>();
            Instack = new HashSet<Vector3Int>();
        }
        
        /// <summary>
        /// 初始化波函数
        /// </summary>
        /// <param name="_size">世界空间大小</param>
        /// <param name="unit">完整的单元格所包含的所有原型数据</param>
        public void WFC_Init(Vector3Int _size,Dictionary<string,Dictionary<string, object>> unit)
        {
            size = _size;
            wfc.Clear();
            
            //好几把长啊
            for (int x = 0; x < size.x; x++)
            {
                //y轴列表
                List<List<Dictionary<string, Dictionary<string, object>>>> yList = new List<List<Dictionary<string, Dictionary<string, object>>>>();
                for (int y = 0; y < size.y; y++)
                {
                    //z轴列表
                    List<Dictionary<string, Dictionary<string, object>>> zList = new List<Dictionary<string, Dictionary<string, object>>>();
                    for (int z = 0; z < size.z; z++)
                    {
                     zList.Add(DeepCopyWFC(unit));   
                    }
                    yList.Add(zList);
                }
                wfc.Add(yList);//wfc本身相当于x轴列表
            }
        }

        #endregion

        #region WFC get方法

        /// <summary>
        /// 检查是否坍缩完毕（所有单元格原型=1）
        /// </summary>
        /// <returns></returns>
        public bool IsCollapse()
        {
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    for (int z = 0; z < size.z; z++)
                    {
                        if (wfc[x][y][z].Count > 1)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 获取指定坐标所有可能原型
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public Dictionary<string, Dictionary<string, object>> GetPossiblePrototype(Vector3Int pos)
        {
            
            return wfc[pos.x][pos.y][pos.z];
        }

        /// <summary>
        /// 获取当前坐标指定方向的有效邻居列表
        /// </summary>
        /// <param name="currentCoord"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public List<string> GetNeibourInDirection(Vector3Int currentCoord, Vector3Int dir)
        {
            List<string> validNeibour = new List<string>();
            Dictionary<string, Dictionary<string, object>> currentUnit = GetPossiblePrototype(currentCoord);
            int direction = directionToIndex[dir];

            foreach (var prototype in currentUnit)
            {
                var Neibour = (JArray)prototype.Value[NEIGHBOR];//邻居大全
                var NeibourOnDir = (JArray)Neibour[direction];//dir方向的邻居

                foreach (var n in NeibourOnDir)
                {
                    string neibourName = n.ToString();//列表是Json读取的，要把里面的邻居名改成字符串
                    if (!validNeibour.Contains(neibourName))
                    {
                        validNeibour.Add(neibourName);
                    }
                }
            }
            return validNeibour;
        }

        /// <summary>
        /// 获取该坐标单元格的熵
        /// </summary>
        /// <param name="coord"></param>
        /// <returns></returns>
        public int GetEntropy(Vector3Int coord)
        {
            return wfc[coord.x][coord.y][coord.z].Count;    
        }
        /// <summary>
        /// 获取熵最小的坐标
        /// </summary>
        /// <returns></returns>
        public Vector3Int GetCoordWithMinimumEntropy()
        {
            Vector3Int MinCoord = new Vector3Int();
            float MinEntropy = float.MaxValue;
            
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    for (int z = 0; z < size.z; z++)
                    {
                        int entropy = GetEntropy(new Vector3Int(x, y, z));
                        if (entropy > 1)
                        {
                            float entropyWithNoise = entropy + Random.Range(-0.1f, 0.1f);//添加一点点噪声，避免每次选相同的格子
                            if (entropyWithNoise < MinEntropy)
                            {
                                MinEntropy = entropyWithNoise;
                                MinCoord =  new Vector3Int(x, y, z);
                            }
                        }
                    }
                }
            }
            return MinCoord;
        }
        
        #endregion

        #region 主要逻辑

        /// <summary>
        /// 将指定坐标坍缩为特定原型
        /// </summary>
        /// <param name="coord"></param>
        /// <param name="prototypeName">原型名称</param>
        public void CollapseTo(Vector3Int coord,string prototypeName)
        {
            var prototype =  wfc[coord.x][coord.y][coord.z][prototypeName];
            wfc[coord.x][coord.y][coord.z].Clear();
            wfc[coord.x][coord.y][coord.z] = new Dictionary<string, Dictionary<string, object>>{{prototypeName,prototype}};
        }

        /// <summary>
        /// 指定坐标加权随机坍缩
        /// </summary>
        /// <param name="coord"></param>
        public void CollapseToRam(Vector3Int coord)
        {
            var prototypes =  wfc[coord.x][coord.y][coord.z];
            string prototypeName = SelectPrototypeWithPower(prototypes);
            CollapseTo(coord, prototypeName);
        }

        
        /// <summary>
        /// 加权随机选择，返回一个原型的名字
        /// </summary>
        /// <param name="prototypes"></param>
        /// <returns></returns>
        public string SelectPrototypeWithPower(Dictionary<string, Dictionary<string, object>> prototypes)
        {
            Dictionary<float,string> PowerToPrototype = new Dictionary<float, string>();
            foreach (var prototype in prototypes)
            {
                float power = System.Convert.ToSingle(prototype.Value[WEIGHT]) ;
                power += Random.Range(-0.1f, 0.1f); //添加小噪声
                PowerToPrototype[power] = prototype.Key;
            }

            List<float> powerList = PowerToPrototype.Keys.ToList();
            powerList.Sort();
            return PowerToPrototype[powerList[^1]];
        }

        /// <summary>
        /// 坍缩熵最小的单元格
        /// </summary>
        public void CollapseTheMinUnit()
        {
            CollapseToRam(GetCoordWithMinimumEntropy());
        }

        /// <summary>
        /// 移除指定坐标的指定原型
        /// </summary>
        /// <param name="coord"></param>
        /// <param name="prototypeName"></param>
        public void RemovePrototypeFromCoord(Vector3Int coord, string prototypeName)
        {
            wfc[coord.x][coord.y][coord.z].Remove(prototypeName);
        }

        /// <summary>
        /// 一次迭代
        /// (传播约束)
        /// </summary>
        public void Iterat()
        {
            Vector3Int coord = GetCoordWithMinimumEntropy();
            //CollapseToRam(coord);
            CollapseToRam(coord);
            Propagate(coord);
        }

        /// <summary>s
        /// 传播约束
        /// </summary>
        /// <param name="coord">起始坐标（可不填）</param>
        /// <param name="once">是否只迭代一次</param>
        public void Propagate(Vector3Int? coord, bool once= false)
        {
            if(coord.HasValue)
            {
                stack.Push(coord.Value);
                Instack.Add(coord.Value);
            }

            while (stack.Count > 0)
            {
                Vector3Int currentCoord = stack.Pop();
                Instack.Remove(currentCoord);
                var validDir = GetValidDirection(currentCoord);//获取有效方向列表，下面可以直接计算相邻坐标

                foreach (var dir in validDir)
                {
                    Vector3Int dirCoord = currentCoord + dir;
                    var dirPrototypes = new Dictionary<string,Dictionary<string,object>> (GetPossiblePrototype(dirCoord));
                    if(!dirPrototypes.Any()) continue;
                    var allowedPrototypes = GetNeibourInDirection(currentCoord, dir);

                    foreach (var prototype in dirPrototypes)
                    {
                        if (!allowedPrototypes.Contains(prototype.Key))
                        {
                            wfc[dirCoord.x][dirCoord.y][dirCoord.z].Remove(prototype.Key);
                            if (!Instack.Contains(dirCoord))
                            {
                                stack.Push(dirCoord);
                                Instack.Add(dirCoord);
                            }
                        }
                    }
                }
                if(once) break;
            }
        }

        #endregion
        
        #region 其他

        /// <summary>
        /// 深拷贝单元格数据
        /// </summary>
        /// <param name="unit">传入模板</param>
        /// <returns></returns>
        private Dictionary<string, Dictionary<string, object>> DeepCopyWFC(Dictionary<string, Dictionary<string, object>> unit)
        {
            Dictionary<string, Dictionary<string, object>> tempUnit = new Dictionary<string, Dictionary<string, object>>();
            foreach (var p in unit)//单元格里的每一个原型
            {
                Dictionary<string, object> temp_p = new Dictionary<string, object>();
                foreach (var av in p.Value)//原型里的每条属性
                {
                    if (av.Value is List<object> list)
                    {
                        temp_p[av.Key] = new List<object>(list);//列表浅拷贝
                    }
                    else
                    {
                        temp_p[av.Key] = av.Value;
                    }
                }
                tempUnit.Add(p.Key, temp_p);
            }
            return tempUnit;
        }

        /// <summary>
        /// 获取指定坐标的所有有效方向(返回列表)
        /// </summary>
        /// <param name="coord"></param>
        /// <returns></returns>
        public List<Vector3Int> GetValidDirection(Vector3Int coord)
        {
            List<Vector3Int>  validDirection = new List<Vector3Int>();
            if (coord.x < size.x - 1 ) validDirection.Add(Vector3Int.right);
            if (coord.x > 0 ) validDirection.Add(Vector3Int.left);
            if (coord.y  < size.y - 1 ) validDirection.Add(Vector3Int.up);
            if (coord.y > 0 ) validDirection.Add(Vector3Int.down);
            if (coord.z < size.z - 1 ) validDirection.Add(Vector3Int.forward);
            if (coord.z > 0 ) validDirection.Add(Vector3Int.back);
            return validDirection;
        }
        #endregion
    }
}