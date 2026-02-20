// Write/Blog Editor JavaScript with AI Word Predictions
document.addEventListener('DOMContentLoaded', function () {
    const contentEditor = document.getElementById('contentEditor');
    const titleInput = document.getElementById('titleInput');
    const aiSuggestions = document.getElementById('aiSuggestions');
    const suggestionsList = document.getElementById('suggestionsList');
    const categorySelect = document.getElementById('categorySelect');

    // AI Service configuration - Google Colab ngrok URL
    const AI_SERVICE_URL = 'https://excursional-elease-undistrustfully.ngrok-free.dev/api/predict';
    const MIN_WORDS_FOR_AI = 5;

    let currentSuggestions = [];
    let selectedIndex = -1;
    let debounceTimer = null;
    let lastText = '';
    let isAIActive = false;
    let savedRange = null;

    // Save the editor selection/range whenever it changes so we can restore it
    // when the user clicks a suggestion (which steals focus from the editor)
    document.addEventListener('selectionchange', function () {
        const selection = window.getSelection();
        if (selection.rangeCount > 0) {
            const range = selection.getRangeAt(0);
            // Only save if the selection is inside the content editor
            if (contentEditor.contains(range.startContainer)) {
                savedRange = range.cloneRange();
            }
        }
    });

    // Check if AI service is available
    async function checkAIStatus() {
        const aiBadge = document.getElementById('aiBadge');
        const aiDot = aiBadge?.querySelector('.ai-dot');
        const aiStatusText = document.getElementById('aiStatusText');

        try {
            const response = await fetch(AI_SERVICE_URL, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'ngrok-skip-browser-warning': 'true'
                },
                body: JSON.stringify({ text: 'test', count: 1 })
            });

            if (response.ok) {
                isAIActive = true;
                if (aiDot) aiDot.classList.remove('offline');
                if (aiStatusText) aiStatusText.textContent = 'AI is active';
            } else {
                throw new Error('AI service not available');
            }
        } catch (error) {
            isAIActive = false;
            if (aiDot) aiDot.classList.add('offline');
            if (aiStatusText) aiStatusText.textContent = 'AI is not active';
        }
    }

    // Check AI status on page load
    checkAIStatus();

    // ========================================
    // Toolbar Functions
    // ========================================

    document.getElementById('boldBtn')?.addEventListener('click', () => {
        document.execCommand('bold', false, null);
        contentEditor.focus();
    });

    document.getElementById('italicBtn')?.addEventListener('click', () => {
        document.execCommand('italic', false, null);
        contentEditor.focus();
    });

    document.getElementById('underlineBtn')?.addEventListener('click', () => {
        document.execCommand('underline', false, null);
        contentEditor.focus();
    });

    document.getElementById('strikeBtn')?.addEventListener('click', () => {
        document.execCommand('strikeThrough', false, null);
        contentEditor.focus();
    });

    document.getElementById('listBtn')?.addEventListener('click', () => {
        contentEditor.focus();
        const selection = window.getSelection();
        if (!selection.rangeCount) return;

        const range = selection.getRangeAt(0);

        // Get the text node(s) in the selection
        // Find the start and end block-level elements
        let startNode = range.startContainer;
        let endNode = range.endContainer;

        // Walk up to direct children of contentEditor
        function getLineNode(node) {
            while (node && node.parentNode !== contentEditor) {
                node = node.parentNode;
            }
            return node;
        }

        const startLine = getLineNode(startNode);
        const endLine = getLineNode(endNode);

        // Collect all line nodes in range
        const lines = [];
        let current = startLine;
        while (current) {
            lines.push(current);
            if (current === endLine) break;
            current = current.nextSibling;
        }

        // Toggle ● prefix on each line
        lines.forEach(line => {
            if (line.nodeType === Node.TEXT_NODE) {
                const text = line.textContent;
                if (text.startsWith('● ')) {
                    line.textContent = text.substring(2);
                } else {
                    line.textContent = '● ' + text;
                }
            } else if (line.nodeType === Node.ELEMENT_NODE) {
                const text = line.textContent;
                if (text.startsWith('● ')) {
                    line.innerHTML = line.innerHTML.replace('● ', '');
                } else {
                    line.insertBefore(document.createTextNode('● '), line.firstChild);
                }
            }
        });
    });

    // Insert Image from device
    const imageFileInput = document.getElementById('imageFileInput');
    document.getElementById('imageBtn')?.addEventListener('click', (e) => {
        e.preventDefault();
        if (imageFileInput) {
            imageFileInput.click();
        }
    });

    imageFileInput?.addEventListener('change', function () {
        const file = this.files[0];
        if (!file) return;

        const reader = new FileReader();
        reader.onload = function (e) {
            contentEditor.focus();

            // Restore cursor position
            if (savedRange) {
                const selection = window.getSelection();
                selection.removeAllRanges();
                selection.addRange(savedRange);
            }

            // Create img element and insert at cursor
            const img = document.createElement('img');
            img.src = e.target.result;
            img.style.maxWidth = '100%';
            img.style.borderRadius = '8px';
            img.style.margin = '8px 0';

            const selection = window.getSelection();
            if (selection.rangeCount) {
                const range = selection.getRangeAt(0);
                range.deleteContents();
                range.insertNode(img);

                // Move cursor after the image
                range.setStartAfter(img);
                range.collapse(true);
                selection.removeAllRanges();
                selection.addRange(range);
            } else {
                contentEditor.appendChild(img);
            }
        };
        reader.readAsDataURL(file);
        // Reset so the same file can be selected again
        this.value = '';
    });

    const undoBtn = document.getElementById('undoBtn');
    const redoBtn = document.getElementById('redoBtn');
    let undoStack = [];
    let redoStack = [];
    let isUndoRedo = false;

    // Disable both buttons initially
    if (undoBtn) {
        undoBtn.disabled = true;
        undoBtn.classList.add('disabled');
    }
    if (redoBtn) {
        redoBtn.disabled = true;
        redoBtn.classList.add('disabled');
    }

    function updateButtonStates() {
        if (undoBtn) {
            undoBtn.disabled = undoStack.length === 0;
            undoBtn.classList.toggle('disabled', undoStack.length === 0);
        }
        if (redoBtn) {
            redoBtn.disabled = redoStack.length === 0;
            redoBtn.classList.toggle('disabled', redoStack.length === 0);
        }
    }

    // Track content changes for undo
    contentEditor.addEventListener('input', () => {
        if (isUndoRedo) return;

        undoStack.push(contentEditor.innerHTML);
        redoStack = []; // Clear redo on new input
        updateButtonStates();
    });

    // Save initial empty state
    undoStack.push('');

    undoBtn?.addEventListener('click', () => {
        if (undoStack.length > 1) {
            isUndoRedo = true;
            const current = undoStack.pop();
            redoStack.push(current);
            contentEditor.innerHTML = undoStack[undoStack.length - 1];
            updateButtonStates();
            isUndoRedo = false;
        }
        contentEditor.focus();
    });

    redoBtn?.addEventListener('click', () => {
        if (redoStack.length > 0) {
            isUndoRedo = true;
            const redoContent = redoStack.pop();
            undoStack.push(redoContent);
            contentEditor.innerHTML = redoContent;
            updateButtonStates();
            isUndoRedo = false;
        }
        contentEditor.focus();
    });

    // ========================================
    // AI Word Prediction - Full Context
    // ========================================

    function getWordCount(text) {
        const cleanText = text.replace(/<[^>]*>/g, ' ').trim();
        return cleanText.split(/\s+/).filter(word => word.length > 0).length;
    }

    function getPlainText() {
        return contentEditor.innerText || contentEditor.textContent || '';
    }

    // Get full text content for AI context - includes ALL previous text
    function getFullContextForAI() {
        // Get the entire text content from the editor
        const fullText = getPlainText().trim();

        // Return all text - AI will analyze the complete context
        return fullText;
    }

    async function fetchAIPredictions(text) {
        try {
            console.log('Sending to AI:', text); // Debug log

            const response = await fetch(AI_SERVICE_URL, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'ngrok-skip-browser-warning': 'true' // Skip ngrok warning page
                },
                body: JSON.stringify({
                    text: text, // Send FULL text for context
                    count: 5
                })
            });

            if (!response.ok) {
                console.error('AI service error:', response.status);
                throw new Error('AI service error');
            }

            const data = await response.json();
            console.log('AI response:', data); // Debug log

            if (data.success && data.predictions) {
                return data.predictions;
            }
            return [];
        } catch (error) {
            console.error('AI prediction error:', error);
            return [];
        }
    }

    function showSuggestions(predictions) {
        if (predictions.length === 0) {
            hideSuggestions();
            return;
        }

        currentSuggestions = predictions;
        selectedIndex = -1;

        suggestionsList.innerHTML = predictions.map((word, index) => `
            <div class="suggestion-item" data-index="${index}">
                <span class="suggestion-text">${word}</span>
                <span class="suggestion-key">${index + 1}</span>
            </div>
        `).join('');

        // Position suggestions near cursor
        const selection = window.getSelection();
        if (selection.rangeCount > 0) {
            const range = selection.getRangeAt(0);
            const rect = range.getBoundingClientRect();
            const editorRect = contentEditor.getBoundingClientRect();

            // Position below cursor
            let left = rect.left - editorRect.left;
            let top = rect.bottom - editorRect.top + 10;

            // Keep within editor bounds
            if (left < 0) left = 0;
            if (left > editorRect.width - 200) left = editorRect.width - 200;

            aiSuggestions.style.left = `${left}px`;
            aiSuggestions.style.top = `${top}px`;
        }

        aiSuggestions.style.display = 'block';

        // Use mousedown + preventDefault to prevent the click from stealing
        // focus away from the editor (which would lose the cursor position)
        suggestionsList.querySelectorAll('.suggestion-item').forEach(item => {
            item.addEventListener('mousedown', (e) => {
                e.preventDefault();
                insertSuggestion(predictions[parseInt(item.dataset.index)]);
            });
        });
    }

    function showLoading() {
        suggestionsList.innerHTML = '<div class="ai-loading">AI is thinking...</div>';
        aiSuggestions.style.display = 'block';
    }

    function hideSuggestions() {
        aiSuggestions.style.display = 'none';
        currentSuggestions = [];
        selectedIndex = -1;
    }

    function insertSuggestion(word) {
        // Restore the saved editor range (in case focus was lost by clicking)
        const selection = window.getSelection();
        let range = null;

        // Try to use the current selection if it's inside the editor
        if (selection.rangeCount > 0) {
            const currentRange = selection.getRangeAt(0);
            if (contentEditor.contains(currentRange.startContainer)) {
                range = currentRange;
            }
        }

        // If current selection is not in the editor, restore the saved range
        if (!range && savedRange) {
            range = savedRange.cloneRange();
        }

        if (range) {
            // Collapse range to end to ensure we insert at the cursor position
            range.collapse(false);

            // Check if we need a space before the word
            const textBefore = getPlainText();
            const needsSpace = textBefore.length > 0 && !textBefore.endsWith(' ') && !textBefore.endsWith('\n');

            const textNode = document.createTextNode((needsSpace ? ' ' : '') + word + ' ');
            range.insertNode(textNode);

            // Move cursor after inserted text
            range.setStartAfter(textNode);
            range.setEndAfter(textNode);
            selection.removeAllRanges();
            selection.addRange(range);
        }

        hideSuggestions();
        contentEditor.focus();

        // Trigger new predictions after a short delay
        setTimeout(() => handleContentChange(), 100);
    }

    function handleContentChange() {
        // Get FULL text for AI context analysis
        const fullText = getFullContextForAI();
        const wordCount = getWordCount(fullText);

        // Only show predictions after 5 words - then on EVERY keystroke
        if (wordCount < MIN_WORDS_FOR_AI) {
            hideSuggestions();
            lastText = '';
            return;
        }

        // Debounce API calls - shorter delay for responsive feel
        if (debounceTimer) clearTimeout(debounceTimer);

        // Check if text has changed (ignore if just moving cursor)
        if (fullText === lastText) return;
        lastText = fullText;

        // Wait 150ms after user stops typing for fast responsiveness
        debounceTimer = setTimeout(async () => {
            showLoading();
            // Send ENTIRE text to AI for full context analysis
            const predictions = await fetchAIPredictions(fullText);
            showSuggestions(predictions);
        }, 150);
    }

    // Listen for content changes
    contentEditor.addEventListener('input', handleContentChange);

    // Also trigger on focus to show predictions if text exists
    contentEditor.addEventListener('focus', () => {
        const text = getPlainText().trim();
        if (getWordCount(text) >= MIN_WORDS_FOR_AI) {
            handleContentChange();
        }
    });

    // Handle keyboard navigation in suggestions
    contentEditor.addEventListener('keydown', function (e) {
        if (aiSuggestions.style.display === 'none' || currentSuggestions.length === 0) {
            return;
        }

        // Number keys 1-5 to select suggestion
        if (e.key >= '1' && e.key <= '5') {
            const index = parseInt(e.key) - 1;
            if (index < currentSuggestions.length) {
                e.preventDefault();
                insertSuggestion(currentSuggestions[index]);
                return;
            }
        }

        // Arrow keys to navigate
        if (e.key === 'ArrowDown') {
            e.preventDefault();
            selectedIndex = Math.min(selectedIndex + 1, currentSuggestions.length - 1);
            updateSelection();
        } else if (e.key === 'ArrowUp') {
            e.preventDefault();
            selectedIndex = Math.max(selectedIndex - 1, 0);
            updateSelection();
        } else if (e.key === 'Enter' && selectedIndex >= 0) {
            e.preventDefault();
            insertSuggestion(currentSuggestions[selectedIndex]);
        } else if (e.key === 'Escape') {
            hideSuggestions();
        } else if (e.key === 'Tab' && selectedIndex >= 0) {
            e.preventDefault();
            insertSuggestion(currentSuggestions[selectedIndex]);
        }
    });

    function updateSelection() {
        suggestionsList.querySelectorAll('.suggestion-item').forEach((item, index) => {
            item.classList.toggle('selected', index === selectedIndex);
        });
    }

    // Hide suggestions when clicking outside
    document.addEventListener('click', function (e) {
        if (!aiSuggestions.contains(e.target) && e.target !== contentEditor) {
            hideSuggestions();
        }
    });

    // ========================================
    // Save and Publish
    // ========================================

    function saveBlog(isPublished) {
        const title = titleInput.value.trim();
        const content = contentEditor.innerHTML.trim();
        const categoryId = categorySelect ? parseInt(categorySelect.value) : 0;
        const editingPostId = document.getElementById('editingPostId')?.value;

        // Validation
        if (!title) {
            alert('Please enter a title.');
            titleInput.focus();
            return;
        }

        if (!content || content === '<br>') {
            alert('Please write some content.');
            contentEditor.focus();
            return;
        }

        if (!categoryId) {
            alert('Please choose a category.');
            categorySelect?.focus();
            return;
        }

        const payload = {
            title: title,
            content: content,
            categoryId: categoryId,
            isPublished: isPublished
        };

        // Include ID if editing an existing post
        if (editingPostId) {
            payload.id = parseInt(editingPostId);
        }

        fetch('/Home/SaveBlog', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    if (isPublished) {
                        alert('Your post has been published!');
                        window.location.href = '/Home/Profile';
                    } else {
                        alert('Your draft has been saved!');
                        window.location.href = '/Home/Archive?tab=drafts';
                    }
                } else {
                    alert('Error: ' + (data.error || 'Something went wrong.'));
                }
            })
            .catch(error => {
                console.error('Error saving blog:', error);
                alert('An error occurred while saving. Please try again.');
            });
    }

    document.getElementById('saveBtn')?.addEventListener('click', function () {
        saveBlog(false);
    });

    document.getElementById('publishBtn')?.addEventListener('click', function () {
        saveBlog(true);
    });
});
