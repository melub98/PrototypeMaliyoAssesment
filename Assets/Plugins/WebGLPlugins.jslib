mergeInto(LibraryManager.library, {
    CloseWebGLWindow: function () {
        window.close();
        // Fallback: if window.close() is blocked by browser, navigate away
        if (!window.closed) {
            window.location.href = "about:blank";
        }
    }
});
