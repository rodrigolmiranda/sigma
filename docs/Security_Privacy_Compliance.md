# Security, Privacy & Compliance (v2025-09-27)

## Frameworks & Standards
- **ISO/IEC 27001/27002**: ISMS scope, risk assessment, control library; keys: access control, cryptography, operations, incident mgmt, supplier mgmt.
- **SOC 2 (Security/Availability/Confidentiality)**: TSC mapping; monitoring; change mgmt; backups; DR.
- **ISO 9001**: process quality; corrective/preventive actions; customer satisfaction tracking.
- **GDPR / LGPD / Australian APPs**: legal basis & consent; DSRs (access/export/delete); data mapping; retention per tenant; DPA & SCCs where needed.

## Technical Controls
- **Identity**: OIDC; RBAC; MFA for admin; SCIM (Phase 3).
- **Data**: TLS 1.2+; disk & blob encryption; envelope encryption for sensitive fields with Key Vault; field‑level hashing where useful.
- **Isolation**: tenant scoping enforced in queries; per‑tenant keys for sensitive blobs.
- **AppSec**: OWASP ASVS L2; SAST (CodeQL); dependency audit; secrets scanning; SSRF egress allow‑list; strict input validation & output encoding; CSRF as needed.
- **Logging**: structured logs; PII redaction; access‑controlled; immutability for Guardian evidence; lifecycle retention.
- **Privacy**: consent registry; export/delete APIs; audit trail of administrative actions.
- **Platform compliance**: adhere to Slack/Discord/Telegram/Teams terms; WhatsApp limited to permitted flows.

## Operations
- **Backups**: Postgres PITR; blob versioning; periodic restores.
- **Monitoring**: ingestion lag; queue depth; error budgets; security alerts; anomaly detection on login.
- **Incident Response**: severity matrix; 24h customer notice; forensics on evidence blobs; post‑mortems.

## Readiness
- Quarterly internal audits; vendor DDQ pack; pen tests; security whitepaper; compliance roadmap (ISO 27001 then SOC 2).
