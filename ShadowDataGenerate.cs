using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
 

[RequireComponent(typeof(Light))]
public class ShadowDataGenerate : MonoBehaviour
{

	public Shader copyDepth;
	private RenderTexture renderTexture;
	public ComputeShader calShadow;
	public int rtSize = 2048;
	// Use this for initialization
	void Start()
	{

	}

	//高版本 可用  computeShader.SetMatrix ; 代替 5.x 需要转数组
	float[] getFloatArrayFromMatrix(Matrix4x4 m) {
		return new float[] {
			m.m00,m.m10,m.m20,m.m30,
		   m.m01,m.m11,m.m21,m.m31,
			m.m02,m.m12,m.m22,m.m32,
			m.m03,m.m13,m.m23,m.m33
		};
	}


 
 
	public  IEnumerator getShadowData(int xLen, int yLen, int zLen, Vector3 offsetWpos, int unitsPerMeter,bool useLightSpace ,Action<LinkedList<BOcTree.int3>> onFinish)
	{

		//var list = new List<BOcTree.int3>();
		var list = new LinkedList<BOcTree.int3>();
		//UnLimitListItem<Int32> rootData=new UnLimitListItem<int>();
		//UnLimitListItem<Int32> curData= rootData;
		var cmr = GetComponent<Camera>();
		cmr.enabled = false;
		cmr.aspect = 1;

		int tempall=0;

		renderTexture = RenderTexture.GetTemporary(rtSize, rtSize, 24, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
		cmr.targetTexture = renderTexture;
		cmr.RenderWithShader(copyDepth, "");
		Texture2D texDepth = new Texture2D(rtSize, rtSize, TextureFormat.RGBAFloat, false,true);

		RenderTexture.active = renderTexture;
		texDepth.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
		texDepth.Apply();
		RenderTexture.active = null;
		var colors = texDepth.GetPixels();
	 
		var colorBuffer = new ComputeBuffer(colors.Length, 4*4);
		colorBuffer.SetData(colors);
		var bufferCount = new ComputeBuffer(1, 4);
		var shadowResultBuffer = new ComputeBuffer(1000000, 4*3);

		calShadow.SetBool("useLightSpace", useLightSpace);
		calShadow.SetInt("unitsPerMeter", unitsPerMeter);
		calShadow.SetInt("rtSize", rtSize);
		calShadow.SetBuffer(0, "bufferCount", bufferCount);
		calShadow.SetBuffer(0, "shadowResultBuffer", shadowResultBuffer);
		calShadow.SetBuffer(0, "colorBuffer", colorBuffer);
		//calShadow.SetTexture(0, "shadowmap", texDepth);
		calShadow.SetFloats("localToWorldMatrix",getFloatArrayFromMatrix( cmr.transform.localToWorldMatrix));
		calShadow.SetFloats("MatrixVP",getFloatArrayFromMatrix(GL.GetGPUProjectionMatrix(cmr.projectionMatrix, false) * cmr.worldToCameraMatrix));
		calShadow.SetFloats("MatrixV",getFloatArrayFromMatrix(cmr.worldToCameraMatrix));
	 
            int callCount = (int)(128* unitsPerMeter  / 8.0f + 0.9999999f); 
	
		int[] countData = new int[1];
		for (int k = 0; k < callCount; k++)
            {
                calShadow.SetInt("loopOffset",  k*8);
			countData[0] = 0;
			bufferCount.SetData(countData);
				calShadow.Dispatch(0, 128 * unitsPerMeter / 8, 128 * unitsPerMeter / 8, 1);
    
		bufferCount.GetData(countData); 
			tempall += countData[0];
		print("countData:"+ tempall);


			var shadowData = new int[countData[0] * 3];
			shadowResultBuffer.GetData(shadowData );
            for (int i = 0, len = countData[0]; i < len; i++)
            {
                int x = shadowData[i * 3];
                int y = shadowData[i * 3 + 1];
                int z = shadowData[i * 3 + 2];
				Vector3 wpos;
				if(useLightSpace)
				wpos= cmr.transform.localToWorldMatrix.MultiplyPoint3x4(new Vector3(x - 60 * unitsPerMeter, y - 60 * unitsPerMeter, z) / unitsPerMeter) + offsetWpos;
				else
					wpos =  new Vector3(x , y , z) / unitsPerMeter + offsetWpos;
				int vicull = Physics.CheckSphere(wpos, 0.001f) ? 1 : 0;
                list.AddLast(new BOcTree.int3() { x =(short) x, y = (short)y, z = (short)z, vicull = (short)vicull });
				


			}
	 
            yield return 0;
			
			}

      
       
	 


        
		onFinish(list);
		 
	}
	void OnDestroy()
	{
		GetComponent<Camera>().targetTexture = null;
		RenderTexture.ReleaseTemporary(renderTexture);
	}

}