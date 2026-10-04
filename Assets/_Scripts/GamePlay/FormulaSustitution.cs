using UnityEngine;
using TMPro;

public class FormulaSustitution : MonoBehaviour
{

    public FireCanonManager fManager;

    public TMP_Text range;
    public TMP_Text Vo;
    public TMP_Text angle;
    public TMP_Text gravity;

    [Header("Total Flying Time Formula")]
    public TMP_Text totalFlyingTimeResult;
    public TMP_Text totalFlyingTimeInitialVelocity;
    public TMP_Text totalFlyingTimeSinAngle;
    public TMP_Text totalFlyingTimeGravity;

    [Header("Time To Max Height Formula")]
    public TMP_Text timeToMaxHeightResult;
    public TMP_Text timeToMaxHeightInitialVelocity;
    public TMP_Text timeToMaxHeightSinAngle;
    public TMP_Text timeToMaxHeightGravity;
    
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

        if (fManager.physicsMode == CannonPhysicsMode.SolveTotalFlyingTime)
        {
            UpdateTotalFlyingTimeFormulaValues();
        }

        if (fManager.physicsMode == CannonPhysicsMode.SolveTimeToMaxHeigth)
        {
            UpdateTimeToMaxHeightFormulaValues();
        }
    }

    void UpdateTotalFlyingTimeFormulaValues()
    {
        if (totalFlyingTimeResult != null)
        {
            totalFlyingTimeResult.text = "T";
        }

        if (totalFlyingTimeInitialVelocity != null)
        {
            totalFlyingTimeInitialVelocity.text =
                FormatFloat(fManager.challengeInitialVelocity);
        }

        if (totalFlyingTimeSinAngle != null)
        {
            totalFlyingTimeSinAngle.text =
                FormatFloat(CalculateTotalFlyingTimeSinAngle());
        }

        if (totalFlyingTimeGravity != null)
        {
            totalFlyingTimeGravity.text =
                FormatFloat(fManager.gravity);
        }
    }

    void UpdateTimeToMaxHeightFormulaValues()
    {
        if (timeToMaxHeightResult != null)
        {
            timeToMaxHeightResult.text = "t<sub>hmax</sub>";
        }

        if (timeToMaxHeightInitialVelocity != null)
        {
            timeToMaxHeightInitialVelocity.text =
                FormatFloat(fManager.challengeInitialVelocity);
        }

        if (timeToMaxHeightSinAngle != null)
        {
            timeToMaxHeightSinAngle.text =
                FormatFloat(CalculateTimeToMaxHeightSinAngle());
        }

        if (timeToMaxHeightGravity != null)
        {
            timeToMaxHeightGravity.text =
                FormatFloat(fManager.gravity);
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

    float CalculateTotalFlyingTimeSinAngle()
    {
        if (fManager == null)
        {
            return 0f;
        }

        float angleRadians =
            fManager.challengeAngle *
            Mathf.Deg2Rad;

        return Mathf.Sin(angleRadians);
    }

    float CalculateTimeToMaxHeightSinAngle()
    {
        if (fManager == null)
        {
            return 0f;
        }

        float angleRadians =
            fManager.challengeAngle *
            Mathf.Deg2Rad;

        return Mathf.Sin(angleRadians);
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
        // CEIL
        // ====================================

        if (
            mode ==
            ValidationMode.Ceil
        )
        {
            float factor =
                Mathf.Pow(10, decimals);

            float ceil =
                value >= 0f
                    ? (float)System.Math.Ceiling(
                        value * factor
                    ) / factor
                    : (float)System.Math.Floor(
                        value * factor
                    ) / factor;

            return RemoveTrailingZeros(
                ceil,
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
