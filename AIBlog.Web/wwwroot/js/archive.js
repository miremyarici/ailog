// Archive page JavaScript
document.addEventListener('DOMContentLoaded', function () {
    const searchInput = document.getElementById('searchInput');
    const searchBtn = document.getElementById('searchBtn');
    const clearFilters = document.getElementById('clearFilters');
    const filterBtns = document.querySelectorAll('.filter-btn');
    const sortBtns = document.querySelectorAll('.sort-btn');
    const loadMoreBtn = document.getElementById('loadMoreBtn');

    // Current filter state
    let currentFilters = {
        search: searchInput ? searchInput.value : '',
        timePeriod: '',
        categoryId: '',
        sortBy: 'newest'
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
        if (currentFilters.categoryId) params.set('categoryId', currentFilters.categoryId);
        if (currentFilters.sortBy) params.set('sortBy', currentFilters.sortBy);

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
                            <p class="blog-author">${post.authorName}</p>
                            <p class="blog-summary">${post.summary}</p>
                            <a href="#" class="read-more-btn">Continue Reading</a>
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
