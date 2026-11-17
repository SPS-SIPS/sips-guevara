"""Fail on unsupported OS images, high/critical findings, or fixable medium findings."""
import json
import sys
from pathlib import Path

audit = json.loads(Path(sys.argv[1]).read_text())
os_info = audit.get("Metadata", {}).get("OS")
if not os_info:
    sys.exit("Container audit has no operating-system metadata")
if os_info.get("EOSL"):
    sys.exit("Container operating system is outside normal support")
findings = [v for result in audit.get("Results", []) for v in result.get("Vulnerabilities", [])]
blocked = [v for v in findings if v["Severity"] in ("CRITICAL", "HIGH") or (v["Severity"] == "MEDIUM" and v.get("FixedVersion"))]
print(f"Container audit: {len(blocked)} blocking findings; {len(findings)} total advisory entries")
if blocked:
    sys.exit("Container updates required: " + ", ".join(sorted({v["VulnerabilityID"] for v in blocked})))
