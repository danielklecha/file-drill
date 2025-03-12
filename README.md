# file-drill

File-drill tool can:

- read file content using local code and AI
- classify content using AI
- extract fields using AI

## Configuration

```powershell
file-drill config set .\config.json
file-drill read classify extract c:\invoice.pdf
```

Sample configuration file:

```json
{
  "FallbackAIService": "llama",
  "AIServices": {
    "llama": {
      "Type": "Ollama",
      "Url": "http://localhost:11434",
      "ModelName": "llama3.1:latest"
      }
  }
}
```

# Supported file extensions

- pdf
- txt
- md
- docx
- dotx
- docm
- dotm
- png
- jpeg
- rtf
