// ============================================
// PDF EXPORT - UNIVERSAL FUNCTION
// ============================================

function exportPDF(event, url) {
    event.preventDefault();

    // Get the button
    var btn = event.currentTarget;
    var originalText = btn.innerHTML;

    // Show loading state
    btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Generating PDF...';
    btn.disabled = true;

    // Open PDF in new tab
    var newWindow = window.open(url, '_blank');

    // Check if window was blocked
    if (!newWindow || newWindow.closed || typeof newWindow.closed === 'undefined') {
        // If popup was blocked, fallback to direct navigation
        window.location.href = url;
    }

    // Reset button after delay
    setTimeout(function () {
        btn.innerHTML = originalText;
        btn.disabled = false;
    }, 5000);
}

// ============================================
// PDF EXPORT WITH LOADING OVERLAY
// ============================================

function exportPDFWithOverlay(event, url) {
    event.preventDefault();

    // Show loading overlay
    showLoadingOverlay();

    // Open PDF in new tab
    var newWindow = window.open(url, '_blank');

    // Check if window was blocked
    if (!newWindow || newWindow.closed || typeof newWindow.closed === 'undefined') {
        // If popup was blocked, fallback to direct navigation
        window.location.href = url;
    }

    // Hide overlay after delay
    setTimeout(function () {
        hideLoadingOverlay();
    }, 3000);
}

// ============================================
// LOADING OVERLAY FUNCTIONS
// ============================================

function showLoadingOverlay() {
    // Create overlay if it doesn't exist
    var overlay = document.getElementById('pdfLoadingOverlay');
    if (!overlay) {
        overlay = document.createElement('div');
        overlay.id = 'pdfLoadingOverlay';
        overlay.style.cssText = `
            position: fixed;
            top: 0;
            left: 0;
            right: 0;
            bottom: 0;
            background: rgba(0,0,0,0.6);
            z-index: 9999;
            display: flex;
            justify-content: center;
            align-items: center;
            flex-direction: column;
            backdrop-filter: blur(4px);
        `;
        overlay.innerHTML = `
            <div style="background: white; padding: 40px 50px; border-radius: 16px; text-align: center; box-shadow: 0 20px 60px rgba(0,0,0,0.3);">
                <div style="width: 50px; height: 50px; border: 4px solid #e5e7eb; border-top-color: #dc3545; border-radius: 50%; animation: pdfSpin 1s linear infinite; margin: 0 auto;"></div>
                <div style="margin-top: 20px; font-size: 1.1rem; font-weight: 600; color: #1a1a3e;">Generating PDF...</div>
                <div style="margin-top: 5px; font-size: 0.9rem; color: #6c757d;">Please wait while your document is being prepared</div>
                <style>
                    @keyframes pdfSpin {
                        to { transform: rotate(360deg); }
                    }
                </style>
            </div>
        `;
        document.body.appendChild(overlay);
    }
    overlay.style.display = 'flex';
}

function hideLoadingOverlay() {
    var overlay = document.getElementById('pdfLoadingOverlay');
    if (overlay) {
        overlay.style.display = 'none';
    }
}