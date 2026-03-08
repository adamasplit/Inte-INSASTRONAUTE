const { DataApi } = require("@unity-services/cloud-save-1.4");
const { CurrenciesApi } = require("@unity-services/economy-2.4");

const CURRENCY_ID = "TOKEN";
const betKey = (eventId) => `bet_${eventId}`;

module.exports = async ({ params, context, logger }) => {
  const { projectId, playerId, accessToken } = context;
  const { eventId, amount, choice, odds, answerType, deadlineIso, status } = params;

  if (!eventId || typeof amount !== "number" || amount <= 0) {
    return { ok: false, message: "Paramètres invalides" };
  }
  if (typeof choice !== "string" || choice.trim() === "") {
    return { ok: false, message: "Le choix est requis" };
  }
  if (answerType !== "list" && answerType !== "free") {
    return { ok: false, message: "answerType invalide (list | free)" };
  }
  if (status !== "OPEN") {
    return { ok: false, message: "Les paris sont fermés pour cet événement" };
  }

  // Normalisation pour les réponses libres : lowercase, sans espaces
  const normalizedChoice = answerType === "free"
    ? choice.toLowerCase().replace(/\s+/g, "")
    : choice.trim();

  if (deadlineIso) {
    const deadline = new Date(deadlineIso);
    if (Number.isNaN(deadline.getTime())) return { ok: false, message: "deadlineIso invalide" };
    if (Date.now() > deadline.getTime()) return { ok: false, message: "La deadline est dépassée" };
  }

  const cloudSave = new DataApi({ accessToken });
  const key = betKey(eventId);

  // Lock : refus si pari non résolu existant
  try {
    const existing = await cloudSave.getItems(projectId, playerId, [key]);
    const val = existing?.data?.results?.[0]?.value;
    if (val && val.resolved === false) {
      return { ok: false, message: "Vous avez déjà parié sur cet événement" };
    }
  } catch (_) {}

  // Débit TOKEN
  const currencies = new CurrenciesApi({ accessToken });
  try {
    await currencies.decrementPlayerCurrencyBalance({
      projectId,
      playerId,
      currencyId: CURRENCY_ID,
      currencyModifyBalanceRequest: { currencyId: CURRENCY_ID, amount: Math.floor(amount) }
    });
  } catch (err) {
    logger.error("Debit failed:", err);
    return { ok: false, message: "Solde TOKEN insuffisant" };
  }

  const bet = {
    eventId,
    amount: Math.floor(amount),
    answerType,
    choice: normalizedChoice,
    // Pour list : côte connue maintenant. Pour free : 0, déterminée à la résolution.
    odds: answerType === "list" ? Math.max(0, Number(odds) || 0) : 0,
    placedIso: new Date().toISOString(),
    resolved: false
  };

  await cloudSave.setItem(projectId, playerId, { key, value: bet });

  return { ok: true, message: "Pari enregistré", bet, eventId };
};
