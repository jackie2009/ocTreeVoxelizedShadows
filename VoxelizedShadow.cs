using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelizedShadow : MonoBehaviour {
	public Shader drawShadow;
	private Material drawMaterial;
	public RenderTexture rt_mask;
	// Use this for initialization
	void Start () {
		GetComponent<Camera>().depthTextureMode = DepthTextureMode.Depth;
		drawMaterial = new Material(drawShadow);

		rt_mask = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.R8);
		//rt_mask = RenderTexture.GetTemporary(8192, 8192, 0, RenderTextureFormat.R8);
		Shader.SetGlobalTexture("_TestQTreeMaskTex", rt_mask);
	}
	void OnRenderImage(RenderTexture src, RenderTexture dest)
	{
		Graphics.Blit(src, rt_mask, drawMaterial, 0);
		Graphics.Blit(src, dest, drawMaterial, 1);
	}
 
}
