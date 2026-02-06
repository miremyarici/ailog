"""
Flask API for GPT-2 Word Prediction Service
This service provides word prediction endpoints for the ASP.NET MVC blog application.
"""
from flask import Flask, request, jsonify
from flask_cors import CORS
from predictor import get_predictor

app = Flask(__name__)

# Enable CORS for ASP.NET MVC application
# In production, replace '*' with your ASP.NET app URL
CORS(app, origins=["http://localhost:5000", "https://localhost:5001", "http://localhost:7000", "*"])


@app.route('/api/health', methods=['GET'])
def health_check():
    """Health check endpoint to verify the service is running."""
    return jsonify({
        "status": "healthy",
        "service": "AI Word Prediction Service",
        "model": "GPT-2"
    })


@app.route('/api/predict', methods=['POST'])
def predict_words():
    """
    Predict the next words based on input text.
    
    Request body:
        {
            "text": "The quick brown fox",
            "count": 5  // optional, default is 5
        }
    
    Response:
        {
            "success": true,
            "predictions": ["jumps", "ran", "was", "is", "walked"],
            "input_text": "The quick brown fox"
        }
    """
    try:
        data = request.get_json()
        
        if not data or 'text' not in data:
            return jsonify({
                "success": False,
                "error": "Missing 'text' field in request body"
            }), 400
        
        text = data['text']
        count = data.get('count', 5)
        
        predictor = get_predictor()
        predictions = predictor.predict_next_words(text, num_predictions=count)
        
        return jsonify({
            "success": True,
            "predictions": predictions,
            "input_text": text
        })
    
    except Exception as e:
        return jsonify({
            "success": False,
            "error": str(e)
        }), 500


@app.route('/api/complete', methods=['POST'])
def complete_text():
    """
    Generate a text completion based on input.
    
    Request body:
        {
            "text": "The quick brown fox",
            "max_length": 10  // optional, default is 10
        }
    
    Response:
        {
            "success": true,
            "completion": "jumps over the lazy dog",
            "input_text": "The quick brown fox"
        }
    """
    try:
        data = request.get_json()
        
        if not data or 'text' not in data:
            return jsonify({
                "success": False,
                "error": "Missing 'text' field in request body"
            }), 400
        
        text = data['text']
        max_length = data.get('max_length', 10)
        
        predictor = get_predictor()
        completion = predictor.predict_completion(text, max_length=max_length)
        
        return jsonify({
            "success": True,
            "completion": completion,
            "input_text": text
        })
    
    except Exception as e:
        return jsonify({
            "success": False,
            "error": str(e)
        }), 500


if __name__ == '__main__':
    # Pre-load the model on startup
    print("Initializing GPT-2 model...")
    get_predictor()
    print("Model ready! Starting Flask server...")
    
    # Run on port 5001 to avoid conflict with ASP.NET (usually 5000/5001)
    app.run(host='0.0.0.0', port=5002, debug=True)
