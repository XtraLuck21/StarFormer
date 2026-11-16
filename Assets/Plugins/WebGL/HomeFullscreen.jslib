mergeInto(LibraryManager.library, {
  HomeFullscreenEntryCount: function () {
    return window.starFormerCornerFullscreenEntries || 0;
  }
});
