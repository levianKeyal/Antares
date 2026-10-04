using UnityEngine;

public sealed class StudentPerformanceTracker : MonoBehaviour
{
    [Header("Range")]
    [SerializeField] private StudentDataDefinition rangeAttempts;
    [SerializeField] private StudentDataDefinition rangeCorrectAnswers;

    [Header("Angle")]
    [SerializeField] private StudentDataDefinition angleAttempts;
    [SerializeField] private StudentDataDefinition angleCorrectAnswers;

    [Header("Velocity")]
    [SerializeField] private StudentDataDefinition velocityAttempts;
    [SerializeField] private StudentDataDefinition velocityCorrectAnswers;

    [Header("Total Flying Time")]
    [SerializeField] private StudentDataDefinition totalFlyingTimeAttempts;
    [SerializeField] private StudentDataDefinition totalFlyingTimeCorrectAnswers;

    [Header("Time To Max Height")]
    [SerializeField] private StudentDataDefinition timeToMaxHeightAttempts;
    [SerializeField] private StudentDataDefinition timeToMaxHeightCorrectAnswers;

    public bool RecordAnswer(StudentPerformanceCalculationType type, bool correct)
    {
        if (!StudentData.IsReady)
        {
            Debug.LogWarning($"[StudentPerformanceTracker] StudentData is not ready. Performance answer was not recorded. Type={type}");
            return false;
        }

        StudentDataDefinition attemptDefinition;
        StudentDataDefinition correctDefinition;
        ResolveDefinitions(type, out attemptDefinition, out correctDefinition);

        if (attemptDefinition == null)
        {
            Debug.LogWarning($"[StudentPerformanceTracker] Missing attempts definition for {type}.");
            return false;
        }

        if (correct && correctDefinition == null)
        {
            Debug.LogWarning($"[StudentPerformanceTracker] Missing correct answers definition for {type}.");
            return false;
        }

        bool attemptRecorded = StudentData.Increment(attemptDefinition, 1L);
        if (!attemptRecorded)
        {
            Debug.LogWarning($"[StudentPerformanceTracker] Could not increment attempts for {type}.");
            return false;
        }

        if (!correct)
        {
            return true;
        }

        bool correctRecorded = StudentData.Increment(correctDefinition, 1L);
        if (!correctRecorded)
        {
            Debug.LogWarning($"[StudentPerformanceTracker] Could not increment correct answers for {type}.");
            return false;
        }

        return true;
    }

    private void ResolveDefinitions(
        StudentPerformanceCalculationType type,
        out StudentDataDefinition attemptDefinition,
        out StudentDataDefinition correctDefinition
    )
    {
        switch (type)
        {
            case StudentPerformanceCalculationType.Range:
                attemptDefinition = rangeAttempts;
                correctDefinition = rangeCorrectAnswers;
                return;

            case StudentPerformanceCalculationType.Angle:
                attemptDefinition = angleAttempts;
                correctDefinition = angleCorrectAnswers;
                return;

            case StudentPerformanceCalculationType.Velocity:
                attemptDefinition = velocityAttempts;
                correctDefinition = velocityCorrectAnswers;
                return;

            case StudentPerformanceCalculationType.TotalFlyingTime:
                attemptDefinition = totalFlyingTimeAttempts;
                correctDefinition = totalFlyingTimeCorrectAnswers;
                return;

            case StudentPerformanceCalculationType.TimeToMaxHeight:
                attemptDefinition = timeToMaxHeightAttempts;
                correctDefinition = timeToMaxHeightCorrectAnswers;
                return;

            default:
                attemptDefinition = null;
                correctDefinition = null;
                return;
        }
    }
}
