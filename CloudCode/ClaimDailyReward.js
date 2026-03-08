// ClaimDailyReward.js
// Attribue la récompense quotidienne au joueur si le délai de recharge est écoulé.
// La configuration est passée en paramètre JSON string par le client C#.

const { DataApi } = require("@unity-services/cloud-save-1.4");
const { CurrenciesApi } = require("@unity-services/economy-2.4");

const CLAIM_SAVE_KEY = "daily_reward_last_claim";

module.exports = async ({ params, context, logger }) => {
  const { projectId, playerId, accessToken } = context;

  const cloudSave = new DataApi({ accessToken });
  const currencies = new CurrenciesApi({ accessToken });
  const checkOnly = params.checkOnly === true
    || params.checkOnly === 1
    || String(params.checkOnly).toLowerCase() === "true";

  // ── 1. Lire la configuration depuis les paramètres ───────────────────────
  // Le C# envoie rewardsJson = JSON string car le SDK ne sérialise pas bien les arrays.
  let rewards;
  let cooldownHours = 24;

  try {
    const raw = params.rewardsJson;
    if (!raw) {
      logger.error(`Paramètre 'rewardsJson' manquant. params reçus : ${JSON.stringify(params)}`);
      return makeError("CONFIG_NOT_FOUND", "Configuration des récompenses manquante.");
    }
    const parsed = JSON.parse(raw);
    rewards = parsed.rewards;
    cooldownHours = parsed.cooldownHours ?? 24;
  } catch (parseErr) {
    logger.error("Erreur parsing rewardsJson : " + parseErr.message);
    return makeError("CONFIG_ERROR", "Configuration des récompenses invalide.");
  }

  if (!Array.isArray(rewards) || rewards.length === 0) {
    logger.error(`Tableau 'rewards' vide après parsing.`);
    return makeError("CONFIG_NOT_FOUND", "Configuration des récompenses manquante.");
  }

  logger.info(`Config reçue : cooldownHours=${cooldownHours}, rewards=${rewards.length}`);

  const cooldownMs = cooldownHours * 3600 * 1000;

  // ── 2. Vérifier la date du dernier claim dans Cloud Save ──────────────────
  let lastClaimMs = 0;
  try {
    const saveResult = await cloudSave.getItems(projectId, playerId, [CLAIM_SAVE_KEY]);
    if (saveResult.data?.results?.length > 0) {
      lastClaimMs = saveResult.data.results[0].value?.lastClaimMs ?? 0;
    }
  } catch (err) {
    // Clé absente = premier claim, on continue normalement
    logger.info(`Aucun claim précédent trouvé pour ${playerId}`);
  }

  const now = Date.now();
  const elapsed = now - lastClaimMs;

  if (elapsed < cooldownMs) {
    const remainingSeconds = Math.ceil((cooldownMs - elapsed) / 1000);
    return {
      ok: false,
      errorCode: "ALREADY_CLAIMED",
      cooldownSecondsRemaining: remainingSeconds,
      grantedRewards: [],
      message: "Vous avez déjà réclamé votre récompense aujourd'hui."
    };
  }

  // Mode statut : ne pas attribuer, juste confirmer que le claim est possible.
  if (checkOnly) {
    return {
      ok: true,
      errorCode: "",
      cooldownSecondsRemaining: 0,
      grantedRewards: [],
      message: "CAN_CLAIM"
    };
  }

  // ── 3. Distribuer les récompenses ─────────────────────────────────────────
  const grantedRewards = [];
  const errors = [];

  for (const reward of rewards) {
    try {
      if (reward.type === "TOKEN" || reward.type === "PC") {
        // Crédit de monnaie via Economy
        logger.info(`Crédit ${reward.amount} ${reward.type} pour ${playerId}...`);
        await currencies.incrementPlayerCurrencyBalance({
          projectId,
          playerId,
          currencyId: reward.type,
          currencyModifyBalanceRequest: { currencyId: reward.type, amount: reward.amount }
        });
        logger.info(`Economy OK pour ${reward.type}`);
        grantedRewards.push({
          type: reward.type,
          packId: "",
          amount: reward.amount,
          label: reward.label ?? ""
        });

      } else if (reward.type === "PACK") {
        // Ajout du pack dans packCollection (Cloud Save)
        let packCollection = {};
        try {
          const packResult = await cloudSave.getItems(projectId, playerId, ["packCollection"]);
          if (packResult.data?.results?.length > 0) {
            packCollection = packResult.data.results[0].value ?? {};
          }
        } catch (e) {
          // Pas encore de collection, on part d'un objet vide
        }

        const packId = reward.packId;
        if (!packId) {
          logger.error("Récompense PACK sans packId, ignorée.");
          errors.push("PACK sans packId");
          continue;
        }

        packCollection[packId] = (packCollection[packId] ?? 0) + (reward.amount ?? 1);
        logger.info(`Sauvegarde packCollection pour ${playerId}: ${JSON.stringify(packCollection)}`);
        await cloudSave.setItem(projectId, playerId, { key: "packCollection", value: packCollection });
        logger.info("CloudSave packCollection OK");

        grantedRewards.push({
          type: "PACK",
          packId,
          amount: reward.amount ?? 1,
          label: reward.label ?? ""
        });

      } else {
        logger.info(`Type de récompense inconnu : ${reward.type}`);
      }
    } catch (err) {
      const msg = `Erreur reward ${reward.type}: ${err?.message ?? JSON.stringify(err)}`;
      logger.error(msg);
      errors.push(msg);
    }
  }

  // Si aucune récompense n'a pu être attribuée, ne pas enregistrer le claim
  if (grantedRewards.length === 0) {
    const detail = errors.length > 0 ? errors.join(" | ") : "Aucune erreur capturée mais aucune reward distribuée";
    logger.error("Aucune récompense attribuée: " + detail);
    return makeError("GRANT_FAILED", "Échec: " + detail);
  }

  // ── 4. Enregistrer le timestamp du claim ──────────────────────────────────
  try {
    await cloudSave.setItem(projectId, playerId, { key: CLAIM_SAVE_KEY, value: { lastClaimMs: now } });
  } catch (err) {
    logger.error("Erreur lors de la sauvegarde du timestamp de claim :", err);
    // Pas bloquant : les récompenses ont déjà été attribuées
  }

  logger.info(`Récompense journalière attribuée à ${playerId} : ${grantedRewards.length} récompense(s)`);

  return {
    ok: true,
    errorCode: "",
    cooldownSecondsRemaining: 0,
    grantedRewards,
    message: "Récompenses du jour réclamées avec succès !"
  };
};

// ── Helpers ───────────────────────────────────────────────────────────────────
function makeError(errorCode, message) {
  return {
    ok: false,
    errorCode,
    cooldownSecondsRemaining: 0,
    grantedRewards: [],
    message
  };
}
