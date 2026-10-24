import sys
sys.stdout.reconfigure(encoding='utf-8')
from pathlib import Path

def view_span(path_str, start, end):
    with open(path_str, 'r', encoding='utf-8', errors='ignore') as f:
        lines = f.readlines()
    for idx in range(max(0, start - 1), min(len(lines), end)):
        print(f"{idx+1:4d}: {lines[idx]}", end='')

if __name__ == '__main__':
    if len(sys.argv) >= 4:
        view_span(sys.argv[1], int(sys.argv[2]), int(sys.argv[3]))
    elif len(sys.argv) == 2:
        view_span(sys.argv[1], 1, 100)
