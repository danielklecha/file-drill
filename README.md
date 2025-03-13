# file-drill

[![NuGet](https://img.shields.io/nuget/v/file-drill.svg)](https://www.nuget.org/packages/file-drill)
[![NuGet downloads](https://img.shields.io/nuget/dt/file-drill.svg)](https://www.nuget.org/packages/file-drill)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](https://github.com/danielklecha/SharpIppNext/blob/master/LICENSE.txt)

File-drill tool can:

- read file content using local code and AI
- classify content using AI
- extract fields using AI

## Installation

```powershell
dotnet tool install --global file-drill
```

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

| Extensions | Library |
|---|---|
| txt, md | built-in |
| pdf | PdfPig |
| docx, dotx, docm, dotm | DocumentFormat.OpenXml |
| png, jpeg | OCR using AI service |
| rtf | RtfPipe |
