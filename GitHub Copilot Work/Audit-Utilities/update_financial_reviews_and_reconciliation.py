# -*- coding: utf-8 -*-
import os
import sys
import json
import hashlib
import re

REPO_ROOT = r"c:\Users\Mohamed\Desktop\Falah-Restaurant-CSharp"
WORKSPACE = os.path.join(REPO_ROOT, "GitHub Copilot Work")

def get_file_sha256(path):
    full = os.path.join(REPO_ROOT, path)
    h = hashlib.sha256()
    with open(full, 'rb') as f:
        while chunk := f.read(65536):
            h.update(chunk)
    return h.hexdigest().upper()

print("Script initialized successfully.")
