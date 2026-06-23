// Lightweight client-side search + pagination for lists/tables.
//
// Usage:
//   <div data-paginate data-page-size="10" data-items=".js-row">
//     <div class="list-toolbar">
//       <input data-search-input class="form-control search-input" placeholder="Suchen..." />
//     </div>
//     <table>
//       <tbody>
//         <tr class="js-row" data-search="anna muster anna@example.com">...</tr>
//         ...
//       </tbody>
//     </table>
//     <div data-empty class="list-empty d-none">Keine Einträge gefunden.</div>
//     <div data-pager class="pager"></div>
//   </div>
(function () {
    function normalize(value) {
        return (value || '').toString().toLowerCase().trim();
    }

    function init(root) {
        var pageSize = parseInt(root.getAttribute('data-page-size') || '10', 10);
        var itemSelector = root.getAttribute('data-items') || '[data-search]';
        var searchInput = root.querySelector('[data-search-input]');
        var pagerContainer = root.querySelector('[data-pager]');
        var emptyMessage = root.querySelector('[data-empty]');
        var items = Array.prototype.slice.call(root.querySelectorAll(itemSelector));
        var filtered = items.slice();
        var currentPage = 1;

        function makeButton(label, page, opts) {
            var btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'page-link';
            btn.textContent = label;
            if (opts && opts.active) btn.classList.add('active');
            if (opts && opts.disabled) btn.disabled = true;
            btn.addEventListener('click', function () {
                if (btn.disabled || (opts && opts.active)) return;
                currentPage = page;
                render();
            });
            return btn;
        }

        function renderPager(totalPages) {
            if (!pagerContainer) return;
            pagerContainer.innerHTML = '';
            if (filtered.length <= pageSize) return;

            pagerContainer.appendChild(makeButton('‹', Math.max(1, currentPage - 1), {
                disabled: currentPage === 1
            }));

            var pages = visiblePages(totalPages, currentPage);
            pages.forEach(function (page) {
                if (page === '…') {
                    var span = document.createElement('span');
                    span.className = 'page-link';
                    span.textContent = '…';
                    span.style.cursor = 'default';
                    span.style.background = 'transparent';
                    span.style.border = 'none';
                    pagerContainer.appendChild(span);
                } else {
                    pagerContainer.appendChild(makeButton(String(page), page, { active: page === currentPage }));
                }
            });

            pagerContainer.appendChild(makeButton('›', Math.min(totalPages, currentPage + 1), {
                disabled: currentPage === totalPages
            }));
        }

        function visiblePages(totalPages, current) {
            // For small page counts show them all; otherwise show a windowed list.
            if (totalPages <= 7) {
                var all = [];
                for (var i = 1; i <= totalPages; i++) all.push(i);
                return all;
            }
            var pages = [1];
            var start = Math.max(2, current - 1);
            var end = Math.min(totalPages - 1, current + 1);
            if (start > 2) pages.push('…');
            for (var j = start; j <= end; j++) pages.push(j);
            if (end < totalPages - 1) pages.push('…');
            pages.push(totalPages);
            return pages;
        }

        function render() {
            var totalPages = Math.max(1, Math.ceil(filtered.length / pageSize));
            if (currentPage > totalPages) currentPage = totalPages;

            var start = (currentPage - 1) * pageSize;
            var end = start + pageSize;

            items.forEach(function (item) {
                item.classList.add('d-none');
            });
            filtered.slice(start, end).forEach(function (item) {
                item.classList.remove('d-none');
            });

            if (emptyMessage) {
                emptyMessage.classList.toggle('d-none', filtered.length !== 0);
            }

            renderPager(totalPages);
        }

        function applyFilter() {
            var query = normalize(searchInput ? searchInput.value : '');
            if (!query) {
                filtered = items.slice();
            } else {
                filtered = items.filter(function (item) {
                    var haystack = normalize(item.getAttribute('data-search'));
                    return haystack.indexOf(query) !== -1;
                });
            }
            currentPage = 1;
            render();
        }

        if (searchInput) {
            searchInput.addEventListener('input', applyFilter);
        }

        applyFilter();
    }

    document.addEventListener('DOMContentLoaded', function () {
        Array.prototype.forEach.call(document.querySelectorAll('[data-paginate]'), init);
    });
})();
