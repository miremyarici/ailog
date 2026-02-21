$(document).ready(function () {
    // Submit top-level comment
    $('#commentForm').on('submit', function (e) {
        e.preventDefault();
        var content = $('#commentText').val().trim();
        if (!content) return;

        var blogPostId = $(this).data('post-id');

        $.ajax({
            url: '/Home/AddComment',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                blogPostId: blogPostId,
                content: content,
                parentCommentId: null
            }),
            success: function (response) {
                if (response.success) {
                    // Add comment to the list
                    var html = buildCommentHtml(response.comment);
                    $('.comment-list').prepend(html);
                    $('#commentText').val('');

                    // Update comment count
                    var countEl = $('#commentCount');
                    countEl.text(parseInt(countEl.text()) + 1);
                }
            }
        });
    });

    // Toggle reply form
    $(document).on('click', '.reply-btn', function () {
        var commentId = $(this).data('comment-id');
        var form = $('#replyForm-' + commentId);

        // Close all other reply forms
        $('.reply-form').not(form).removeClass('active');

        form.toggleClass('active');
        if (form.hasClass('active')) {
            form.find('textarea').focus();
        }
    });

    // Cancel reply
    $(document).on('click', '.reply-cancel-btn', function () {
        $(this).closest('.reply-form').removeClass('active');
    });

    // Submit reply
    $(document).on('click', '.reply-submit-btn', function () {
        var form = $(this).closest('.reply-form');
        var content = form.find('textarea').val().trim();
        if (!content) return;

        var parentCommentId = form.data('parent-id');
        var blogPostId = form.data('post-id');

        $.ajax({
            url: '/Home/AddComment',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                blogPostId: blogPostId,
                content: content,
                parentCommentId: parentCommentId
            }),
            success: function (response) {
                if (response.success) {
                    var html = buildCommentHtml(response.comment);

                    // Find or create replies list
                    var repliesList = form.siblings('.replies-list');
                    if (repliesList.length === 0) {
                        repliesList = $('<div class="replies-list"></div>');
                        form.before(repliesList);
                    }
                    repliesList.append(html);

                    form.find('textarea').val('');
                    form.removeClass('active');

                    var countEl = $('#commentCount');
                    countEl.text(parseInt(countEl.text()) + 1);
                }
            }
        });
    });

    function buildCommentHtml(c) {
        var avatarHtml = c.authorAvatar
            ? '<img src="' + c.authorAvatar + '" alt="' + c.authorName + '" />'
            : '<span>' + c.authorName.charAt(0).toUpperCase() + '</span>';

        return '<div class="comment-item" data-comment-id="' + c.id + '">' +
            '<div class="comment-header">' +
            '<div class="comment-avatar">' + avatarHtml + '</div>' +
            '<span class="comment-author-name">' + c.authorName + '</span>' +
            '<span class="comment-date">Just now</span>' +
            '</div>' +
            '<p class="comment-content">' + escapeHtml(c.content) + '</p>' +
            '<button class="reply-btn" data-comment-id="' + c.id + '">Reply</button>' +
            '<div class="reply-form" id="replyForm-' + c.id + '" data-parent-id="' + c.id + '" data-post-id="' + c.blogPostId + '">' +
            '<textarea class="reply-textarea" placeholder="Write a reply..."></textarea>' +
            '<div class="reply-actions">' +
            '<button type="button" class="reply-cancel-btn">Cancel</button>' +
            '<button type="button" class="reply-submit-btn">Reply</button>' +
            '</div>' +
            '</div>' +
            '</div>';
    }

    function escapeHtml(str) {
        var div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }
});
