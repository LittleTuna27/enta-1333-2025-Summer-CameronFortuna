using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageFlash : MonoBehaviour
{
    [Header("Flash Settings")]
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField] private float flashDuration = 0.15f;

    private Renderer[] renderers;
    private Material[] originalMaterials;
    private Material[] flashMaterials;

    void Start()
    {
        // Get all renderers on this object and children
        renderers = GetComponentsInChildren<Renderer>();

        // Store original materials and create flash versions
        originalMaterials = new Material[renderers.Length];
        flashMaterials = new Material[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].material;
            flashMaterials[i] = new Material(originalMaterials[i]);
            flashMaterials[i].color = flashColor;
        }
    }

    public void Flash()
    {
        StartCoroutine(FlashCoroutine());
    }

    private IEnumerator FlashCoroutine()
    {
        // Switch to flash materials
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material = flashMaterials[i];
        }

        yield return new WaitForSeconds(flashDuration);

        // Switch back to original materials
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material = originalMaterials[i];
        }
    }
}
