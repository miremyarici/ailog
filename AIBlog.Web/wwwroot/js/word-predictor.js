/**
 * Word Predictor - Frontend JavaScript for AI Word Prediction
 */

class WordPredictor {
    constructor(options = {}) {
        this.apiBaseUrl = options.apiBaseUrl || '/api/ai';
        this.debounceMs = options.debounceMs || 300;
        this.minTextLength = options.minTextLength || 3;
        this.maxPredictions = options.maxPredictions || 5;
        this.debounceTimer = null;
        this.isEnabled = true;
        this.onPredictionsReceived = options.onPredictionsReceived || null;
        this.onError = options.onError || null;
    }

    async checkHealth() {
        try {
            const response = await fetch(`${this.apiBaseUrl}/health`);
            const data = await response.json();
            return data.status === 'healthy';
        } catch (error) {
            console.error('AI service health check failed:', error);
            return false;
        }
    }

    async getPredictions(text) {
        if (!text || text.trim().length < this.minTextLength) {
            return [];
        }

        try {
            const response = await fetch(`${this.apiBaseUrl}/predict`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ text: text, count: this.maxPredictions })
            });

            const data = await response.json();

            if (data.success) {
                return data.predictions || [];
            } else {
                if (this.onError) this.onError(data.error);
                return [];
            }
        } catch (error) {
            console.error('Prediction error:', error);
            if (this.onError) this.onError(error.message);
            return [];
        }
    }

    async getCompletion(text, maxLength = 10) {
        if (!text || text.trim().length < this.minTextLength) {
            return '';
        }

        try {
            const response = await fetch(`${this.apiBaseUrl}/complete`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ text: text, maxLength: maxLength })
            });

            const data = await response.json();

            if (data.success) {
                return data.completion || '';
            } else {
                if (this.onError) this.onError(data.error);
                return '';
            }
        } catch (error) {
            console.error('Completion error:', error);
            if (this.onError) this.onError(error.message);
            return '';
        }
    }

    attachToElement(element, suggestionContainer) {
        if (!element) return;

        element.addEventListener('input', (e) => {
            if (!this.isEnabled) return;

            if (this.debounceTimer) {
                clearTimeout(this.debounceTimer);
            }

            this.debounceTimer = setTimeout(async () => {
                const text = e.target.value;
                const predictions = await this.getPredictions(text);

                if (this.onPredictionsReceived) {
                    this.onPredictionsReceived(predictions);
                }

                if (suggestionContainer) {
                    this.renderSuggestions(predictions, suggestionContainer, element);
                }
            }, this.debounceMs);
        });

        element.addEventListener('keydown', (e) => {
            if (suggestionContainer && suggestionContainer.children.length > 0) {
                const items = suggestionContainer.querySelectorAll('.prediction-item');
                const activeItem = suggestionContainer.querySelector('.prediction-item.active');

                if (e.key === 'ArrowDown') {
                    e.preventDefault();
                    if (!activeItem) {
                        items[0]?.classList.add('active');
                    } else {
                        const nextIndex = Array.from(items).indexOf(activeItem) + 1;
                        activeItem.classList.remove('active');
                        items[nextIndex % items.length]?.classList.add('active');
                    }
                } else if (e.key === 'ArrowUp') {
                    e.preventDefault();
                    if (activeItem) {
                        const prevIndex = Array.from(items).indexOf(activeItem) - 1;
                        activeItem.classList.remove('active');
                        items[prevIndex >= 0 ? prevIndex : items.length - 1]?.classList.add('active');
                    }
                } else if (e.key === 'Tab' || e.key === 'Enter') {
                    if (activeItem && suggestionContainer.style.display !== 'none') {
                        e.preventDefault();
                        this.insertPrediction(element, activeItem.textContent.replace('→ ', ''));
                        this.clearSuggestions(suggestionContainer);
                    }
                } else if (e.key === 'Escape') {
                    this.clearSuggestions(suggestionContainer);
                }
            }
        });
    }

    renderSuggestions(predictions, container, targetElement) {
        container.innerHTML = '';

        if (!predictions || predictions.length === 0) {
            container.style.display = 'none';
            return;
        }

        predictions.forEach((prediction, index) => {
            const item = document.createElement('div');
            item.className = 'prediction-item';
            item.textContent = '→ ' + prediction;
            item.addEventListener('click', () => {
                this.insertPrediction(targetElement, prediction);
                this.clearSuggestions(container);
            });
            item.addEventListener('mouseenter', () => {
                container.querySelectorAll('.prediction-item').forEach(i => i.classList.remove('active'));
                item.classList.add('active');
            });
            container.appendChild(item);
        });

        container.style.display = 'block';
    }

    insertPrediction(element, prediction) {
        const currentText = element.value;
        const needsSpace = currentText.length > 0 && !currentText.endsWith(' ');
        element.value = currentText + (needsSpace ? ' ' : '') + prediction + ' ';
        element.focus();
        element.dispatchEvent(new Event('input', { bubbles: true }));
    }

    clearSuggestions(container) {
        if (container) {
            container.innerHTML = '';
            container.style.display = 'none';
        }
    }

    setEnabled(enabled) {
        this.isEnabled = enabled;
    }
}
