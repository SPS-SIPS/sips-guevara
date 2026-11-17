"""Fail CI on NuGet audit errors or vulnerable direct/transitive packages."""
import json
import sys
from pathlib import Path

audit = json.loads(Path(sys.argv[1]).read_text())
if audit.get("problems") or audit.get("errors"):
    sys.exit("NuGet audit did not complete successfully")
findings = []
for project in audit.get("projects", []):
    for framework in project.get("frameworks", []):
        for kind in ("topLevelPackages", "transitivePackages"):
            for package in framework.get(kind, []):
                if package.get("vulnerabilities"):
                    findings.append(f"{package['id']} {package.get('resolvedVersion', '')}")
if findings:
    sys.exit("Vulnerable NuGet packages: " + ", ".join(findings))
print("NuGet audit: no known vulnerable packages")
