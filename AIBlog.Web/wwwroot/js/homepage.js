// Homepage JavaScript - Load More functionality

document.addEventListener('DOMContentLoaded', function () {
    const loadMoreBtn = document.getElementById('loadMoreBtn');
    const blogContainer = document.getElementById('blogContainer');
    const loadMoreContainer = document.getElementById('loadMoreContainer');

    if (loadMoreBtn) {
        loadMoreBtn.addEventListener('click', async function () {
            const page = parseInt(this.dataset.page);

            // Show loading state
            loadMoreBtn.disabled = true;
            loadMoreBtn.innerHTML = '<span class="loading-spinner"></span> Loading...';

            try {
                const response = await fetch(`/Home/LoadMorePosts?page=${page}`);
                const data = await response.json();

                if (data.posts && data.posts.length > 0) {
                    // Create and append new blog cards
                    data.posts.forEach(blog => {
                        const card = createBlogCard(blog);
                        // Insert before load more container
                        loadMoreContainer.insertAdjacentHTML('beforebegin', card);
                    });

                    // Update page number
                    loadMoreBtn.dataset.page = page + 1;

                    // Hide button if no more posts
                    if (!data.hasMore) {
                        loadMoreContainer.style.display = 'none';
                    }
                }
            } catch (error) {
                console.error('Error loading more posts:', error);
                loadMoreBtn.textContent = 'Error - Try Again';
            } finally {
                // Reset button state
                loadMoreBtn.disabled = false;
                loadMoreBtn.textContent = 'Load More';
            }
        });
    }

    // Create blog card HTML
    function createBlogCard(blog) {
        return `
            <article class="blog-card" data-blog-id="${blog.id}">
                <h2 class="blog-title">${escapeHtml(blog.title)}</h2>
                <p class="blog-author">${escapeHtml(blog.authorName)}</p>
                <p class="blog-summary">${escapeHtml(blog.summary)}</p>
                <a href="#" class="read-more-btn">Read More</a>
            </article>
        `;
    }

    // Helper function to escape HTML
    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    // FAB button click handler
    const fabBtn = document.getElementById('fabBtn');
    if (fabBtn) {
        fabBtn.addEventListener('click', function () {
            // Navigate to create new blog post page
            window.location.href = '/Blog/Create';
        });
    }

    // Add smooth scroll behavior
    document.querySelectorAll('.nav-item').forEach(item => {
        item.addEventListener('click', function (e) {
            // Remove active class from all items
            document.querySelectorAll('.nav-item').forEach(i => i.classList.remove('active'));
            // Add active class to clicked item
            this.classList.add('active');
        });
    });
});
