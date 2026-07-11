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
  }
});
