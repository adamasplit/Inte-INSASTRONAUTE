using System;
using Newtonsoft.Json.Linq;

/// <summary>
/// Lequel des combattants d'un état est celui que ce client pilote.
///
/// <para>Le client répondait <c>"player"</c>, en dur. C'est exact en PvE, où le serveur
/// nomme le joueur ainsi par convention, et faux en PvP, où les identifiants sont des
/// UUID d'utilisateur.</para>
///
/// <para>Deux règles, dans cet ordre. La première est l'identifiant qu'on nous donne —
/// la convention PvE, ou l'identifiant d'utilisateur connu du menu multijoueur — et elle
/// ne s'applique que si le snapshot le contient réellement : un identifiant attendu et
/// absent est une divergence, pas une réponse. La seconde est une propriété du protocole
/// plutôt qu'un champ : la projection PvP ne montre les cartes qu'au spectateur, et
/// réduit tout autre combattant à des compteurs. <b>Celui qui montre ses cartes pendant
/// qu'un autre les cache est donc celui qui regarde.</b> Cette règle refuse de conclure
/// dès qu'il y a plusieurs mains visibles, parce qu'en co-op la question n'aurait plus de
/// réponse unique.</para>
/// </summary>
public static class LocalCombatantResolver
{
    public static string Resolve(JToken combatToken, string preferredCombatantId)
    {
        if (!(combatToken is JObject combat) || !(combat["combatants"] is JArray combatants))
            return null;

        if (!string.IsNullOrWhiteSpace(preferredCombatantId))
        {
            foreach (JToken combatantToken in combatants)
            {
                if (string.Equals(
                        combatantToken?.Value<string>("combatantId"),
                        preferredCombatantId,
                        StringComparison.Ordinal))
                {
                    return preferredCombatantId;
                }
            }
        }

        string onlyVisible = null;
        int visibleCount = 0;
        bool anyHidden = false;

        foreach (JToken combatantToken in combatants)
        {
            if (!(combatantToken is JObject combatant))
                continue;

            string combatantId = combatant.Value<string>("combatantId");
            if (string.IsNullOrWhiteSpace(combatantId))
                continue;

            if (combatant["hiddenPiles"] is JObject)
                anyHidden = true;

            if (combatant["piles"] is JObject)
            {
                visibleCount++;
                onlyVisible = combatantId;
            }
        }

        return anyHidden && visibleCount == 1 ? onlyVisible : null;
    }
}
