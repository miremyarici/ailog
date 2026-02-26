// Explore page JavaScript
document.addEventListener('DOMContentLoaded', function () {
    const searchInput = document.getElementById('searchInput');
    const searchBtn = document.getElementById('searchBtn');
    const clearFilters = document.getElementById('clearFilters');
    const filterBtns = document.querySelectorAll('.filter-btn');
    const sortBtns = document.querySelectorAll('.sort-btn');
    const loadMoreBtn = document.getElementById('loadMoreBtn');

    // Current filter state - default sort is most-popular for Explore
    let currentFilters = {
        search: searchInput ? searchInput.value : '',
        timePeriod: '',
        categoryId: '',
        sortBy: 'most-popular'
    };

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
    const currentMonth = today.getMonth() + 1; // 1-12
    const currentYear = today.getFullYear();

    // Populate day select with future date restriction
    function populateDays() {
        const previousDay = parseInt(daySelect.value) || 1; // Preserve selected day
        daySelect.innerHTML = '';

        const month = parseInt(monthSelect.value);
        const year = parseInt(yearSelect.value);
        let daysInMonth = new Date(year, month, 0).getDate();

        // If current year and month, limit days to today
        if (year === currentYear && month === currentMonth) {
            daysInMonth = Math.min(daysInMonth, currentDay);
        }

        for (let i = 1; i <= daysInMonth; i++) {
            const option = document.createElement('option');
            option.value = i;
            option.textContent = i;
            daySelect.appendChild(option);
        }

        // Restore previous day if valid, otherwise use last available day
        if (previousDay <= daysInMonth) {
            daySelect.value = previousDay;
        } else {
            daySelect.value = daysInMonth;
        }
    }

    // Populate month select with future date restriction
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

        // Restore previous month if valid
        if (previousMonth <= maxMonth) {
            monthSelect.value = previousMonth;
        } else {
            monthSelect.value = maxMonth;
        }
    }

    // Populate year select (2020 - current year)
    function populateYears() {
        yearSelect.innerHTML = '';
        for (let i = currentYear; i >= 2020; i--) {
            const option = document.createElement('option');
            option.value = i;
            option.textContent = i;
            yearSelect.appendChild(option);
        }
    }

    // Initialize date picker
    if (yearSelect && monthSelect && daySelect) {
        populateYears();
        populateMonths();
        populateDays();

        // Update months and days when year changes
        yearSelect.addEventListener('change', function () {
            populateMonths();
            populateDays();
        });

        // Update days when month changes
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

    // Close modal on overlay click
    if (datePickerOverlay) {
        datePickerOverlay.addEventListener('click', closeDatePicker);
    }

    // Save date and apply filter
    if (datePickerSave) {
        datePickerSave.addEventListener('click', function () {
            const day = String(daySelect.value).padStart(2, '0');
            const month = String(monthSelect.value).padStart(2, '0');
            const year = yearSelect.value;

            // Set custom date filter
            currentFilters.customDate = `${year}-${month}-${day}`;
            currentFilters.timePeriod = 'custom';

            // Update button states
            const timePeriodBtns = document.querySelectorAll('[data-filter="timePeriod"]');
            timePeriodBtns.forEach(btn => {
                btn.classList.remove('active');
                if (btn.dataset.value === '') {
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
                sortBy: 'most-popular'
            };
            if (searchInput) searchInput.value = '';
            filterBtns.forEach(btn => btn.classList.remove('active'));
            sortBtns.forEach(btn => {
                btn.classList.remove('active');
                if (btn.dataset.sort === 'most-popular') btn.classList.add('active');
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

        // Fetch results via AJAX
        fetch('/Home/SearchExplore?' + params.toString())
            .then(res => res.json())
            .then(data => {
                const container = document.getElementById('blogContainer');
                container.innerHTML = ''; // Clear container

                let htmlContent = '';

                // Handle Users Section if search exists
                if (data.query) {
                    htmlContent += '<div class="search-results-section"><h3 class="section-title">Users</h3>';

                    if (data.authors && data.authors.length > 0) {
                        htmlContent += '<div class="authors-grid" style="display: flex; flex-direction: column; gap: 12px; margin-bottom: 32px;">';
                        data.authors.forEach(author => {
                            htmlContent += `
                                <a href="/Profile/AuthorProfile/${author.id}" class="author-info" style="text-decoration: none; color: inherit; display: flex; align-items: center; gap: 12px; padding: 12px; background: var(--bg-card); border-radius: 12px; border: 1px solid var(--border-color); transition: transform 0.2s, box-shadow 0.2s;">
                                    <div class="author-avatar" style="width: 40px; height: 40px; border-radius: 50%; overflow: hidden; background: var(--bg-card-alt); display: flex; align-items: center; justify-content: center; flex-shrink: 0;">
                                        ${author.avatar
                                    ? `<img src="${author.avatar}" alt="${author.name}" style="width: 100%; height: 100%; object-fit: cover;" />`
                                    : `<span style="font-size: 1rem; font-weight: 600; color: var(--text-secondary);">${author.initials}</span>`}
                                    </div>
                                    <span class="author-name" style="font-weight: 600; font-size: 1rem;">${author.name}</span>
                                </a>
                            `;
                        });
                        htmlContent += '</div>';
                    } else {
                        htmlContent += `
                            <div class="empty-state" style="margin-bottom: 32px; padding: 24px; text-align: left; background: transparent; border: 1px dashed var(--border-color);">
                                <p>No users found matching "${data.query}".</p>
                            </div>
                        `;
                    }
                    htmlContent += '<h3 class="section-title">Articles</h3></div>';
                }

                // Handle Articles Section
                if (data.blogs && data.blogs.length > 0) {
                    data.blogs.forEach(post => {
                        htmlContent += `
                            <article class="blog-card" data-blog-id="${post.id}">
                                <h2 class="blog-title">${post.title}</h2>
                                <p class="blog-author"><a href="/Profile/AuthorProfile/${post.authorId}" style="color: inherit; text-decoration: none;">${post.authorName}</a></p>
                                <p class="blog-summary">${post.summary}</p>
                                <a href="/Blog/BlogDetail/${post.id}" class="read-more-btn">Read More</a>
                            </article>
                        `;
                    });

                    // Add Load More indicator if hasMore
                    if (data.hasMore) {
                        htmlContent += `
                            <div class="load-more-container" id="loadMoreContainer">
                                <button class="load-more-btn" id="loadMoreBtn" data-page="2">
                                    Load More
                                </button>
                            </div>
                        `;
                    }
                } else {
                    htmlContent += `
                        <div class="empty-state">
                            ${data.query
                            ? `<h2>No articles found</h2>
                                   <p>Try different keywords or filters.</p>
                                   <a href="/Home/Explore" class="read-more-btn" style="margin-top: 16px;">Clear Search</a>`
                            : `<h2>No trending posts found</h2>
                                   <p>Check back later for trending content!</p>
                                   <a href="/Home/Index" class="read-more-btn">Browse Articles</a>`}
                        </div>
                    `;
                }

                container.innerHTML = htmlContent;

                // Re-attach Load More logic if button exists
                const newLoadMoreBtn = document.getElementById('loadMoreBtn');
                if (newLoadMoreBtn) {
                    newLoadMoreBtn.addEventListener('click', handleLoadMore);
                }
            })
            .catch(error => {
                console.error("Error applying filters:", error);
            });
    }

    // Extracted logic to be attached to dynamic load more buttons
    function handleLoadMore() {
        const page = parseInt(this.dataset.page);
        this.disabled = true;
        this.innerHTML = '<span class="loading-spinner"></span> Loading...';

        const params = new URLSearchParams();
        params.set('page', page);
        if (currentFilters.search) params.set('search', currentFilters.search);
        if (currentFilters.timePeriod) params.set('timePeriod', currentFilters.timePeriod);
        if (currentFilters.categoryId) params.set('categoryId', currentFilters.categoryId);
        if (currentFilters.sortBy) params.set('sortBy', currentFilters.sortBy);

        fetch('/Home/LoadMoreExplore?' + params.toString())
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
                        <p class="blog-author"><a href="/Profile/AuthorProfile/${post.authorId}" style="color: inherit; text-decoration: none;">${post.authorName}</a></p>
                        <p class="blog-summary">${post.summary}</p>
                        <a href="/Blog/BlogDetail/${post.id}" class="read-more-btn">Read More</a>
                    `;
                    container.insertBefore(article, loadMoreContainer);
                });

                if (data.hasMore) {
                    this.dataset.page = page + 1;
                    this.disabled = false;
                    this.textContent = 'Load More';
                } else {
                    loadMoreContainer.style.display = 'none';
                }
            })
            .catch(error => {
                console.error('Error loading more posts:', error);
                this.disabled = false;
                this.textContent = 'Load More';
            });
    }

    // Initial Load More attachment
    if (loadMoreBtn) {
        loadMoreBtn.addEventListener('click', handleLoadMore);
    }

    // FAB click handler
    const createBtn = document.getElementById('createBtn');
    if (createBtn) {
        createBtn.addEventListener('click', function () {
            window.location.href = '/Blog/Create';
        });
    }
});
