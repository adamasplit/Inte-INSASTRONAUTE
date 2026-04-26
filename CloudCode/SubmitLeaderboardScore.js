const { CurrenciesApi } = require("@unity-services/economy-2.4");
const { DataApi } = require("@unity-services/cloud-save-1.3");
const { LeaderboardsApi } = require("@unity-services/leaderboards-1.1");

const DEFAULT_LEADERBOARD_ID = "PTD";

function parseScore(value, fallback = 0) {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : fallback;
}

function normalizeNonEmpty(value, fallback = "") {
    if (typeof value !== "string") return fallback;
    const trimmed = value.trim();
    return trimmed.length > 0 ? trimmed : fallback;
}

async function getCloudProfile(cloudSaveApi, projectId, playerId, logger) {
    const profile = {
        displayName: playerId,
        department: "UNKNOWN",
        friends: []
    };

    try {
        const response = await cloudSaveApi.getItems({
            projectId,
            playerId,
            keys: ["displayName", "department", "friends"]
        });

        const map = new Map((response.data.results || []).map(item => [item.key, item.value]));
        const cloudDisplayName = map.get("displayName");
        profile.displayName = typeof cloudDisplayName === "string" && cloudDisplayName.trim().length > 0
            ? cloudDisplayName.trim()
            : profile.displayName;
        profile.department = map.get("department") || profile.department;

        const friendIds = map.get("friends");
        if (Array.isArray(friendIds)) {
            profile.friends = friendIds.filter(id => typeof id === "string" && id.length > 0);
        }
    } catch (error) {
        logger.warning(`Could not load cloud profile: ${error.message}`);
    }

    return profile;
}

async function getEconomyFallbackScore(economyApi, projectId, playerId, leaderboardId, logger) {
    try {
        const balancesResponse = await economyApi.getPlayerCurrencies({ projectId, playerId });
        const currency = (balancesResponse.data.results || []).find(c => c.currencyId === leaderboardId);
        return currency ? Number(currency.balance) : 0;
    } catch (error) {
        logger.warning(`Could not read Economy fallback score for ${leaderboardId}: ${error.message}`);
        return 0;
    }
}

module.exports = async ({ params, context, logger }) => {
    const { projectId, playerId, accessToken } = context;

    const leaderboardId = typeof params?.leaderboardId === "string" && params.leaderboardId.length > 0
        ? params.leaderboardId
        : DEFAULT_LEADERBOARD_ID;

    const economyApi = new CurrenciesApi({ accessToken });
    const cloudSaveApi = new DataApi({ accessToken });
    const leaderboardsApi = new LeaderboardsApi({ accessToken });

    try {
        const profile = await getCloudProfile(cloudSaveApi, projectId, playerId, logger);
        const fallbackScore = await getEconomyFallbackScore(economyApi, projectId, playerId, leaderboardId, logger);
        const score = parseScore(params?.score, fallbackScore);

        const metadata = {
            displayName: normalizeNonEmpty(params?.displayName, normalizeNonEmpty(profile.displayName, playerId)),
            department: params?.department || profile.department || "UNKNOWN",
            scoreTheme: typeof params?.scoreTheme === "string" ? params.scoreTheme : "",
            updatedAtIso: new Date().toISOString()
        };

        await leaderboardsApi.addLeaderboardPlayerScore({
            projectId,
            leaderboardId,
            playerId,
            leaderboardScore: {
                score,
                metadata: JSON.stringify(metadata)
            }
        });

        logger.info(`Score ${score} submitted for player ${playerId} on ${leaderboardId}`);

        return {
            ok: true,
            leaderboardId,
            score,
            metadata,
            message: "Score submitted successfully"
        };
    } catch (error) {
        logger.error(`Error while submitting leaderboard score: ${error.message}`);
        return {
            ok: false,
            leaderboardId,
            score: 0,
            message: error.message
        };
    }
};