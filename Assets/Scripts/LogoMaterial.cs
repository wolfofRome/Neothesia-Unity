using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LogoMaterial : MonoBehaviour
{
    public Material material;
    // Start is called before the first frame update
    void Start()
    {
        Image image = GetComponent<Image>();
        image.material = material;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
