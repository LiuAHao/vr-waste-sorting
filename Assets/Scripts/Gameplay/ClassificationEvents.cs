using System;
using UnityEngine;

public static class ClassificationEvents
{
    public static event Action<ClassificationResult> OnClassified;

    public static void RaiseClassified(ClassificationResult result)
    {
        if (result == null)
        {
            Debug.LogWarning("ClassificationEvents.RaiseClassified: result is null, ignored.");
            return;
        }

        OnClassified?.Invoke(result);
    }
}
