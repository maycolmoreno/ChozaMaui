import os
import requests
from pathlib import Path

# === CONFIGURACION ===
PROJECT_ROOT  = r"C:\Users\mmorenos\Desktop\version final 3\Proyecto"
OLLAMA_URL    = "http://localhost:11434/api/generate"
MODEL         = "qwen2.5-coder:14b"
EXTENSIONS    = {".java", ".cs", ".xml", ".csproj", ".xaml", ".properties", ".yaml", ".yml"}
IGNORE_DIRS   = {".git", "bin", "obj", "target", ".gradle", "node_modules", "__pycache__", ".tmp_apiPOS"}
MAX_FILE_SIZE = 12_000

def collect_files():
    files = []
    for dirpath, dirnames, filenames in os.walk(PROJECT_ROOT):
        dirnames[:] = [d for d in dirnames if d not in IGNORE_DIRS]
        for fname in filenames:
            fpath = Path(dirpath) / fname
            if fpath.suffix in EXTENSIONS and fpath.stat().st_size <= MAX_FILE_SIZE:
                files.append(fpath)
    return sorted(files)

def analyze_file(filepath):
    content = filepath.read_text(encoding="utf-8", errors="ignore")
    prompt = (
        "Eres un experto en Java Spring Boot y .NET MAUI.\n"
        "Analiza este archivo y responde en espanol:\n"
        "1. Que hace este archivo?\n"
        "2. Problemas o bugs encontrados?\n"
        "3. Mejoras o buenas practicas faltantes?\n\n"
        "Archivo: " + filepath.name + "\n\n"
        + content
    )
    try:
        r = requests.post(OLLAMA_URL, json={
            "model": MODEL,
            "prompt": prompt,
            "stream": False
        }, timeout=180)
        return r.json().get("response", "Sin respuesta")
    except Exception as e:
        return "ERROR: " + str(e)

def generate_summary(analyses):
    combined = "\n\n".join(["### " + name + "\n" + text for name, text in analyses])
    prompt = (
        "Basandote en el analisis de estos archivos de un proyecto Java Spring Boot + .NET MAUI,\n"
        "genera un resumen ejecutivo en espanol con:\n"
        "1. Proposito general del proyecto\n"
        "2. Problemas criticos encontrados\n"
        "3. Top 5 mejoras recomendadas\n"
        "4. Calidad general del codigo (1-10)\n\n"
        + combined[:8000]
    )
    try:
        r = requests.post(OLLAMA_URL, json={
            "model": MODEL,
            "prompt": prompt,
            "stream": False
        }, timeout=180)
        return r.json().get("response", "Sin respuesta")
    except Exception as e:
        return "ERROR: " + str(e)

def main():
    print("Escaneando: " + PROJECT_ROOT + "\n")
    files = collect_files()

    if not files:
        print("No se encontraron archivos.")
        return

    print(str(len(files)) + " archivos encontrados\n" + "-" * 50)

    analyses = []
    report_path = r"C:\Users\mmorenos\Desktop\reporte_proyecto.md"

    with open(report_path, "w", encoding="utf-8") as report:
        report.write("# Reporte de Analisis del Proyecto\n")
        report.write("**Modelo:** " + MODEL + "\n\n---\n\n")

        for i, filepath in enumerate(files, 1):
            print("[" + str(i) + "/" + str(len(files)) + "] " + filepath.name + "...", end=" ", flush=True)
            analysis = analyze_file(filepath)
            analyses.append((filepath.name, analysis))
            report.write("## " + filepath.name + "\n")
            report.write("**Ruta:** " + str(filepath) + "\n\n")
            report.write(analysis + "\n\n---\n\n")
            print("OK")

        print("\nGenerando resumen ejecutivo...")
        summary = generate_summary(analyses)
        report.write("# Resumen Ejecutivo\n\n" + summary + "\n")

    print("\nReporte guardado en: " + report_path)

if __name__ == "__main__":
    main()