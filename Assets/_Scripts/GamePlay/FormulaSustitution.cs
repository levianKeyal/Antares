using UnityEngine;
using TMPro;

public class FormulaSustitution : MonoBehaviour
{

    public FireCanonManager fManager;

    public TMP_Text range;
    public TMP_Text Vo;
    public TMP_Text angle;
    public TMP_Text gravity;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (fManager == null)
        {
            fManager = GetComponent<FireCanonManager>();
        }

        if (fManager == null)
        {
            fManager = FindFirstObjectByType<FireCanonManager>();
        }

        UpdateFormulaValues();
    }

    public void UpdateFormulaValues()
    {
        if (fManager == null)
        {
            return;
        }

        if (Vo != null)
        {
            Vo.text = FormatFloat(VoSquare());
        }

        if (angle != null)
        {
            angle.text = FormatFloat(CalculateSin());
        }

        if (gravity != null)
        {
            gravity.text = FormatFloat(fManager.gravity);
        }

        if (range != null)
        {
            range.text = FormatFloat(Range());
        }
    }

    float VoSquare()
    {
        if (fManager == null)
        {
            return 0f;
        }

        float voSquare = fManager.initialVelocity * fManager.initialVelocity;
        return voSquare;
    }

    float CalculateSin()
    {
        if (fManager == null)
        {
            return 0f;
        }

        float angleDegrees = fManager.currentAngle;
        float angleRadians = angleDegrees * Mathf.Deg2Rad;

        float sinDoubleAngle = Mathf.Sin(2f * angleRadians);

        return sinDoubleAngle;
    }

    float Range()
    {
        if (fManager == null || Mathf.Approximately(fManager.gravity, 0f))
        {
            return 0f;
        }

        float range = (VoSquare() * CalculateSin()) / fManager.gravity;
        return range;
    }

    string FormatFloat(float value)
    {
        GameSettings settings =
            GameSettings.Instance;

        if (settings == null)
        {
            return value.ToString();
        }

        int decimals =
            settings.decimals;

        ValidationMode mode =
            settings.validationMode;

        // ====================================
        // EXACT
        // ====================================

        if (
            mode ==
            ValidationMode.ExactOnly
        )
        {
            return value.ToString();
        }

        // ====================================
        // TRUNCATED
        // ====================================

        if (
            mode ==
            ValidationMode.Truncated
        )
        {
            float factor =
                Mathf.Pow(10, decimals);

            float truncated =
                (float)System.Math.Truncate(
                    value * factor
                ) / factor;

            return RemoveTrailingZeros(
                truncated,
                decimals
            );
        }

        // ====================================
        // CEIL / APPROX
        // ====================================

        if (
            mode ==
            ValidationMode.Ceil
        )
        {
            float rounded =
                (float)System.Math.Round(
                    value,
                    decimals
                );

            return RemoveTrailingZeros(
                rounded,
                decimals
            );
        }

        // ====================================
        // ALL
        // ====================================

        if (
            mode ==
            ValidationMode.All
        )
        {
            return value.ToString(
                "F" + decimals
            );
        }

        return RemoveTrailingZeros(
            value,
            decimals
        );
    }

    string RemoveTrailingZeros(
    float value,
    int maxDecimals
)
    {
        return value.ToString(
            "0." +
            new string('#', maxDecimals)
        );
    }
}
