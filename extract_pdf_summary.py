from pathlib import Path
import PyPDF2
import sys
p = Path('GUIA_DEL_PROYECTO_LLENA (3) (1).pdf')
r = PyPDF2.PdfReader(p)
for i, page in enumerate(r.pages, start=1):
    text = page.extract_text() or ''
    summary = text.replace('\n', ' ')[:400]
    print(f'---PAGE {i}---')
    print(summary)
