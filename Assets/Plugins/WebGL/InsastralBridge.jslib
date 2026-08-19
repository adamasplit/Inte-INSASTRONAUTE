mergeInto(LibraryManager.library, {
  Insastral_Request: function (jsonPtr) {
    var json = UTF8ToString(jsonPtr);

    try {
      if (
        typeof window === 'undefined' ||
        !window.insastralUnityBridge ||
        typeof window.insastralUnityBridge.request !== 'function'
      ) {
        console.error('[InsastralBridge] React bridge is not ready');
        return 0;
      }

      window.insastralUnityBridge.request(json);
      return 1;
    } catch (error) {
      console.error('Insastral_Request bridge failed:', error);
      return 0;
    }
  },

  Insastral_CombatConnect: function (jsonPtr) {
    var json = UTF8ToString(jsonPtr);

    try {
      if (
        typeof window === 'undefined' ||
        !window.insastralCombatBridge ||
        typeof window.insastralCombatBridge.connect !== 'function'
      ) {
        console.error('[InsastralBridge] Combat bridge is not ready');
        return 0;
      }

      window.insastralCombatBridge.connect(json);
      return 1;
    } catch (error) {
      console.error('Insastral_CombatConnect bridge failed:', error);
      return 0;
    }
  },

  Insastral_CombatDisconnect: function (jsonPtr) {
    var json = UTF8ToString(jsonPtr);

    try {
      if (
        typeof window === 'undefined' ||
        !window.insastralCombatBridge ||
        typeof window.insastralCombatBridge.disconnect !== 'function'
      ) {
        console.error('[InsastralBridge] Combat bridge is not ready');
        return 0;
      }

      window.insastralCombatBridge.disconnect(json);
      return 1;
    } catch (error) {
      console.error('Insastral_CombatDisconnect bridge failed:', error);
      return 0;
    }
  },

  Insastral_CombatCommand: function (jsonPtr) {
    var json = UTF8ToString(jsonPtr);

    try {
      if (
        typeof window === 'undefined' ||
        !window.insastralCombatBridge ||
        typeof window.insastralCombatBridge.command !== 'function'
      ) {
        console.error('[InsastralBridge] Combat bridge is not ready');
        return 0;
      }

      window.insastralCombatBridge.command(json);
      return 1;
    } catch (error) {
      console.error('Insastral_CombatCommand bridge failed:', error);
      return 0;
    }
  }
});
