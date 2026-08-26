/// <summary>
/// Qui décide des effets : le serveur, ou le moteur local d'Unity.
///
/// <para>Un seul point de décision, volontairement. Le moteur local reste pour le bac
/// à sable et le tutoriel, qui n'ont aucun serveur à interroger ; le jour où ces modes
/// disparaîtront, retirer l'autorité serveur sera un retrait, pas une fouille.</para>
///
/// <para>Il vit dans STS.RunResume, l'assembly de logique pure — sans UnityEngine, donc
/// atteignable par les tests EditMode, qui ne voient pas Assembly-CSharp. C'est déjà là
/// que se trouvent STSRunResumeResolver, STSHealing et STSRestState, pour la même
/// raison.</para>
/// </summary>
public static class STSServerAuthority
{
    public static bool Decides(string runId) => !string.IsNullOrWhiteSpace(runId);
}
