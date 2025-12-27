using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]

public class saturation : MonoBehaviour
{
    [Range(0f, 1f)]
    public float value;

    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private string saturationParam = "_Saturation";
    // Start is called before the first frame update
     MaterialPropertyBlock _mpb;
    int _satId;

    void OnEnable()
    {
        if (!targetRenderer) targetRenderer = GetComponent<Renderer>();
        _mpb ??= new MaterialPropertyBlock();
        _satId = Shader.PropertyToID(saturationParam);
        Apply();
    }

    public void SetValue(float v)
    {
        value = Mathf.Clamp01(v);
        //Debug.Log(value);
        Apply();
    }

    void Apply()
    {
        if (!targetRenderer) return;

        targetRenderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat(_satId, value);
        targetRenderer.SetPropertyBlock(_mpb);
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
