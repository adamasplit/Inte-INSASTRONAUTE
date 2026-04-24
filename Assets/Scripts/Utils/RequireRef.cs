using UnityEngine;

public static class RequireRef
{
    /// <summary>
    /// Vérifie qu'une référence Inspector est assignée. Log une erreur visible pointant
    /// vers le GameObject fautif si elle est null. Retourne la valeur pour le chaînage.
    /// Usage dans Awake : myField = this.Require(myField, nameof(myField));
    /// </summary>
    public static T Require<T>(this MonoBehaviour host, T value, string fieldName) where T : class
    {
        if (value == null)
            Debug.LogError(
                $"[{host.GetType().Name}] Référence requise '{fieldName}' non assignée " +
                $"sur '{host.gameObject.name}'. Assignez-la dans l'Inspector.",
                host.gameObject
            );
        return value;
    }
}
