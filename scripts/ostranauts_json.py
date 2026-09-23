"""Read the commented JSON used by local Ostranauts definitions and metadata."""

import json
import re


def read_json(path):
    text = path.read_text(encoding="utf-8-sig")
    token = r'"(?:\\.|[^"\\])*"|//[^\n]*|/\*[\s\S]*?\*/'
    text = re.sub(token, lambda m: m[0] if m[0].startswith('"') else " ", text)
    text = re.sub(r'"(?:\\.|[^"\\])*"|,\s*(?=[}\]])',
                  lambda m: m[0] if m[0].startswith('"') else "", text)
    return json.loads(text, strict=False)
