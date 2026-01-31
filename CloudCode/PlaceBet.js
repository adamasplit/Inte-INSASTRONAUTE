// PlaceBet Cloud Code Script
// Permet à un joueur de placer un pari YES/NO sur un événement

const { DataApi } = require("@unity-services/cloud-save-1.2");
const { EconomyService } = require("@unity-services/economy-2.3");

module.exports = async ({ params, context, logger }) => {
  const { eventId, amount, side, odds, deadlineIso, status } = params;
  const playerId = context.playerId;

  try {
    // Validation des paramètres
    if (!eventId || typeof amount !== 'number' || amount <= 0) {
      return {
        ok: false,
        message: "Paramètres invalides"
      };
    }

    if (typeof side !== 'boolean') {
      return {
        ok: false,
        message: "Le choix YES/NO est requis"
      };
    }

    // Vérifier que l'événement est ouvert
    if (status !== "OPEN") {
      return {
        ok: false,
        message: "Les paris sont fermés pour cet événement"
      };
    }

    // Vérifier la deadline (optionnel)
    if (deadlineIso) {
      const deadline = new Date(deadlineIso);
      if (new Date() > deadline) {
        return {
          ok: false,
          message: "La deadline est dépassée"
        };
      }
    }

    // Clé du pari dans Cloud Save
    const betKey = `bet_${eventId}`;

    // Vérifier si un pari existe déjà pour cet événement
    const dataApi = new DataApi(context);
    let existingBets;
    
    try {
      existingBets = await dataApi.getItems(playerId, [betKey]);
      
      if (existingBets.data.results && existingBets.data.results.length > 0) {
        const existingBet = existingBets.data.results[0];
        if (existingBet && !existingBet.value.resolved) {
          return {
            ok: false,
            message: "Vous avez déjà parié sur cet événement"
          };
        }
      }
    } catch (err) {
      // Si la clé n'existe pas, c'est OK, on continue
      logger.info("No existing bet found, continuing...");
    }

    // Débiter les TOKEN du joueur
    try {
      await EconomyService.incrementPlayerCurrencyBalance(context, {
        playerId: playerId,
        currencyId: "TOKEN",
        amount: -amount
      });
    } catch (err) {
      logger.error("Failed to debit tokens:", err);
      return {
        ok: false,
        message: `Solde TOKEN insuffisant (${err.message || err})`
      };
    }

    // Créer le pari
    const bet = {
      eventId,
      amount,
      odds: odds || 1.0,
      side,                      // true = YES, false = NO
      placedIso: new Date().toISOString(),
      resolved: false
    };

    // Sauvegarder le pari dans Cloud Save
    await dataApi.setItem(
      playerId,
      betKey,
      { value: bet }
    );

    logger.info(`Bet placed: ${playerId} bet ${amount} TOKEN on ${eventId}, side=${side}`);

    return {
      ok: true,
      message: "Pari enregistré",
      bet,
      eventId
    };

  } catch (error) {
    logger.error("PlaceBet error:", error);
    return {
      ok: false,
      message: "Erreur serveur lors du placement du pari"
    };
  }
};
