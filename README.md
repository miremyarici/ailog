# 🧠 AILog - AI-Powered Blogging Platform

A modern, cloud-integrated blogging platform that features a real-time AI writing assistant. Built with ASP.NET Core MVC and integrated with a custom Python-based LLM microservice to help writers overcome writer's block through intelligent, context-aware autocomplete suggestions.

## ✨ Key Features

* **Real-Time AI Autocomplete:** Inspired by Notion AI, the editor uses a debounced, highly optimized Javascript integration to fetch next-word and sentence completion predictions as you pause your typing.
* **Cloud GPU Architecture:** Heavy AI inference is decoupled from the web server. The LLM runs on Google Colab's T4 GPUs and communicates with the .NET backend securely via Ngrok tunnels.
* **Clean Code & MVC Architecture:** Developed adhering to SOLID principles, DRY (Don't Repeat Yourself), and modular View/Partial View structures.
* **Rich Text Editing:** Format text, insert images, and manage lists seamlessly.
* **Robust State Management:** Custom Undo/Redo stack implemented for the editor.
* **Draft & Publishing System:** Save articles as drafts or publish them instantly to your public profile.

## 🛠️ Tech Stack

**Frontend**
* HTML5, CSS3, Vanilla JavaScript (No heavy frameworks, optimized for speed)
* Custom Debounce & DOM Range Management for seamless AI integration

**Backend**
* C# / ASP.NET Core MVC
* Entity Framework Core (SQL Server)
* IHttpClientFactory & Options Pattern for robust API communication

**AI Microservice (Cloud)**
* Python 3 & Flask
* PyTorch & Hugging Face Transformers
* LLM: `Qwen/Qwen2.5-1.5B` for lightning-fast inference
* Google Colab & Ngrok for cloud deployment

## 🏗️ System Architecture

AILog utilizes a distributed microservice architecture to solve hardware limitations:
1.  **Client (Browser):** The user types in the rich text editor. A debounced JS function captures the full context.
2.  **Web Server (ASP.NET Core):** Receives the context and routes it securely to the AI service, bypassing CORS and browser warnings.
3.  **Cloud Tunnel (Ngrok):** Securely forwards the request to the Google Colab instance.
4.  **AI Server (Colab/Flask):** Processes the text using a GPU-accelerated LLM and returns the most logical next-word predictions.

## 🚀 Getting Started

### 1. Starting the AI Microservice (Google Colab)
Since the LLM requires GPU acceleration, it is hosted on Google Colab.
1. Open a new Google Colab notebook.
2. Copy the contents of the `app.py` (or your Colab script) into a cell.
3. Add your Ngrok Authtoken: `!ngrok authtoken YOUR_TOKEN`
4. Run the cell. Note the generated Ngrok public URL (e.g., `https://xyz.ngrok-free.app`).

### 2. Setting up the Web Application

1. Clone the repository:
   ```bash
   git clone https://github.com/miremyarici/ailog.git
   ```

2. Navigate to the project directory:
   ```bash
   cd ailog/AIBlog.Web
   ```

3. Update `appsettings.json`:
   * Update the `DefaultConnection` string for your local SQL Server.
   * Add the Ngrok URL generated from Colab to `AIService:BaseUrl`.
   * Configure `SmtpSettings` for email verification (Never commit real passwords!).

4. Apply Entity Framework Migrations:
   ```bash
   dotnet ef database update
   ```

5. Run the application:
   ```bash
   dotnet run
   ```
