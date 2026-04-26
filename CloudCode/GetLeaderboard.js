const { LeaderboardsApi } = require("@unity-services/leaderboards-1.1");
const { DataApi } = require("@unity-services/cloud-save-1.3");

const DEFAULT_LEADERBOARD_ID = "PTD";
const DEFAULT_LIMIT = 20;
const MAX_LIMIT = 50;
const DEFAULT_SCAN_LIMIT = 500;
const SCRIPT_VERSION = "GetLeaderboard-displayName-only-cloudsave-fallback-v7-2026-04-25";

function clamp(value, min, max) {
    return Math.max(min, Math.min(max, value));
}

function parseMetadata(rawMetadata) {
    if (!rawMetadata) return {};

    try {
        const parsed = JSON.parse(rawMetadata);
        return typeof parsed === "object" && parsed !== null ? parsed : {};
    } catch {
        return {};
    }
}

function normalize(value) {
    return String(value || "").trim().toUpperCase();
}

function normalizeCsvSet(value) {
    if (value == null) return new Set();

    const raw = Array.isArray(value) ? value.join(",") : String(value);
    const parts = raw
        .split(",")
        .map(v => normalize(v))
        .filter(v => v.length > 0);

    return new Set(parts);
}

function normalizeLeaderboardId(value) {
    const normalized = String(value || "").trim().toUpperCase();
    return normalized.length > 0 ? normalized : DEFAULT_LEADERBOARD_ID;
}

function firstNonEmpty(...values) {
    for (const value of values) {
        if (typeof value === "string") {
            const trimmed = value.trim();
            if (trimmed.length > 0) return trimmed;
        }
    }

    return "";
}

function isGenericName(value) {
    const normalized = String(value || "").trim().toUpperCase();
    return normalized === "PLAYER" || normalized === "GUEST" || normalized === "UNKNOWN";
}

function resolveDisplayName(metadata, entry) {
    const displayName = firstNonEmpty(
        metadata.displayName,
        metadata.displayname,
        metadata.DisplayName
    );
    if (displayName.length > 0 && !isGenericName(displayName)) {
        return displayName;
    }

    return "";
}

async function loadCloudSaveDisplayName(cloudSaveApi, projectId, targetPlayerId, logger, cache) {
    if (!targetPlayerId) return "";

    if (cache.has(targetPlayerId)) {
        return cache.get(targetPlayerId);
    }

    try {
        const response = await cloudSaveApi.getItems({
            projectId,
            playerId: targetPlayerId,
            keys: ["displayName"]
        });

        const displayNameItem = (response.data.results || []).find(item => item.key === "displayName");
        const value = typeof displayNameItem?.value === "string" ? displayNameItem.value.trim() : "";
        const normalized = value.length > 0 && !isGenericName(value) ? value : "";
        cache.set(targetPlayerId, normalized);
        return normalized;
    } catch (error) {
        logger.info(`DisplayName lookup failed for ${targetPlayerId}: ${error.message}`);
        cache.set(targetPlayerId, "");
        return "";
    }
}

async function toEntryResolved(entry, currentPlayerId, friendIds, cloudSaveApi, projectId, logger, displayNameCache) {
    const metadata = parseMetadata(entry.metadata);
    let displayName = resolveDisplayName(metadata, entry);

    if (!displayName) {
        displayName = await loadCloudSaveDisplayName(
            cloudSaveApi,
            projectId,
            entry.playerId,
            logger,
            displayNameCache
        );
    }

    if (!displayName) {
        displayName = "Unknown";
    }

    const department = typeof metadata.department === "string" && metadata.department.length > 0
        ? metadata.department
        : "UNKNOWN";

    const scoreTheme = typeof metadata.scoreTheme === "string" ? metadata.scoreTheme : "";

    return {
        rank: Number(entry.rank) + 1,
        playerId: entry.playerId,
        displayName,
        score: Number(entry.score) || 0,
        department,
        scoreTheme,
        isFriend: friendIds.has(entry.playerId),
        isCurrentPlayer: entry.playerId === currentPlayerId
    };
}

function passesFilters(item, filters) {
    if (filters.friendsOnly && !item.isFriend && !item.isCurrentPlayer) {
        return false;
    }

    if (filters.departmentSet && filters.departmentSet.size > 0 && !filters.departmentSet.has(normalize(item.department))) {
        return false;
    }

    if (filters.theme && normalize(item.scoreTheme) !== filters.theme) {
        return false;
    }

    return true;
}

async function loadFriendIds(cloudSaveApi, projectId, playerId, logger) {
    try {
        const response = await cloudSaveApi.getItems({
            projectId,
            playerId,
            keys: ["friends"]
        });

        const friendsItem = (response.data.results || []).find(item => item.key === "friends");
        if (!friendsItem || !Array.isArray(friendsItem.value)) {
            return [];
        }

        return friendsItem.value.filter(id => typeof id === "string" && id.length > 0);
    } catch (error) {
        logger.warning(`Could not load friends list: ${error.message}`);
        return [];
    }
}

async function tryCall(method, variants) {
    const errors = [];

    for (const variant of variants) {
        try {
            return await variant();
        } catch (error) {
            errors.push(error);
        }
    }

    throw errors[errors.length - 1] || new Error(`All call variants failed for ${method}`);
}

async function callGetLeaderboardScores(leaderboardsApi, projectId, leaderboardId, offset, limit) {
    const requestLeaderboardId = normalizeLeaderboardId(leaderboardId);

    return await tryCall("getLeaderboardScores", [
        // SDK variant with positional args.
        () => leaderboardsApi.getLeaderboardScores(projectId, requestLeaderboardId, offset, limit),
        // SDK variant with positional args + optional params before paging.
        () => leaderboardsApi.getLeaderboardScores(projectId, requestLeaderboardId, undefined, undefined, offset, limit),
        // SDK variant with request object.
        () => leaderboardsApi.getLeaderboardScores({
            projectId,
            leaderboardId: requestLeaderboardId,
            id: requestLeaderboardId,
            offset,
            limit
        })
    ]);
}

async function callGetLeaderboardPlayerScore(leaderboardsApi, projectId, leaderboardId, playerId) {
    const requestLeaderboardId = normalizeLeaderboardId(leaderboardId);

    return await tryCall("getLeaderboardPlayerScore", [
        // SDK variant with positional args.
        () => leaderboardsApi.getLeaderboardPlayerScore(projectId, requestLeaderboardId, playerId),
        // SDK variant with request object.
        () => leaderboardsApi.getLeaderboardPlayerScore({
            projectId,
            leaderboardId: requestLeaderboardId,
            id: requestLeaderboardId,
            playerId
        })
    ]);
}

async function getPlayerEntry(leaderboardsApi, projectId, leaderboardId, targetPlayerId, currentPlayerId, friendIds) {
    const playerScoreResponse = await callGetLeaderboardPlayerScore(
        leaderboardsApi,
        projectId,
        leaderboardId,
        targetPlayerId
    );
    return playerScoreResponse.data;
}

module.exports = async ({ params, context, logger }) => {
    const { projectId, playerId, accessToken } = context;

    const leaderboardsApi = new LeaderboardsApi({ accessToken });
    const cloudSaveApi = new DataApi({ accessToken });

    const leaderboardId = normalizeLeaderboardId(params?.leaderboardId);

    const offset = clamp(Number(params?.offset) || 0, 0, Number.MAX_SAFE_INTEGER);
    const limit = clamp(Number(params?.limit) || DEFAULT_LIMIT, 1, MAX_LIMIT);
    const scanLimit = clamp(Number(params?.scanLimit) || DEFAULT_SCAN_LIMIT, limit, 5000);

    const filters = {
        friendsOnly: Boolean(params?.friendsOnly),
        departmentSet: normalizeCsvSet(params?.department),
        theme: normalize(params?.theme)
    };

    const includeCurrentPlayer = params?.includeCurrentPlayer !== false;
    const includeFriends = params?.includeFriends !== false;

    logger.info(`[${SCRIPT_VERSION}] leaderboardId=${leaderboardId} projectId=${projectId} scoreFnArity=${leaderboardsApi.getLeaderboardScores?.length} playerFnArity=${leaderboardsApi.getLeaderboardPlayerScore?.length}`);

    try {
        const friendIdList = await loadFriendIds(cloudSaveApi, projectId, playerId, logger);
        const friendIds = new Set(friendIdList);
        const displayNameCache = new Map();

        const entries = [];
        let scanned = 0;
        let rawOffset = offset;
        let hasMoreRaw = true;

        while (entries.length < limit && hasMoreRaw && scanned < scanLimit) {
            const rawTake = Math.min(MAX_LIMIT, Math.max(limit * 2, 20));
            const scoresResponse = await callGetLeaderboardScores(
                leaderboardsApi,
                projectId,
                leaderboardId,
                rawOffset,
                rawTake
            );

            const rows = scoresResponse.data.results || [];
            scanned += rows.length;

            for (const row of rows) {
                const parsed = await toEntryResolved(row, playerId, friendIds, cloudSaveApi, projectId, logger, displayNameCache);
                if (passesFilters(parsed, filters)) {
                    entries.push(parsed);
                    if (entries.length >= limit) break;
                }
            }

            rawOffset += rows.length;
            hasMoreRaw = rows.length === rawTake;
        }

        let currentPlayer = null;
        if (includeCurrentPlayer) {
            try {
                const currentRaw = await getPlayerEntry(leaderboardsApi, projectId, normalizeLeaderboardId(leaderboardId), playerId, playerId, friendIds);
                const current = await toEntryResolved(currentRaw, playerId, friendIds, cloudSaveApi, projectId, logger, displayNameCache);
                if (passesFilters(current, filters) || !filters.friendsOnly) {
                    currentPlayer = current;
                }
            } catch (error) {
                logger.info(`Current player has no score yet on ${leaderboardId}: ${error.message}`);
            }
        }

        const friends = [];
        if (includeFriends && friendIdList.length > 0) {
            for (const friendId of friendIdList) {
                try {
                    const friendRaw = await getPlayerEntry(leaderboardsApi, projectId, normalizeLeaderboardId(leaderboardId), friendId, playerId, friendIds);
                    const friendEntry = await toEntryResolved(friendRaw, playerId, friendIds, cloudSaveApi, projectId, logger, displayNameCache);
                    if (passesFilters(friendEntry, filters)) {
                        friends.push(friendEntry);
                    }
                } catch {
                    // Friend without score for this leaderboard, ignore.
                }
            }

            friends.sort((a, b) => a.rank - b.rank);
        }

        return {
            ok: true,
            leaderboardId,
            entries,
            currentPlayer,
            friends,
            nextOffset: rawOffset,
            hasMore: hasMoreRaw && scanned < scanLimit,
            message: `OK (${SCRIPT_VERSION})`
        };
    } catch (error) {
        logger.error(`Error while fetching leaderboard: ${error.message}`);
        return {
            ok: false,
            leaderboardId,
            entries: [],
            currentPlayer: null,
            friends: [],
            nextOffset: offset,
            hasMore: false,
            message: `${error.message} (${SCRIPT_VERSION})`
        };
    }
};