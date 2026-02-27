// Write/Blog Editor JavaScript with AI Word Predictions
document.addEventListener('DOMContentLoaded', function () {
    const contentEditor = document.getElementById('contentEditor');
    const titleInput = document.getElementById('titleInput');
    const aiSuggestions = document.getElementById('aiSuggestions');
    const suggestionsList = document.getElementById('suggestionsList');
    const categorySelect = document.getElementById('categorySelect');

    const AI_SERVICE_URL = '/api/Ai/Predict'; // C# Controller üzerinden atıyoruz, doğrudan Ngrok'a DEĞİL!

    let currentSuggestions = [];
    let selectedIndex = -1;
    let debounceTimer = null;
    let isAIActive = true;
    let savedRange = null;

    // AI Rozetini Aktif Göster
    const aiBadge = document.getElementById('aiBadge');
    const aiDot = aiBadge?.querySelector('.ai-dot');
    const aiStatusText = document.getElementById('aiStatusText');

    // Check AI status from backend
    fetch('/Blog/CheckAiStatus')
        .then(res => res.json())
        .then(data => {
            if (data.isHealthy) {
                isAIActive = true;
                if (aiDot) aiDot.classList.remove('offline');
                if (aiStatusText) aiStatusText.textContent = 'AI is active';
            } else {
                isAIActive = false;
                if (aiDot) aiDot.classList.add('offline');
                if (aiStatusText) aiStatusText.textContent = 'AI is not active';
            }
        })
        .catch(err => {
            isAIActive = false;
            if (aiDot) aiDot.classList.add('offline');
            if (aiStatusText) aiStatusText.textContent = 'AI is not active';
            console.error('Failed to check AI status', err);
        });

    document.addEventListener('selectionchange', function () {
        const selection = window.getSelection();
        if (selection.rangeCount > 0) {
            const range = selection.getRangeAt(0);
            if (contentEditor.contains(range.startContainer)) {
                savedRange = range.cloneRange();
            }
        }
    });

    // ========================================
    // AI Word Prediction - Optimized Debounce
    // ========================================

    function getPlainText() {
        return contentEditor.innerText || contentEditor.textContent || '';
    }

    async function fetchAIPredictions(text) {
        if (!isAIActive) return [];

        try {
            console.log('🤖 Sending to AI:', text);

            // İstekleri Ngrok'a değil, kendi C# servisimize atıyoruz. Ngrok engelini o aşacak.
            const response = await fetch('/Blog/GetAiPrediction', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ text: text, count: 5 })
            });

            const data = await response.json();

            if (data.success && data.predictions) {
                console.log('✅ AI Response:', data.predictions);
                return data.predictions;
            }
            return [];
        } catch (error) {
            console.error('❌ AI prediction error:', error);
            return [];
        }
    }

    function showSuggestions(predictions) {
        if (!predictions || predictions.length === 0) {
            hideSuggestions();
            return;
        }

        currentSuggestions = predictions;
        selectedIndex = -1;

        let html = '';
        predictions.forEach((word, index) => {
            html += `
            <div class="suggestion-item" data-index="${index}" style="padding: 8px 12px; cursor: pointer; display: flex; justify-content: space-between; align-items: center; border-bottom: 1px solid #eee;">
                <span class="suggestion-text" style="font-weight: 500; color: #3d3350;">${word}</span>
                <span class="suggestion-key" style="background: #f0f0f0; padding: 2px 6px; border-radius: 4px; font-size: 12px; color: #666;">Tab or ${index + 1}</span>
            </div>`;
        });

        suggestionsList.innerHTML = html;

        // Position box near cursor
        const selection = window.getSelection();
        if (selection.rangeCount > 0) {
            const range = selection.getRangeAt(0);
            const rect = range.getBoundingClientRect();

            // Eğer yeni satırdaysa rect değerleri sıfır gelebilir, o yüzden editor pozisyonunu baz alıyoruz
            if (rect.x === 0 && rect.y === 0) {
                aiSuggestions.style.left = '50px';
                aiSuggestions.style.top = '100px';
            } else {
                aiSuggestions.style.left = `${rect.left}px`;
                aiSuggestions.style.top = `${rect.bottom + 5}px`;
            }
        }

        aiSuggestions.style.display = 'block';
        aiSuggestions.style.position = 'fixed'; // Ekranda sabit durması için
        aiSuggestions.style.backgroundColor = 'white';
        aiSuggestions.style.border = '1px solid #ddd';
        aiSuggestions.style.borderRadius = '8px';
        aiSuggestions.style.boxShadow = '0 4px 12px rgba(0,0,0,0.15)';
        aiSuggestions.style.zIndex = '1000';
        aiSuggestions.style.minWidth = '200px';

        suggestionsList.querySelectorAll('.suggestion-item').forEach(item => {
            item.addEventListener('mousedown', (e) => {
                e.preventDefault(); // Focus'un kaybolmasını engelle
                insertSuggestion(predictions[parseInt(item.dataset.index)]);
            });

            item.addEventListener('mouseenter', () => {
                item.style.backgroundColor = '#f5f0f8';
            });
            item.addEventListener('mouseleave', () => {
                item.style.backgroundColor = 'transparent';
            });
        });
    }

    function showLoading() {
        suggestionsList.innerHTML = '<div class="ai-loading" style="padding: 10px; color: #888; font-style: italic;">AI is thinking...</div>';
        aiSuggestions.style.display = 'block';
    }

    function hideSuggestions() {
        aiSuggestions.style.display = 'none';
        currentSuggestions = [];
        selectedIndex = -1;
    }

    function insertSuggestion(word) {
        if (!savedRange) return;

        const selection = window.getSelection();
        savedRange.collapse(false);

        // Sadece önerilen kelimeyi ve sonuna bir boşluk ekle
        const textNode = document.createTextNode(word + ' ');
        savedRange.insertNode(textNode);

        savedRange.setStartAfter(textNode);
        savedRange.setEndAfter(textNode);
        selection.removeAllRanges();
        selection.addRange(savedRange);

        hideSuggestions();
        contentEditor.focus();
    }

    // --- SİHİRLİ DEBOUNCE MEKANİZMASI ---
    contentEditor.addEventListener('keyup', function (e) {
        // Yön tuşları veya silme tuşlarında yapay zekayı tetikleme
        if (e.key === 'Backspace' || e.key === 'Delete' || e.key.includes('Arrow')) {
            hideSuggestions();
            return;
        }

        // Boşluk tuşuna basıldıysa (kelime bittiyse) VEYA 800ms duraksadıysa tetikle
        if (debounceTimer) clearTimeout(debounceTimer);

        if (e.key === ' ' || e.code === 'Space') {
            triggerAI();
        } else {
            // Kullanıcı yazmaya devam ediyorsa 800ms bekle. Duraksarsa tetikle.
            debounceTimer = setTimeout(() => {
                triggerAI();
            }, 800);
        }
    });

    function triggerAI() {
        // trim() KULLANMIYORUZ! Çünkü sondaki boşluk AI'a "sıradaki kelimeyi bul" demek için şart.
        const fullText = getPlainText();

        if (fullText.trim().length < 2) return; // Çok kısaysa istek atma

        showLoading();
        fetchAIPredictions(fullText).then(predictions => {
            showSuggestions(predictions);
        });
    }

    // --- KLAVYE KONTROLLERİ (TAB VE YÖN TUŞLARI) ---
    contentEditor.addEventListener('keydown', function (e) {
        if (aiSuggestions.style.display === 'none' || currentSuggestions.length === 0) return;

        if (e.key === 'Tab') {
            e.preventDefault();
            // Tab'a basılınca her zaman ilk öneriyi seç
            insertSuggestion(currentSuggestions[0]);
        }
        else if (e.key >= '1' && e.key <= '5') {
            e.preventDefault();
            const index = parseInt(e.key) - 1;
            if (index < currentSuggestions.length) {
                insertSuggestion(currentSuggestions[index]);
            }
        }
        else if (e.key === 'Escape') {
            hideSuggestions();
        }
    });

    document.addEventListener('click', function (e) {
        if (!aiSuggestions.contains(e.target) && e.target !== contentEditor) {
            hideSuggestions();
        }
    });

    // ========================================
    // Toolbar Editor Controls
    // ========================================
    const execCmd = (command, value = null) => {
        contentEditor.focus();
        document.execCommand(command, false, value);
    };

    document.getElementById('undoBtn')?.addEventListener('click', (e) => { e.preventDefault(); execCmd('undo'); });
    document.getElementById('redoBtn')?.addEventListener('click', (e) => { e.preventDefault(); execCmd('redo'); });
    document.getElementById('boldBtn')?.addEventListener('click', (e) => { e.preventDefault(); execCmd('bold'); });
    document.getElementById('italicBtn')?.addEventListener('click', (e) => { e.preventDefault(); execCmd('italic'); });
    document.getElementById('underlineBtn')?.addEventListener('click', (e) => { e.preventDefault(); execCmd('underline'); });
    document.getElementById('strikeBtn')?.addEventListener('click', (e) => { e.preventDefault(); execCmd('strikeThrough'); });

    // For listBtn (bulleted list)
    document.getElementById('listBtn')?.addEventListener('click', (e) => {
        e.preventDefault();

        contentEditor.focus();
        const selection = window.getSelection();
        if (selection.rangeCount > 0) {
            const range = selection.getRangeAt(0);
            const selectedText = range.toString();

            if (selectedText) {
                // If there's selected text, split by newlines and add bullet before each line
                const bulletedText = selectedText.split('\n').map(line => '● ' + line).join('\n');
                document.execCommand('insertText', false, bulletedText);
            } else {
                // Just insert a bullet character
                document.execCommand('insertText', false, '● ');
            }
        }
    });

    // For imageBtn (device photo upload)
    const imageFileInput = document.getElementById('imageFileInput');
    document.getElementById('imageBtn')?.addEventListener('click', (e) => {
        e.preventDefault();
        imageFileInput?.click();
    });

    imageFileInput?.addEventListener('change', function (e) {
        const file = e.target.files[0];
        if (file) {
            const reader = new FileReader();
            reader.onload = function (event) {
                execCmd('insertImage', event.target.result);
            };
            reader.readAsDataURL(file);
        }
    });

    // ========================================
    // Save and Publish Logic (Eski kodunun aynısı)
    // ========================================
    function saveBlog(isPublished) {
        const title = titleInput.value.trim();
        const content = contentEditor.innerHTML.trim();
        const categoryId = categorySelect ? parseInt(categorySelect.value) : 0;
        const editingPostId = document.getElementById('editingPostId')?.value;

        if (!title) { alert('Please enter a title.'); titleInput.focus(); return; }
        if (!content || content === '<br>') { alert('Please write some content.'); contentEditor.focus(); return; }
        if (!categoryId) { alert('Please choose a category.'); categorySelect?.focus(); return; }

        const payload = { title, content, categoryId, isPublished };
        if (editingPostId) payload.id = parseInt(editingPostId);

        fetch('/Blog/SaveBlog', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        })
            .then(res => res.json())
            .then(data => {
                if (data.success) {
                    window.location.href = isPublished ? '/Profile/Profile' : '/Blog/Archive?tab=drafts';
                } else {
                    alert('Error: ' + (data.error || 'Something went wrong.'));
                }
            });
    }

    document.getElementById('saveBtn')?.addEventListener('click', () => saveBlog(false));
    document.getElementById('publishBtn')?.addEventListener('click', () => saveBlog(true));
});
