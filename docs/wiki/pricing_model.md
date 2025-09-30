# Pricing Model

## Overview

Hybrid base subscription + usage + add-on modules.

## Plan Structure

| Tier | Base Monthly | Included Messages | Included Polls | Add-ons Available | Notes |
|------|--------------|-------------------|----------------|-------------------|-------|
| Core | $X | 1M normalized | 50 active | Guardian, RAG, Broadcasts | Entry |
| Growth | $Y | 5M | 200 | All + Priority Support | Mid-market |
| Enterprise | Custom | 20M | 500 | All + SSO, Audit Export | Contracts |

## Add-On Metrics

| Add-on | Metric Basis | Pricing Basis |
|--------|--------------|---------------|
| Guardian Pro | Flags processed | Per 1k flags |
| Ask‑DB | RAG queries | Per 1k queries (banded) |
| Broadcasts | Delivered messages | Per 1k delivered |

## Gating Flags

`guardian.pro.enabled`, `askdb.enabled`, `broadcasts.enabled`, `youtube.live.enabled`

## Expansion Drivers

* Increased message volume → higher analytics baseline.
* Add-on attach via in-app upsell prompts (contextual usage thresholds).

---
Navigation: [Home](home.md) | Previous: [Feature Acceptance Matrix](feature_acceptance_matrix.md) | Next: [KPI ↔ Metric Mapping](kpi_metric_mapping.md)
