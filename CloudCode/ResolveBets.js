// ResolveBets Cloud Code Script
// Résout les paris pour les événements clôturés et crédite les gains

const { DataApi } = require("@unity-services/cloud-save-1.2");
const { EconomyService } = require("@unity-services/economy-2.3");

module.exports = async ({ params, context, logger }) => {
  const { events } = params;
  const playerId = context.playerId;

  try {
    if (!events || !Array.isArray(events) || events.length === 0) {
      return {
        ok: true,
        resolved: [],
        message: "Aucun événement à résoudre"
      };
    }

    const dataApi = new DataApi(context);
    const resolved = [];

    for (const event of events) {
      const { id: eventId, status, outcome } = event;

      // Vérifier que l'événement est clôturé avec un résultat
      if (status !== "CLOSED" || typeof outcome !== 'boolean') {
        continue;
      }

      const betKey = `bet_${eventId}`;

      try {
        // Récupérer le pari du joueur pour cet événement
        const betData = await dataApi.getItems(playerId, [betKey]);
        
        if (!betData.data.results || betData.data.results.length === 0) {
          continue; // Pas de pari pour cet événement
        }

        const betItem = betData.data.results[0];
        const bet = betItem.value;

        // Si déjà résolu, skip
        if (bet.resolved) {
          continue;
        }

        // Déterminer si le joueur a gagné
        const win = bet.side === outcome;

        let refund = 0;

        if (win) {
          // Formule de gain: mise * odds
          refund = Math.floor(bet.amount * bet.odds);
        } else {
          // Perdu: pas de remboursement
          refund = 0;
        }

        // Créditer les TOKEN si gain
        if (refund > 0) {
          await EconomyService.incrementPlayerCurrencyBalance(context, {
            playerId: playerId,
            currencyId: "TOKEN",
            amount: refund
          });
        }

        // Marquer le pari comme résolu
        bet.resolved = true;
        bet.resolvedIso = new Date().toISOString();
        bet.win = win;
        bet.refund = refund;

        await dataApi.setItem(
          playerId,
          betKey,
          { value: bet }
        );

        resolved.push({
          eventId,
          win,
          refund
        });

        logger.info(`Bet resolved: ${playerId} ${win ? 'WON' : 'LOST'} ${eventId}, refund=${refund}`);

      } catch (err) {
        logger.error(`Error resolving bet for ${eventId}:`, err);
        // Continue avec les autres paris
      }
    }

    return {
      ok: true,
      resolved,
      message: `${resolved.length} pari(s) résolu(s)`
    };

  } catch (error) {
    logger.error("ResolveBets error:", error);
    return {
      ok: false,
      resolved: [],
      message: "Erreur serveur lors de la résolution des paris"
    };
  }
};
