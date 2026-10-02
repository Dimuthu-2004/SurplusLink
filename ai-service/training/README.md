# SurplusLink AI Assistant Fine-Tuning & Evaluation Pipeline

## Overview

This directory contains the production-quality fine-tuning and evaluation pipeline for the SurplusLink AI Assistant (`surpluslink-semantic-v1`).

> [!IMPORTANT]
> **Semantic Brain Boundaries**: Fine-tuning is strictly scoped to language understanding, intent classification, slot extraction, follow-up understanding, ambiguity detection, topic switch detection, multilingual Sri Lankan phrasing, and capability selection.
> 
> The fine-tuned model does **NOT** memorize database records, live marketplace prices, active listings, inventory, or construction formulas. Those remain controlled by RAG, live ASP.NET Core tools, catalog schemas, deterministic calculators, validators, and the conversation state machine.

---

## Directory Structure

```
ai-service/training/
├── configs/
│   ├── dataset_config.json        # Dataset balance, language, and size config
│   └── training_config.json       # Hyperparameters, model version, and eval thresholds
├── datasets/
│   ├── train.jsonl               # 80% Training set (2,003 items)
│   ├── val.jsonl                 # 10% Validation set (250 items)
│   ├── test.jsonl                # 10% Held-Out Test set (251 items)
│   ├── challenge.jsonl           # 320-item Challenge set (Adversarial, noisy, Sri Lankan)
│   ├── groq_fine_tune_train.jsonl # Provider-ready train JSONL
│   └── groq_fine_tune_val.jsonl   # Provider-ready val JSONL
├── scripts/
│   ├── generate_dataset.py       # Dataset generator with unique message deduplication
│   ├── validate_dataset.py       # Schema validator and data leakage checker
│   ├── run_baseline_eval.py      # Baseline evaluation engine
│   ├── train_model.py            # Fine-tuning job manager & adapter builder
│   └── run_post_eval.py          # Post-train eval engine & confusion matrix builder
├── evaluations/
│   ├── baseline_report.json      # Baseline model metrics
│   ├── fine_tune_job_manifest.json# Model versioning and hyper-parameter manifest
│   ├── post_train_report.json    # Post-training evaluation report
│   └── confusion_matrix.json     # Intent confusion matrix
└── reports/
    ├── dataset_quality_report.json# Dataset quality and leakage inspection report
    └── failure_analysis.json      # Categorized failure analysis
```

---

## Execution Pipeline

To generate datasets, validate schema compliance, run baseline evaluation, build fine-tuning artifacts, and evaluate post-training models:

```powershell
# 1. Generate Datasets
python training/scripts/generate_dataset.py

# 2. Validate Dataset Quality & Leakage
python training/scripts/validate_dataset.py

# 3. Run Baseline Evaluation
python training/scripts/run_baseline_eval.py

# 4. Prepare Fine-Tuning Job Artifacts
python training/scripts/train_model.py

# 5. Run Post-Training Evaluation
python training/scripts/run_post_eval.py
```

---

## Deployment Safety & Fallback Strategy

The fine-tuned model (`surpluslink-semantic-v1`) operates inside SurplusLink's deterministic safety harness:
1. Output passes through Pydantic `SemanticRoutingResult` validation.
2. `QuantityNormalizer` parses physical quantities and package counts.
3. `ConversationStateMachine` handles turn context and topic switches.
4. `ValidationGate` enforces area sanity, location cleaning, and tool provenance.
5. If model output is malformed or confidence $< 0.68$, the engine falls back to safe clarification.
