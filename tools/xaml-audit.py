"""
Static XAML safety audit for the WinUI clients.

Catches resource/style failures that compile but crash at runtime, plus unsupported panel properties:

  1. A Style applied to an element whose type is not the Style's TargetType. WinUI throws
     0xc000027b in Microsoft.ui.xaml.dll the moment the view is realised - it took down the whole
     Cashier app once, on three separate management screens.
  2. A StaticResource key that no merged dictionary defines ("Cannot find a Resource with the
     Name/Key ..."), same symptom.
  3. IsEnabled on a layout element rather than a Control. WinUI's XAML compiler can exit with
     an opaque MSB3073 error here; disable the individual input controls instead.
  4. "--" inside an XML comment. XML forbids it outright, and XamlCompiler.exe reacts by exiting
     with code 1 and printing nothing at all - no file, no line, no message - so a divider rule
     drawn with hyphens costs an afternoon of bisecting. Draw dividers with "=" instead.
  6. An App.xaml resource entry naming a converter class that does not exist. The key resolves,
     so rule 2 sees nothing wrong, and the XAML compiler dies with the same silent exit 1 as
     rule 4 - this one cost a Call Center build.
  5. A collection converter applied to a bare ViewModel collection property. Filling an
     ObservableCollection raises CollectionChanged, not PropertyChanged on the property holding it,
     so the binding never re-evaluates: every empty state in this app was sitting on top of the
     rows it said were not there. Bind the collection's own Count instead.

Run from the repo root:  python tools/xaml-audit.py
Exits non-zero when anything is found, so it can gate a build.
"""
import glob
import io
import os
import re
import sys

PROJECTS = [
    "src/Falah.Client.CashierWinUI",
    "src/Falah.Client.CallCenterWinUI",
]


def load_defined(project):
    """Style key -> TargetType, plus every x:Key defined anywhere in the project's resources."""
    targets, keys = {}, set()
    for f in glob.glob(f"{project}/Styles/*.xaml") + [f"{project}/App.xaml"]:
        if not os.path.exists(f):
            continue
        s = io.open(f, encoding="utf-8").read()
        for m in re.finditer(r'<Style x:Key="([^"]+)" TargetType="([^"]+)"', s):
            targets[m.group(1)] = m.group(2)
        for m in re.finditer(r'x:Key="([^"]+)"', s):
            keys.add(m.group(1))
    return targets, keys


def audit_converters(project):
    """Every <conv:X x:Key="X"/> in App.xaml has to be a class that exists."""
    app = f"{project}/App.xaml"
    converters = f"{project}/Converters/Converters.cs"
    if not (os.path.exists(app) and os.path.exists(converters)):
        return []

    declared = set(re.findall(r"public sealed class (\w+)\s*:\s*IValueConverter", io.open(converters, encoding="utf-8").read()))
    s = io.open(app, encoding="utf-8").read()
    problems = []
    for m in re.finditer(r"<conv:(\w+)\s", s):
        if m.group(1) not in declared:
            line = s[: m.start()].count("\n") + 1
            problems.append(f"{os.path.relpath(app)}:{line}  '{m.group(1)}' is registered but no such converter class exists")
    return problems


def audit(project):
    targets, defined = load_defined(project)
    problems = []
    files = glob.glob(f"{project}/Views/*.xaml") + glob.glob(f"{project}/Controls/*.xaml")
    for f in files:
        s = io.open(f, encoding="utf-8").read()
        name = os.path.relpath(f)

        for m in re.finditer(r'\{Binding ([A-Za-z_][A-Za-z0-9_]*), Converter=\{StaticResource '
                             r'(Empty|NonEmpty)CollectionToVisibleConverter\}\}', s):
            line = s[: m.start()].count("\n") + 1
            problems.append(
                f"{name}:{line}  '{m.group(1)}' never re-evaluates when rows arrive; "
                f"bind {m.group(1)}.Count with ZeroCountToVisibleConverter/CountToVisibleConverter"
            )

        for m in re.finditer(r'<!--(.*?)-->', s, re.S):
            if "--" in m.group(1):
                line = s[: m.start()].count("\n") + 1
                problems.append(f"{name}:{line}  '--' inside an XML comment; XamlCompiler exits 1 in silence")

        for m in re.finditer(r'<(Grid|StackPanel|Border|TextBlock|Canvas)\b[^>]*?\bIsEnabled\s*=', s, re.S):
            line = s[: m.start()].count("\n") + 1
            problems.append(f"{name}:{line}  <{m.group(1)}> has no IsEnabled property; use the input Control")

        for m in re.finditer(r'<([A-Za-z][\w.]*)\b[^>]*?Style="\{StaticResource ([A-Za-z0-9_]+)\}"', s, re.S):
            elem, key = m.group(1), m.group(2)
            line = s[: m.start()].count("\n") + 1
            if key not in defined:
                problems.append(f"{name}:{line}  undefined style key '{key}'")
            elif key in targets and targets[key] != elem:
                problems.append(
                    f"{name}:{line}  <{elem}> uses '{key}' whose TargetType is {targets[key]} -> runtime crash"
                )

        for m in re.finditer(r'\{StaticResource ([A-Za-z0-9_]+)\}', s):
            key = m.group(1)
            if key not in defined:
                line = s[: m.start()].count("\n") + 1
                problems.append(f"{name}:{line}  undefined resource key '{key}'")

    return problems


def main():
    found = []
    for project in PROJECTS:
        if os.path.isdir(project):
            found += audit(project) + audit_converters(project)

    # Same key can be reported by both passes; report each site once.
    found = sorted(set(found))
    if found:
        print("\n".join(found))
        print(f"\n{len(found)} XAML issue(s)")
        return 1
    print("XAML audit clean")
    return 0


if __name__ == "__main__":
    sys.exit(main())
