mergeInto(LibraryManager.library, {
    CloseWebGLWindow: function () {
        window.close();
        // Fallback: if window.close() is blocked by browser, navigate away
        if (!window.closed) {
            window.location.href = "about:blank";
        }
    },

    InitMobileWebGL: function () {
        var canvas = document.querySelector('#unity-canvas') || document.querySelector('canvas');
        if (canvas) {
            // Prevent iOS Safari from treating touches as scroll/zoom gestures
            canvas.style.touchAction = 'none';
            canvas.style.webkitTouchCallout = 'none';
            canvas.style.webkitUserSelect = 'none';
            canvas.style.userSelect = 'none';

            // Block default touch behavior so events reach Unity
            canvas.addEventListener('touchstart', function(e) {
                e.preventDefault();
            }, { passive: false });
            canvas.addEventListener('touchmove', function(e) {
                e.preventDefault();
            }, { passive: false });
            canvas.addEventListener('touchend', function(e) {
                e.preventDefault();
            }, { passive: false });
        }

        // Disable iOS pinch-to-zoom on the whole page
        document.addEventListener('gesturestart', function(e) { e.preventDefault(); });
        document.addEventListener('gesturechange', function(e) { e.preventDefault(); });
        document.addEventListener('gestureend', function(e) { e.preventDefault(); });

        // iOS requires a user gesture to unlock AudioContext.
        // Resume on first touch/click so game audio works.
        var resumeAudio = function() {
            var allContexts = window.unityAudioContexts || [];
            // Unity stores its AudioContext on the Module
            if (typeof Module !== 'undefined') {
                var ctx = Module.audioContext || Module.asc;
                if (ctx && ctx.state === 'suspended') {
                    ctx.resume();
                }
            }
            document.removeEventListener('touchstart', resumeAudio);
            document.removeEventListener('touchend', resumeAudio);
            document.removeEventListener('click', resumeAudio);
        };
        document.addEventListener('touchstart', resumeAudio, { once: true });
        document.addEventListener('touchend', resumeAudio, { once: true });
        document.addEventListener('click', resumeAudio, { once: true });
    }
});
