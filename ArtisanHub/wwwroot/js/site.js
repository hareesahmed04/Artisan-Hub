// ArtisanHub - small page helpers (no external libraries besides Bootstrap).
document.addEventListener('DOMContentLoaded', function () {

    // Product gallery: click a thumbnail to show it as the main photo.
    document.querySelectorAll('[data-gallery]').forEach(function (gallery) {
        var main = gallery.querySelector('[data-gallery-main]');
        gallery.querySelectorAll('[data-gallery-thumb]').forEach(function (btn) {
            btn.addEventListener('click', function () {
                main.src = btn.getAttribute('data-gallery-thumb');
                gallery.querySelectorAll('[data-gallery-thumb]').forEach(function (b) { b.classList.remove('active'); });
                btn.classList.add('active');
            });
        });
    });

    // Quantity steppers: <div data-qty><button data-step="-1">...<input>...<button data-step="1"></div>
    document.querySelectorAll('[data-qty]').forEach(function (box) {
        var input = box.querySelector('input');
        box.querySelectorAll('[data-step]').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var step = parseInt(btn.getAttribute('data-step'), 10);
                var min = parseInt(input.getAttribute('min') || '0', 10);
                var max = parseInt(input.getAttribute('max') || '9999', 10);
                var value = (parseInt(input.value, 10) || 0) + step;
                input.value = Math.min(max, Math.max(min, value));
                if (box.hasAttribute('data-autosubmit')) { input.form.submit(); }
            });
        });
    });

    // Show or hide blocks based on a radio / checkbox value.
    // <input name="AccountType" data-toggle-target="#artistFields" data-toggle-value="Artist">
    function syncToggles() {
        document.querySelectorAll('[data-toggle-target]').forEach(function (input) {
            var target = document.querySelector(input.getAttribute('data-toggle-target'));
            if (!target) return;
            var wanted = input.getAttribute('data-toggle-value');
            var group = document.querySelectorAll('[name="' + input.name + '"]');
            var show = false;
            group.forEach(function (el) {
                if (el.type === 'checkbox') { show = show || (el.checked && wanted === 'checked'); }
                else if (el.checked && el.value === wanted) { show = true; }
            });
            target.classList.toggle('d-none', !show);
        });
    }
    document.querySelectorAll('[data-toggle-target]').forEach(function (input) {
        document.querySelectorAll('[name="' + input.name + '"]').forEach(function (el) {
            el.addEventListener('change', syncToggles);
        });
    });
    syncToggles();

    // Ask before destructive actions: <form data-confirm="Cancel this order?">
    document.querySelectorAll('form[data-confirm]').forEach(function (form) {
        form.addEventListener('submit', function (e) {
            if (!window.confirm(form.getAttribute('data-confirm'))) { e.preventDefault(); }
        });
    });

    // Auto-submit selects in filter bars: <select data-autosubmit>
    document.querySelectorAll('select[data-autosubmit]').forEach(function (select) {
        select.addEventListener('change', function () { select.form.submit(); });
    });

    // Preview chosen upload file names.
    document.querySelectorAll('input[type=file][data-file-list]').forEach(function (input) {
        var list = document.querySelector(input.getAttribute('data-file-list'));
        input.addEventListener('change', function () {
            if (!list) return;
            list.textContent = Array.prototype.map.call(input.files, function (f) { return f.name; }).join(', ');
        });
    });

    // Fade out flash messages after a few seconds.
    document.querySelectorAll('.flash[data-autohide]').forEach(function (flash) {
        setTimeout(function () {
            flash.style.transition = 'opacity .5s';
            flash.style.opacity = '0';
            setTimeout(function () { flash.remove(); }, 500);
        }, 6000);
    });
});
