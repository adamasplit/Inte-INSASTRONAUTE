const { DataApi } = require("@unity-services/cloud-save-1.4");
const { CurrenciesApi } = require("@unity-services/economy-2.4");

const CURRENCY_ID = "TOKEN";
const betKey = (eventId) => `bet_${eventId}`;

// Normalise une réponse libre : lowercase, sans espaces
const normalize = (s) => String(s || "").toLowerCase().replace(/\s+/g, "");

module.exports = async ({ params, context, logger }) => {
  const { projectId, playerId, accessToken } = context;
  const events = Array.isArray(params.events) ? params.events : [];

  if (events.length === 0) return { ok: true, resolved: [], message: "Aucun événement à résoudre" };

  const cloudSave = new DataApi({ accessToken });
  const currencies = new CurrenciesApi({ accessToken });

  const resolved = [];

  for (const ev of events) {
    const eventId = String(ev.id || "").trim();
    const status = String(ev.status || "");
    const answerType = String(ev.answerType || "list");

    if (!eventId || status !== "CLOSED") continue;

    // Validation des données d'outcome selon le type
    if (answerType === "list" && typeof ev.outcome !== "string") continue;
    if (answerType === "free" && (!Array.isArray(ev.outcomes) || ev.outcomes.length === 0)) continue;

    const key = betKey(eventId);

    try {
      const betData = await cloudSave.getItems(projectId, playerId, [key]);
      const bet = betData?.data?.results?.[0]?.value;
      if (!bet || bet.resolved) continue;

      const amount = Math.floor(Number(bet.amount));
      const choice = String(bet.choice || "");

      let win = false;
      let winOdds = 0;

      if (answerType === "list") {
        // Comparaison exacte du label choisi avec le label gagnant
        const winningLabel = String(ev.outcome).trim();
        win = choice === winningLabel;
        // La côte était stockée au moment du pari
        winOdds = win ? Math.max(0, Number(bet.odds) || 0) : 0;
      } else {
        // Réponse libre : normalisation + matching parmi les bonnes réponses
        const normalizedChoice = normalize(choice);
        for (const o of ev.outcomes) {
          if (normalize(o.answer) === normalizedChoice) {
            win = true;
            winOdds = Math.max(0, Number(o.odds) || 0);
            break;
          }
        }
      }

      // win  => mise * côte
      // loss => mise * 0.5
      const refundRaw = win ? amount * winOdds : amount * 0.5;
      const refund = Math.max(0, Math.floor(refundRaw));

      if (refund > 0) {
        await currencies.incrementPlayerCurrencyBalance({
          projectId,
          playerId,
          currencyId: CURRENCY_ID,
          currencyModifyBalanceRequest: { currencyId: CURRENCY_ID, amount: refund }
        });
      }

      bet.resolved = true;
      bet.resolvedIso = new Date().toISOString();
      bet.win = win;
      bet.refund = refund;
      if (win) bet.winOdds = winOdds;

      await cloudSave.setItem(projectId, playerId, { key, value: bet });

      resolved.push({ eventId, win, refund });
    } catch (err) {
      logger.error(`Resolve error for ${eventId}:`, err);
    }
  }

  return { ok: true, resolved, message: `${resolved.length} pari(s) résolu(s)` };
};
