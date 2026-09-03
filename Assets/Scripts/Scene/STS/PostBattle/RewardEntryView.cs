using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// Ce que le serveur a répondu quand on lui a réclamé une récompense.
/// </summary>
/// <remarks>
/// <para>Une carte gagnée en combat naît deux fois : le serveur l'inscrit à son deck et lui
/// donne son identifiant d'instance, puis le client inscrit la sienne. Tant que le client s'en
/// inventait un, les deux decks cessaient de désigner la même carte — le feu de camp la
/// proposait à l'enchantement, le serveur ne la trouvait pas dans le deck, et le panneau se
/// refermait sans rien enchanter ni consommer de charge.</para>
///
/// <para>La réclamation rapporte donc la carte telle que le serveur l'a inscrite, pour que le
/// client recopie celle-là au lieu d'en forger une.</para>
/// </remarks>
public readonly struct RewardClaim
{
    RewardClaim(bool accepted, CardInstance grantedCard)
    {
        Accepted = accepted;
        GrantedCard = grantedCard;
    }

    /// <summary>Le gain est acquis : le serveur a dit oui, ou il n'y avait pas de serveur.</summary>
    public bool Accepted { get; }

    /// <summary>
    /// La carte que le serveur vient d'ajouter à son deck, identifiant d'instance compris.
    /// Vaut <c>null</c> hors ligne, ou quand la récompense n'était pas une carte.
    /// </summary>
    public CardInstance GrantedCard { get; }

    /// <summary>Le serveur a refusé : rien n'a été gagné.</summary>
    public static readonly RewardClaim Refused = new RewardClaim(false, null);

    /// <summary>Personne à consulter : c'est au client d'inscrire le gain lui-même.</summary>
    public static readonly RewardClaim Local = new RewardClaim(true, null);

    public static RewardClaim Granted(CardInstance card) => new RewardClaim(true, card);
}

public interface IRewardFlowHost
{
    void NotifyClaimed(RewardEntryView entry);
    Task<RewardClaim> TryClaimServerRewardAsync(RewardItem rewardItem, string selectedCardId = null);
}

public abstract class RewardEntryView : MonoBehaviour
{
    protected RewardItem item;
    protected IRewardFlowHost manager;

    public virtual void Init(RewardItem rewardItem, IRewardFlowHost mgr)
    {
        item = rewardItem;
        manager = mgr;
    }

    protected IEnumerator Collapse()
    {
        CanvasGroup cg = GetComponent<CanvasGroup>();
        LayoutElement le = GetComponent<LayoutElement>();

        float t = 0;
        float duration = 0.25f;

        float startHeight = le.preferredHeight;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = t / duration;

            cg.alpha = 1 - k;
            le.preferredHeight = Mathf.Lerp(startHeight, 0, k);

            yield return null;
        }

        manager.NotifyClaimed(this);

        Destroy(gameObject);
    }
}