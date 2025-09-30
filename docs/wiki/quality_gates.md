# Quality Gates

| Gate | Threshold | Enforcement |
|------|-----------|-------------|
| Lint | 0 errors | CI step |
| Unit Coverage | >= 70% critical paths | Coverage report |
| Performance | p95 ingest < 2s | Load test nightly |
| Security | No high severity vulns | Dependency scan |
| Docs Updated | Affected pages referenced | PR template checkbox |

---
Navigation: [Home](home.md) | Previous: [Contract Testing](contract_testing.md) | Next: [Governance & Change Control](governance_change_control.md)
