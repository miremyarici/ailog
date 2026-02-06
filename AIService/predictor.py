"""
GPT-2 based word prediction module
"""
from transformers import GPT2LMHeadModel, GPT2Tokenizer
import torch


class WordPredictor:
    def __init__(self, model_name: str = "gpt2-large"):
        """
        Initialize the GPT-2 model for word prediction.
        
        Args:
            model_name: The GPT-2 model variant to use (gpt2, gpt2-medium, gpt2-large)
        """
        print(f"Loading {model_name} model... This may take a few minutes on first run.")
        self.tokenizer = GPT2Tokenizer.from_pretrained(model_name)
        self.model = GPT2LMHeadModel.from_pretrained(model_name)
        self.model.eval()
        
        # Use GPU if available for faster inference
        self.device = "cuda" if torch.cuda.is_available() else "cpu"
        self.model.to(self.device)
        print(f"Model loaded successfully on {self.device}!")
        
        # Set pad token
        self.tokenizer.pad_token = self.tokenizer.eos_token
    
    def predict_next_words(self, text: str, num_predictions: int = 5) -> list[str]:
        """
        Predict the next possible words given the input text.
        Uses generation to produce complete words, not token fragments.
        
        Args:
            text: The input text to predict from
            num_predictions: Number of word predictions to return
            
        Returns:
            List of predicted words
        """
        if not text or not text.strip():
            return []
        
        # Add space at end for proper word boundary
        processed_text = text if text.endswith(' ') else text + ' '
        
        # Encode the input text
        inputs = self.tokenizer.encode(processed_text, return_tensors="pt").to(self.device)
        input_length = inputs.shape[1]
        
        predicted_words = []
        
        # Generate multiple sequences and extract first words
        with torch.no_grad():
            outputs = self.model.generate(
                inputs,
                max_new_tokens=5,  # Generate a few tokens
                num_return_sequences=num_predictions * 3,  # Generate more to filter
                do_sample=True,
                temperature=0.8,
                top_k=50,
                top_p=0.95,
                pad_token_id=self.tokenizer.eos_token_id,
                num_beams=1,  # Use sampling, not beam search for diversity
            )
        
        for output in outputs:
            # Get only the new tokens
            new_tokens = output[input_length:]
            generated_text = self.tokenizer.decode(new_tokens, skip_special_tokens=True).strip()
            
            if generated_text:
                # Extract the first word
                first_word = generated_text.split()[0] if generated_text.split() else ""
                
                # Clean: remove punctuation from end
                first_word = first_word.rstrip('.,!?;:"\'-)')
                first_word = first_word.lstrip('("\'')
                
                # Validate: must be a proper word
                if (first_word 
                    and len(first_word) >= 2 
                    and first_word[0].isalpha()
                    and first_word.isascii()
                    and first_word.lower() not in [w.lower() for w in predicted_words]):
                    predicted_words.append(first_word)
                    
                    if len(predicted_words) >= num_predictions:
                        break
        
        return predicted_words
    
    def predict_completion(self, text: str, max_length: int = 10) -> str:
        """
        Generate a completion for the given text.
        
        Args:
            text: The input text to complete
            max_length: Maximum number of new tokens to generate
            
        Returns:
            The completed text
        """
        if not text or not text.strip():
            return ""
        
        inputs = self.tokenizer.encode(text, return_tensors="pt").to(self.device)
        
        with torch.no_grad():
            outputs = self.model.generate(
                inputs,
                max_new_tokens=max_length,
                num_return_sequences=1,
                do_sample=True,
                temperature=0.7,
                top_p=0.9,
                pad_token_id=self.tokenizer.eos_token_id
            )
        
        completed_text = self.tokenizer.decode(outputs[0], skip_special_tokens=True)
        # Return only the new part
        return completed_text[len(text):].strip()


# Singleton instance
_predictor_instance = None


def get_predictor() -> WordPredictor:
    """Get or create the singleton WordPredictor instance."""
    global _predictor_instance
    if _predictor_instance is None:
        _predictor_instance = WordPredictor()
    return _predictor_instance
