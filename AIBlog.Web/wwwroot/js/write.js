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
        document.execCommand('insertUnorderedList', false, null);
        contentEditor.focus();
    });

    document.getElementById('undoBtn')?.addEventListener('click', () => {
        document.execCommand('undo', false, null);
        contentEditor.focus();
    });

    document.getElementById('redoBtn')?.addEventListener('click', () => {
        document.execCommand('redo', false, null);
        contentEditor.focus();
    });

    document.getElementById('imageBtn')?.addEventListener('click', () => {
        const url = prompt('Enter image URL:');
        if (url) {
            document.execCommand('insertImage', false, url);
            contentEditor.focus();
        }
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

        // Add click handlers
        suggestionsList.querySelectorAll('.suggestion-item').forEach(item => {
            item.addEventListener('click', () => {
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
        const selection = window.getSelection();
        if (selection.rangeCount > 0) {
            const range = selection.getRangeAt(0);

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

        // Wait 300ms after user stops typing (shorter for responsiveness)
        debounceTimer = setTimeout(async () => {
            showLoading();
            // Send ENTIRE text to AI for full context analysis
            const predictions = await fetchAIPredictions(fullText);
            showSuggestions(predictions);
        }, 300);
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
    // Save and Publish - DISABLED FOR NOW
    // ========================================

    document.getElementById('saveBtn')?.addEventListener('click', function () {
        // Disabled - just show message
        alert('Save functionality coming soon!');
    });

    document.getElementById('publishBtn')?.addEventListener('click', function () {
        // Disabled - just show message
        alert('Publish functionality coming soon!');
    });
});
