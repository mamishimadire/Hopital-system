// MedBridge - Client-side JS

(function () {
    'use strict';

    // ---- Sidebar toggle (mobile) ----
    const sidebar = document.getElementById('sidebar');
    const toggle  = document.getElementById('sidebarToggle');
    const overlay = document.getElementById('sidebarOverlay');

    function openSidebar()  { sidebar?.classList.add('open'); overlay?.classList.add('show'); }
    function closeSidebar() { sidebar?.classList.remove('open'); overlay?.classList.remove('show'); }

    toggle?.addEventListener('click', openSidebar);
    overlay?.addEventListener('click', closeSidebar);

    // ---- Auto-dismiss flash alerts ----
    document.querySelectorAll('.alert-banner').forEach(function (el) {
        setTimeout(function () {
            el.style.transition = 'opacity 0.5s';
            el.style.opacity = '0';
            setTimeout(function () { el.remove(); }, 500);
        }, 4000);
    });

    // ---- Confirm dangerous actions ----
    document.querySelectorAll('[data-confirm]').forEach(function (el) {
        el.addEventListener('click', function (e) {
            var msg = el.dataset.confirm || 'Are you sure?';
            if (!confirm(msg)) e.preventDefault();
        });
    });

    // ---- Auto-submit filter forms on select change ----
    document.querySelectorAll('.filter-select').forEach(function (sel) {
        sel.addEventListener('change', function () {
            this.closest('form')?.submit();
        });
    });

})();
