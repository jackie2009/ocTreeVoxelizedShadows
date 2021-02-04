using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class BOcTree : MonoBehaviour
{

    class Node
    {
        public int level;
        public int x;
        public int y;
        public int z;
        public int size;
        public int flag;// 0 node 1 shadowdata  
        public Node parent;
        public Node linkTo;
        public Node[] children;
        public int index;
        public int vicull;
        static int3[] OffsetNodeList = {
            new int3() { x = 0, y = 0,z = 0 }, new int3() { x = 1, y = 0, z = 0 }, new int3() { x = 0, y = 1, z = 0 }, new int3() { x = 1, y = 1, z = 0 },
            new int3() { x = 0, y = 0,z = 1 }, new int3() { x = 1, y = 0, z = 1 }, new int3() { x = 0, y = 1, z = 1 }, new int3() { x = 1, y = 1, z = 1 },
        };
        internal void insert(int vx, int vy, int vz,int vicull)
        {

            if (size <= 1) { flag = 1; this.vicull = vicull; return; }
            if (children == null)
            {
                children = new Node[8];
                for (int i = 0; i < 8; i++)
                {
                    children[i] = new Node()
                    {
                        x = x + OffsetNodeList[i].x * size / 2,
                        y = y + OffsetNodeList[i].y * size / 2,
                        z = z + OffsetNodeList[i].z * size / 2,
                        size = size / 2,
                        level = level + 1,
                        parent = this
                    };
                }
            }
            int offset = 0;
            if (vx > x + size / 2) offset++;
            if (vy > y + size / 2) offset += 2;
            if (vz > z + size / 2) offset += 4;
            children[offset].insert(vx, vy, vz, vicull);
        }

        public void draw(float drawScale, Vector3 wposOffset,Matrix4x4 l2w,bool useLightSpace, int commSubNodeOffsetX,int commSubNodeOffsetY, int commSubNodeOffsetZ)
        {
            var _children = children;
            if (linkTo != null)
            {
                _children = linkTo.children;

            }
            if (_children == null)
            {
                Gizmos.color = flag == 1 ? Color.red : Color.green;
                Vector3 wpos;
              if (useLightSpace)
                    wpos = l2w.MultiplyPoint3x4(new Vector3(x+ commSubNodeOffsetX - 60/ drawScale, y+ commSubNodeOffsetY - 60/ drawScale, z+ commSubNodeOffsetZ) *drawScale);
              else
                wpos = new Vector3(x+ commSubNodeOffsetX , y+ commSubNodeOffsetY , z+ commSubNodeOffsetZ) *drawScale;
                if (flag == 1)
                    Gizmos.DrawWireCube(wpos + wposOffset + new Vector3(size, size, size) * 0.5f * drawScale,
                        new Vector3(size, size, size) * drawScale);
            }
            //	if(flag==1)
            //print("level:" + level + ",size:" + size  +",x:"+x+","+",z:"+z);
            if (_children != null)
            {
                if (linkTo != null)
                {
                    commSubNodeOffsetX += x - linkTo.x;
                    commSubNodeOffsetY += y - linkTo.y;
                    commSubNodeOffsetZ += z - linkTo.z;
                }
                foreach (var item in _children)
                {
                    item.draw(drawScale, wposOffset,l2w, useLightSpace, commSubNodeOffsetX, commSubNodeOffsetY, commSubNodeOffsetZ);
                }
            }
        }
        private void calCount(ref int count0, ref int count1)
        {
            if (flag == 0)
                count0++;
            else
                count1++;
            if (children != null)
            {
                foreach (var item in children)
                {
                    item.calCount(ref count0, ref count1);
                }
            }
        }
        public void clipSameNode(bool recursion, bool compressHiddenVox)
        {
            if (children == null) return;

            if (compressHiddenVox == false) { 
                int dataCount = 0;
            foreach (var item in children)
            {
                if (item.flag == 1)
                {
                    dataCount++;
                }
            }

            if (dataCount == 8)
            {
                if (flag != 1)
                {
                    flag = 1;
                       
                    children = null;
                    if (parent != null) parent.clipSameNode(false, compressHiddenVox);
                }
            }
            else if (recursion)
            {
                foreach (var item in children)
                {
                    item.clipSameNode(true, compressHiddenVox);
                }
            }
        }else{
                int dataCount = 0;
                int emptyCount = 0;
                int vicullCount = 0;
                foreach (var item in children)
                {
                    if (item.vicull == 1) vicullCount++;
                    else
                    {
                        if (item.flag == 1)
                        {
                            dataCount++;

                        }
                        else if (item.children == null)
                        {
                            emptyCount++;

                        }
                    }
                }

                if (dataCount == 8 - vicullCount || emptyCount == 8 - vicullCount)

                {

                    flag = dataCount == 8 - vicullCount ? 1 : 0;
                    vicull = vicullCount == 8 ? 1 : 0;
                    children = null;
                    if (parent != null) parent.clipSameNode(false, compressHiddenVox);

                }
                else if (recursion)
                {
                    foreach (var item in children)
                    {
                        item.clipSameNode(true, compressHiddenVox);
                    }
                }

            }
            


        }
        public void compressCommSubNode()
        {
            if (children == null) return;
            List<Node> currentLevels = new List<Node>();
            currentLevels.AddRange(children);
            compressCommSubNode(currentLevels);
        }

        private void compressCommSubNode(List<Node> currentLevels)
        {
            List<Node> nextLevels = new List<Node>();

            for (int i = 0, len = currentLevels.Count; i < len; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    var item = currentLevels[j];
                    if (item.linkTo != null) continue;

                    if (currentLevels[i].equalSubNodes(item))
                    {
                        currentLevels[i].linkTo = item;
                        currentLevels[i].children = null;
                        break;
                    }

                }
                if (currentLevels[i].children != null) nextLevels.AddRange(currentLevels[i].children);

            }
            if (nextLevels.Count > 0)
                compressCommSubNode(nextLevels);
        }
        public bool equalSubNodes(Node target)
        {
            if (flag != target.flag) return false;
            if (size != target.size) return false;

            if (children == null && target.children == null) return true;
            if (children == null && target.children != null) return false;
            if (children != null && target.children == null) return false;
            for (int i = 0; i < 8; i++)
            {
                if (!children[i].equalSubNodes(target.children[i])) return false;
            }
            return true;

        }
        public LinkedList<Node> getAllNodes()
        {

            LinkedList<Node> nodes = new LinkedList<Node>();
            var current = nodes.AddLast(this);

            int addedCount = 1;
            while (addedCount > 0)
            {
                int loopCount = addedCount;
                addedCount = 0;
                for (int i = 0; i < loopCount; i++)
                {
                    if (current.Value.children != null)
                    {
                        foreach (var item in current.Value.children)
                        {
                            nodes.AddLast(item);
                            addedCount++;
                        }

                    }
                    current = current.Next;
                }

            }

            return nodes;

        }
        public void getAllNodes(List<Node> nodes)
        {


            nodes.Add(this);
            if (children != null )
            {
                for (int i = 0; i < 8; i++)
                {
                    children[i].getAllNodes(nodes);
                }
            }

        }
  
        public void printCount()
        {
            int count0 = 0;
            int count1 = 0;
            calCount(ref count0, ref count1);
            print(count0);
            print(count1);
        }

        public bool find(int fx, int fy, int fz)
        {
            if (children == null)
            {
                return flag == 1;
            }
            int offset = 0;
            if (fx > x + size / 2) offset++;
            if (fy > y + size / 2) offset += 2;
            if (fz > z + size / 2) offset += 4;
            return children[offset].find(fx, fy, fz);
        }
    }

    public Texture2D tex;
    public Light light;

 
     public bool debugCellMode = false;
    public ShadowDataGenerate shadowDataGenerate;
    public Vector3 wposOffset;
    public int unitsPerMeter = 1;


    [Header("压缩物体内部的区域")]
    public bool compressHiddenVox;

    [Header("合并公共子节点")]
    public bool commSubNode;
    public bool useLightSpace;
    // Use this for initialization
    void Start()
    {
      
       StartCoroutine( shadowDataGenerate.getShadowData(1000 * unitsPerMeter, 20 * unitsPerMeter, 1000 * unitsPerMeter, wposOffset, unitsPerMeter,useLightSpace, onShadowDataFinish));
        
    }
    Node root;
   
    private void onShadowDataFinish(LinkedList<BOcTree.int3>  shaodws)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
  

        
        stopwatch.Stop();
        print("getshadows time:" + stopwatch.ElapsedMilliseconds);
        stopwatch.Reset();
        stopwatch.Start();
        GetComponent<Camera>().depthTextureMode = DepthTextureMode.Depth;
        root = new Node();
        root.x = 0;
        root.z = 0;
        root.size = 128 * unitsPerMeter;

        int insertCount = 0;
        foreach (var item in shaodws)
        {
            root.insert(item.x, item.y, item.z,item.vicull);
            if (insertCount++ > 500000)
            {
                root.clipSameNode(true, compressHiddenVox);
                insertCount = 0;
                GC.Collect();
            }
        }
        print("insert complete" );
        
        shaodws.Clear();
        GC.Collect();
        root.clipSameNode(true, compressHiddenVox);
        GC.Collect();
        if (commSubNode)
        {
            root.compressCommSubNode();
        }
        GC.Collect();
        root.printCount();
        LinkedList<Node> nodes =  root.getAllNodes();
        print("nodes count:" + nodes.Count);
        stopwatch.Stop();
        print("make nodes time:" + stopwatch.ElapsedMilliseconds);
        stopwatch.Reset();
        stopwatch.Start();
        //   print(nodes.Count);
        int width = Mathf.CeilToInt(Mathf.Sqrt(nodes.Count));


        tex = new Texture2D(width, width, TextureFormat.RGBAFloat, false, true);//其实 rfloat 就可以 但为了不想看到报错 就用rgba
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        var colors = tex.GetPixels();
        //print("------");
       
        int itemIndex = 0;
        foreach (var item in nodes)
        {
            item.index = itemIndex++;
        }
        //for (int i = 0; i < nodes.Count; i++)
        //{
        //    nodes[i].index = i;
        //    if (nodes[i].children != null) {
        //       // if (nodes[i].x != nodes[i].children[0].x || nodes[i].y != nodes[i].children[0].y || nodes[i].z != nodes[i].children[0].z) errCount++;
        //    }
        //}
        int i = 0;
        foreach (var item in nodes)
        {
            colors[i].r = item.x;
            colors[i].g = item.y;
            colors[i].b = item.z;
            if (item.linkTo == null)
            {
                colors[i].a = item.children != null ? item.children[0].index : 0;
            }
            else
            {
                colors[i].a = item.linkTo.children != null ? item.linkTo.children[0].index : 0;
                item.flag += 2;
            }

            colors[i].a = colors[i].a * 10 + item.flag;

            i++;
        }
        //for (int i = 0; i < nodes.Count; i++)
        //{
        //    // print(nodes[i].Item);
        //    colors[i].r = nodes[i].x;
        //    colors[i].g = nodes[i].y;
        //    colors[i].b = nodes[i].z;
        //    if (nodes[i].linkTo == null)
        //    {
        //        colors[i].a = nodes[i].children != null ? nodes[i].children[0].index : 0;
        //    }
        //    else
        //    {
        //        colors[i].a = nodes[i].linkTo.children != null ? nodes[i].linkTo.children[0].index : 0;
        //        nodes[i].flag += 2;
        //    }

        //    colors[i].a = colors[i].a * 10 + nodes[i].flag;

        //}
        tex.SetPixels(colors);
        tex.Apply();
        GC.Collect();
        Shader.SetGlobalTexture("_OTreeTex", tex);
        Shader.SetGlobalInt("_OTreeWidth", width);
        Shader.SetGlobalInt("_unitsPerMeter", unitsPerMeter);
        Shader.SetGlobalVector("_wposOffset", wposOffset);
        Shader.SetGlobalMatrix("_World2Light", light.transform.worldToLocalMatrix);
        Shader.SetGlobalInt("_useLightSpace", useLightSpace?1:0);
        stopwatch.Stop();
        print("make tex time:" + stopwatch.ElapsedMilliseconds);
    }
    public Transform testObj;
    // Update is called once per frame
    void OnDrawGizmos()
    {

        if (root == null) return;
        if (Application.isPlaying ==false) return;

        if (testObj != null)
        {
            var wpos = testObj.position;
            int x = (int)(wpos.x * unitsPerMeter + 0.5f);
            int y = (int)(wpos.y * unitsPerMeter + 0.5f);
            int z = (int)(wpos.z * unitsPerMeter + 0.5f);
            //testObj.localScale = new Vector3(1, root.find(x, y, z) ? 2 : 1, 1);
            testObj.localScale = new Vector3(1, shadowValue(wpos)==0 ? 2 : 1, 1);
        }
        if (debugCellMode == false) return;
        float drawScale = 1f / unitsPerMeter;
        root.draw(drawScale, wposOffset, light.transform.localToWorldMatrix, useLightSpace, 0,0,0);
    }
    Vector4 getTreeValue(int index)
    {

        return tex.GetPixel(index % tex.width, index / tex.width); 
    }
    float shadowValue(Vector3 wpos)
    {

        /*   wpos = mul(_World2Light, float4(wpos, 1));

         uint x = wpos.x * _unitsPerMeter + 0.5+60* _unitsPerMeter;
         uint y = wpos.y * _unitsPerMeter + 0.5+60* _unitsPerMeter;
         uint z = wpos.z * _unitsPerMeter + 0.5;*/

        int x = (int)(wpos.x * unitsPerMeter + 0.5f);
        int y = (int)(wpos.y * unitsPerMeter + 0.5);
        int z = (int)(wpos.z * unitsPerMeter + 0.5);

        int index = 0;
        int size = 128 * unitsPerMeter;

        while (true)
        {
            print(index);
            Vector4 node = getTreeValue(index);
            int flag = (int)node.w % 10;
            node.w = (int)node.w / 10;

            if (node.w == 0)
            {
                print(flag);
                print(size);
                return 1 - flag;
            }
            if (size == 1)
            {
                print("size==1," + flag);
                print("size==1," + node.w);
                print("size==1," + getTreeValue(index).w);
                return 1;
            }
            int childIndex = 0;

            if (x > (int)node.x + size / 2)
            {
                childIndex++;
            }
            if (y > (int)node.y + size / 2)
            {
                childIndex += 2;
            }
            if (z > (int)node.z + size / 2)
            {
                childIndex += 4;
            }
            index = (int)node.w + childIndex;
            size /= 2;

        }

        return 1;
    }
    //utils 
    public struct int3
    {
        public short vicull;
        public short x, y, z;
         
        public override string ToString()
        {
            return x + "," + y + "," + z;
        }
    }
    

}