mergeInto(LibraryManager.library, {
  Insastral_Request: function (jsonPtr) {
    var json = UTF8ToString(jsonPtr);

    try {
      if (typeof window !== 'undefined' && typeof window.dispatchEvent === 'function') {
        window.dispatchEvent(new CustomEvent('insastral-request', { detail: json }));
      }

      return 1;
    } catch (error) {
      console.error('Insastral_Request bridge failed:', error);
      return 0;
    }
  }
});