# Security & Privacy Controls

| Control Area | Implementation |
|--------------|----------------|
| AuthN | OIDC provider integration |
| AuthZ | RBAC claims & policy enforcement |
| Encryption | Envelope encryption (key per tenant) |
| PII Minimization | Hashing (salted) for user identifiers |
| Rate Limiting | Per-tenant API & mutation cost |
| Audit Logging | Structured event stream |
| Compliance | GDPR/LGPD alignment; SOC2 roadmap |

Data classification matrix forthcoming (ties to [Retention Policies](retention_policies.md)).

---
Navigation: [Home](home.md) | Previous: [API Schema Evolution](api_schema_evolution.md) | Next: [Incident Response](incident_response.md)
