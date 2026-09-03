using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// L'issue d'un duel, et les deux seules choses qu'un joueur veut en faire.
///
/// <para>Modelé sur <see cref="GameOverController"/>, mais <b>pas lui</b> : celui-là écrit
/// « Vous avez été vaincu par … », faux sur une victoire, et son bouton appelle
/// <c>GrantRunEndUnlocks(false)</c> puis <c>OnRunEnd()</c> — ce qui mettrait fin à la run
/// PvE d'un joueur qui vient seulement de perdre un duel. Un duel ne termine rien d'autre
/// que lui-même : ce contrôleur referme la session PvP et rend la main au menu
/// multijoueur, sans toucher à la run.</para>
///
/// <para><b>Le match nul a sa propre ligne</b>, il ne se lit pas comme une défaite. En PvE
/// un nul ferme la run, donc l'assimiler à une défaite y disait quelque chose de vrai ;
/// en duel un nul n'a aucune conséquence, et l'annoncer comme une défaite serait
/// simplement faux. Pour revenir dessus, il n'y a qu'un endroit : le <c>switch</c> de
/// <see cref="Show"/>.</para>
/// </summary>
public class PvpResultController : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI reasonText;
    /// Ce que le duel a rapporté ou coûté. Facultatif : sans lui, l'issue s'affiche seule.
    public TextMeshProUGUI rewardText;
    public CanvasGroup canvasGroup;

    void Awake()
    {
        Hide();
    }

    void Hide()
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void Show(TeamOutcome outcome, string opponentName)
    {
        Show(outcome, opponentName, null);
    }

    /// <param name="reward">
    /// Le classement et les jetons que ce duel a valus, déjà mis en phrase, ou null quand il n'y
    /// a rien à annoncer — un duel amical, un raid, ou un serveur qui n'a rien renvoyé.
    /// </param>
    public void Show(TeamOutcome outcome, string opponentName, string reward)
    {
        string against = string.IsNullOrWhiteSpace(opponentName) ? "" : $" contre {opponentName}";
        string title;
        string reason;
        switch (outcome)
        {
            case TeamOutcome.Victory:
                title = "Victoire !";
                reason = $"Vous avez gagné le duel{against}.";
                break;
            case TeamOutcome.Defeat:
                title = "Défaite";
                reason = $"Vous avez perdu le duel{against}.";
                break;
            case TeamOutcome.Draw:
                title = "Match nul";
                reason = $"Le duel{against} se termine sans vainqueur.";
                break;
            default:
                title = "Duel terminé";
                reason = $"Le duel{against} est terminé.";
                break;
        }

        if (titleText != null)
            titleText.text = title;
        if (reasonText != null)
            reasonText.text = reason;

        if (rewardText != null)
        {
            rewardText.text = reward ?? string.Empty;
            rewardText.gameObject.SetActive(!string.IsNullOrWhiteSpace(reward));
        }

        if (canvasGroup != null)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            StartCoroutine(FadeIn());
        }
    }

    IEnumerator FadeIn()
    {
        float duration = 0.6f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    /// « Revanche » : on referme la bataille, on demande au menu de relancer une recherche
    /// dès qu'il s'ouvre, et on y retourne. Il n'existe pas d'endpoint de revanche côté
    /// serveur — c'est donc un nouveau matchmaking, pas un rematch contre le même joueur.
    public void Rematch()
    {
        LeaveBattle(true);
    }

    public void ToMenu()
    {
        LeaveBattle(false);
    }

    void LeaveBattle(bool queueAnotherMatch)
    {
        if (RunManager.Instance != null)
        {
            RunManager.Instance.EndPvpBattle();
            RunManager.Instance.requestPvpQuickMatch = queueAnotherMatch;
        }

        STSSceneLoader.Instance?.LoadScene("STS_MultiplayerMenu");
    }
}
