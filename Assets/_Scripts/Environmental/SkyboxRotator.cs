using UnityEngine;

public class SkyboxRotator : MonoBehaviour
{
    [Header("Skybox Rotation")]
    public float rotationSpeed = 1f;

    float currentRotation;

    void Update()
    {
        // ====================================
        // ROTAR SKYBOX
        // ====================================

        currentRotation +=
            rotationSpeed *
            Time.deltaTime;

        // ====================================
        // LOOP INFINITO
        // ====================================

        if (currentRotation >= 360f)
        {
            currentRotation = 0f;
        }

        // ====================================
        // APLICAR ROTACIÓN
        // ====================================

        RenderSettings.skybox.SetFloat(
            "_Rotation",
            currentRotation
        );
    }
}