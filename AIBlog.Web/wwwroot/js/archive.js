// Archive page JavaScript
document.addEventListener('DOMContentLoaded', function () {
    const searchInput = document.getElementById('searchInput');
    const searchBtn = document.getElementById('searchBtn');
    const clearFilters = document.getElementById('clearFilters');
    const filterBtns = document.querySelectorAll('.filter-btn');
    const sortBtns = document.querySelectorAll('.sort-btn');
    const loadMoreBtn = document.getElementById('loadMoreBtn');
    const archiveTabs = document.querySelectorAll('.archive-tab');

    // Current filter state
    let currentFilters = {
        search: searchInput ? searchInput.value : '',
        timePeriod: '',
        categoryId: '',
        sortBy: 'newest',
        tab: document.querySelector('.archive-tab.active')?.dataset.tab || 'read'
    };

    // Tab switching
    archiveTabs.forEach(tab => {
        tab.addEventListener('click', function () {
            const targetTab = this.dataset.tab;

            // Update active tab button
            archiveTabs.forEach(t => t.classList.remove('active'));
            this.classList.add('active');

            // Show/hide tab content
            document.querySelectorAll('.tab-content').forEach(content => {
                content.classList.remove('active');
            });

            if (targetTab === 'read') {
                document.getElementById('tabRead')?.classList.add('active');
            } else {
                document.getElementById('tabDrafts')?.classList.add('active');
            }

            // Update filter state
            currentFilters.tab = targetTab;
        });
    });

    // Get active filters from buttons
    filterBtns.forEach(btn => {
        if (btn.classList.contains('active')) {
            const filter = btn.dataset.filter;
            const value = btn.dataset.value;
            currentFilters[filter] = value;
        }
    });

    sortBtns.forEach(btn => {
        if (btn.classList.contains('active')) {
            currentFilters.sortBy = btn.dataset.sort;
        }
    });

    // Search functionality
    if (searchBtn) {
        searchBtn.addEventListener('click', performSearch);
    }
    if (searchInput) {
        searchInput.addEventListener('keypress', function (e) {
            if (e.key === 'Enter') {
                performSearch();
            }
        });
    }

    function performSearch() {
        currentFilters.search = searchInput.value;
        applyFilters();
    }

    // Filter buttons
    filterBtns.forEach(btn => {
        btn.addEventListener('click', function () {
            const filter = this.dataset.filter;
            const value = this.dataset.value;

            // Handle "Choose a date" button
            if (filter === 'timePeriod' && value === 'custom') {
                openDatePicker();
                return;
            }

            // Toggle active state within same filter group
            const siblingBtns = this.parentElement.querySelectorAll('.filter-btn');
            siblingBtns.forEach(b => b.classList.remove('active'));

            if (currentFilters[filter] !== value) {
                this.classList.add('active');
                currentFilters[filter] = value;
            } else {
                currentFilters[filter] = '';
            }

            applyFilters();
        });
    });

    // Date Picker Modal
    const datePickerModal = document.getElementById('datePickerModal');
    const datePickerOverlay = document.getElementById('datePickerOverlay');
    const datePickerSave = document.getElementById('datePickerSave');
    const daySelect = document.getElementById('daySelect');
    const monthSelect = document.getElementById('monthSelect');
    const yearSelect = document.getElementById('yearSelect');

    const today = new Date();
    const currentDay = today.getDate();
    const currentMonth = today.getMonth() + 1;
    const currentYear = today.getFullYear();

    function populateDays() {
        const previousDay = parseInt(daySelect.value) || 1;
        daySelect.innerHTML = '';

        const month = parseInt(monthSelect.value);
        const year = parseInt(yearSelect.value);
        let daysInMonth = new Date(year, month, 0).getDate();

        if (year === currentYear && month === currentMonth) {
            daysInMonth = Math.min(daysInMonth, currentDay);
        }

        for (let i = 1; i <= daysInMonth; i++) {
            const option = document.createElement('option');
            option.value = i;
            option.textContent = i;
            daySelect.appendChild(option);
        }

        if (previousDay <= daysInMonth) {
            daySelect.value = previousDay;
        } else {
            daySelect.value = daysInMonth;
        }
    }

    function populateMonths() {
        const previousMonth = parseInt(monthSelect.value) || currentMonth;
        monthSelect.innerHTML = '';

        const year = parseInt(yearSelect.value);
        const maxMonth = (year === currentYear) ? currentMonth : 12;

        const monthNames = ['January', 'February', 'March', 'April', 'May', 'June',
            'July', 'August', 'September', 'October', 'November', 'December'];

        for (let i = 1; i <= maxMonth; i++) {
            const option = document.createElement('option');
            option.value = i;
            option.textContent = monthNames[i - 1];
            monthSelect.appendChild(option);
        }

        if (previousMonth <= maxMonth) {
            monthSelect.value = previousMonth;
        } else {
            monthSelect.value = maxMonth;
        }
    }

    function populateYears() {
        yearSelect.innerHTML = '';
        for (let i = currentYear; i >= 2020; i--) {
            const option = document.createElement('option');
            option.value = i;
            option.textContent = i;
            yearSelect.appendChild(option);
        }
    }

    if (yearSelect && monthSelect && daySelect) {
        populateYears();
        populateMonths();
        populateDays();

        yearSelect.addEventListener('change', function () {
            populateMonths();
            populateDays();
        });

        monthSelect.addEventListener('change', populateDays);
    }

    function openDatePicker() {
        if (datePickerModal) {
            datePickerModal.classList.add('active');
        }
    }

    function closeDatePicker() {
        if (datePickerModal) {
            datePickerModal.classList.remove('active');
        }
    }

    if (datePickerOverlay) {
        datePickerOverlay.addEventListener('click', closeDatePicker);
    }

    if (datePickerSave) {
        datePickerSave.addEventListener('click', function () {
            const day = String(daySelect.value).padStart(2, '0');
            const month = String(monthSelect.value).padStart(2, '0');
            const year = yearSelect.value;

            currentFilters.customDate = `${year}-${month}-${day}`;
            currentFilters.timePeriod = 'custom';

            const timePeriodBtns = document.querySelectorAll('[data-filter="timePeriod"]');
            timePeriodBtns.forEach(btn => {
                btn.classList.remove('active');
                if (btn.dataset.value === 'custom') {
                    btn.classList.add('active');
                }
            });

            closeDatePicker();
            applyFilters();
        });
    }

    // Sort buttons
    sortBtns.forEach(btn => {
        btn.addEventListener('click', function () {
            sortBtns.forEach(b => b.classList.remove('active'));
            this.classList.add('active');
            currentFilters.sortBy = this.dataset.sort;
            applyFilters();
        });
    });

    // Clear all filters
    if (clearFilters) {
        clearFilters.addEventListener('click', function () {
            currentFilters = {
                search: '',
                timePeriod: '',
                categoryId: '',
                sortBy: 'newest'
            };
            if (searchInput) searchInput.value = '';
            filterBtns.forEach(btn => btn.classList.remove('active'));
            sortBtns.forEach(btn => {
                btn.classList.remove('active');
                if (btn.dataset.sort === 'newest') btn.classList.add('active');
            });
            applyFilters();
        });
    }

    function applyFilters() {
        const params = new URLSearchParams();
        if (currentFilters.search) params.set('search', currentFilters.search);
        if (currentFilters.timePeriod) params.set('timePeriod', currentFilters.timePeriod);
        if (currentFilters.customDate) params.set('customDate', currentFilters.customDate);
        if (currentFilters.categoryId) params.set('categoryId', currentFilters.categoryId);
        if (currentFilters.sortBy) params.set('sortBy', currentFilters.sortBy);
        if (currentFilters.tab) params.set('tab', currentFilters.tab);

        window.location.href = '/Home/Archive?' + params.toString();
    }

    // Load more functionality
    if (loadMoreBtn) {
        loadMoreBtn.addEventListener('click', function () {
            const page = parseInt(this.dataset.page);
            this.disabled = true;
            this.innerHTML = '<span class="loading-spinner"></span> Loading...';

            const params = new URLSearchParams();
            params.set('page', page);
            if (currentFilters.search) params.set('search', currentFilters.search);
            if (currentFilters.timePeriod) params.set('timePeriod', currentFilters.timePeriod);
            if (currentFilters.categoryId) params.set('categoryId', currentFilters.categoryId);
            if (currentFilters.sortBy) params.set('sortBy', currentFilters.sortBy);

            fetch('/Home/LoadMoreArchive?' + params.toString())
                .then(response => response.json())
                .then(data => {
                    const container = document.getElementById('blogContainer');
                    const loadMoreContainer = document.getElementById('loadMoreContainer');

                    data.posts.forEach(post => {
                        const article = document.createElement('article');
                        article.className = 'blog-card';
                        article.dataset.blogId = post.id;
                        article.innerHTML = `
                            <h2 class="blog-title">${post.title}</h2>
                            <p class="blog-author"><a href="/Home/AuthorProfile/${post.authorId}" style="color: inherit; text-decoration: none;">${post.authorName}</a></p>
                            <p class="blog-summary">${post.summary}</p>
                            <a href="/Home/BlogDetail/${post.id}" class="read-more-btn">Continue Reading</a>
                        `;
                        container.insertBefore(article, loadMoreContainer);
                    });

                    if (data.hasMore) {
                        loadMoreBtn.dataset.page = page + 1;
                        loadMoreBtn.disabled = false;
                        loadMoreBtn.textContent = 'Load More';
                    } else {
                        loadMoreContainer.style.display = 'none';
                    }
                })
                .catch(error => {
                    console.error('Error loading more posts:', error);
                    loadMoreBtn.disabled = false;
                    loadMoreBtn.textContent = 'Load More';
                });
        });
    }

    // FAB click handler
    const createBtn = document.getElementById('createBtn');
    if (createBtn) {
        createBtn.addEventListener('click', function () {
            window.location.href = '/Blog/Create';
        });
    }
});
