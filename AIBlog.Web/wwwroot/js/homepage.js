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
                const response = await fetch(`/Blog/LoadMorePosts?page=${page}`);
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

    function createBlogCard(blog) {
        return `
            <article class="blog-card" data-blog-id="${blog.id}">
                <h2 class="blog-title">${escapeHtml(blog.title)}</h2>
                <p class="blog-author"><a href="/Home/AuthorProfile/${blog.authorId}" style="color: inherit; text-decoration: none;">${escapeHtml(blog.authorName)}</a></p>
                <p class="blog-summary">${escapeHtml(blog.summary)}</p>
                <a href="/Home/BlogDetail/${blog.id}" class="read-more-btn">Read More</a>
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

    // Follow button handler in Recommended Authors
    document.querySelectorAll('.authors-section .follow-btn').forEach(btn => {
        btn.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();
            const authorId = this.getAttribute('data-author-id');
            const authorItem = this.closest('.author-item');
            const button = this;

            // Disable button
            button.disabled = true;
            button.textContent = 'Following...';

            fetch('/Home/ToggleFollow', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ authorId: parseInt(authorId) })
            })
                .then(res => res.json())
                .then(data => {
                    if (data.success && data.isFollowing) {
                        button.textContent = 'Followed ✓';
                        button.style.background = '#4a3f5c';
                        button.style.color = '#fff';

                        // After 1 second, replace with random author
                        setTimeout(() => {
                            // Collect all visible author IDs
                            const visibleIds = [];
                            document.querySelectorAll('.authors-section .author-item').forEach(item => {
                                visibleIds.push(item.getAttribute('data-author-id'));
                            });

                            // Fade out
                            authorItem.style.transition = 'opacity 0.3s ease, transform 0.3s ease';
                            authorItem.style.opacity = '0';
                            authorItem.style.transform = 'translateX(20px)';

                            // Fetch replacement
                            fetch('/Home/GetRandomAuthor?excludeIds=' + visibleIds.join(','))
                                .then(res => res.json())
                                .then(result => {
                                    setTimeout(() => {
                                        if (result.success) {
                                            const avatarHtml = result.author.avatar
                                                ? `<img src="${result.author.avatar}" alt="${escapeHtml(result.author.name)}" />`
                                                : `<span>${result.author.name.charAt(0).toUpperCase()}</span>`;

                                            authorItem.setAttribute('data-author-id', result.author.id);
                                            authorItem.innerHTML = `
                                        <a href="/Home/AuthorProfile/${result.author.id}" class="author-info" style="text-decoration: none; color: inherit;">
                                            <div class="author-avatar">${avatarHtml}</div>
                                            <span class="author-name">${escapeHtml(result.author.name)}</span>
                                        </a>
                                        <button class="follow-btn" data-author-id="${result.author.id}">Follow</button>
                                    `;

                                            // Re-attach event listener to new button
                                            const newBtn = authorItem.querySelector('.follow-btn');
                                            newBtn.addEventListener('click', arguments.callee);

                                            // Fade in
                                            authorItem.style.opacity = '0';
                                            authorItem.style.transform = 'translateX(-20px)';
                                            requestAnimationFrame(() => {
                                                authorItem.style.opacity = '1';
                                                authorItem.style.transform = 'translateX(0)';
                                            });
                                        } else {
                                            // No more authors, just remove
                                            authorItem.remove();
                                        }
                                    }, 300);
                                });
                        }, 1000);
                    }
                })
                .catch(() => {
                    button.disabled = false;
                    button.textContent = 'Follow';
                });
        });
    });
});
