// Responsive navigation toggle (hamburger menu).
//
// Markup contract:
//   <button data-nav-toggle="myMenu" aria-controls="myMenu" aria-expanded="false">…</button>
//   <nav id="myMenu">…</nav>
//   <div data-nav-backdrop="myMenu"></div>   (optional, for off-canvas drawers)
//
// Adds/removes the "is-open" class on the toggle and target, toggles the
// backdrop, and closes the menu on link click, backdrop click, Escape, or
// when the viewport grows back to desktop width.
(function () {
    var desktop = window.matchMedia('(min-width: 769px)');

    function bind(btn) {
        var id = btn.getAttribute('data-nav-toggle');
        var target = document.getElementById(id);
        if (!target) return;
        var backdrop = document.querySelector('[data-nav-backdrop="' + id + '"]');

        function setOpen(open) {
            target.classList.toggle('is-open', open);
            btn.classList.toggle('is-open', open);
            btn.setAttribute('aria-expanded', open ? 'true' : 'false');
            if (backdrop) {
                backdrop.classList.toggle('is-visible', open);
                document.body.classList.toggle('nav-open', open);
            }
        }

        btn.addEventListener('click', function () {
            setOpen(!target.classList.contains('is-open'));
        });

        if (backdrop) {
            backdrop.addEventListener('click', function () { setOpen(false); });
        }

        // Close once the user follows a link or submits a form inside the menu.
        target.addEventListener('click', function (e) {
            if (e.target.closest('a, button[type="submit"]')) setOpen(false);
        });

        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') setOpen(false);
        });

        function onDesktop() {
            if (desktop.matches) setOpen(false);
        }
        if (desktop.addEventListener) desktop.addEventListener('change', onDesktop);
        else if (desktop.addListener) desktop.addListener(onDesktop);
    }

    Array.prototype.forEach.call(document.querySelectorAll('[data-nav-toggle]'), bind);
})();
