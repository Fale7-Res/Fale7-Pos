# -*- coding: utf-8 -*-
"""
Generate technically-grounded, substantive reviews for:
1. OwnerDashboardQueryService.cs.md
2. DailyStatementQueryService.cs.md
And reconcile ALL 77 findings in HISTORICAL_AUDIT_RECONCILIATION.json
"""

import os
import sys
import json
import hashlib
import re

REPO_ROOT = r"c:\Users\Mohamed\Desktop\Falah-Restaurant-CSharp"
WORKSPACE = os.path.join(REPO_ROOT, "GitHub Copilot Work")
RECON_JSON_PATH = os.path.join(WORKSPACE, "Checkpoints", "HISTORICAL_AUDIT_RECONCILIATION.json")
MASTER_FINDINGS_PATH = os.path.join(WORKSPACE, "MASTER_FINDINGS.json")

def get_file_sha256(path):
    full = os.path.join(REPO_ROOT, path)
    h = hashlib.sha256()
    with open(full, 'rb') as f:
        while chunk := f.read(65536):
            h.update(chunk)
    return h.hexdigest().upper()

print("Generator script template loaded.")
