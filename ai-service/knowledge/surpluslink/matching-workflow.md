<!--
title: SurplusLink AI Matching Workflow
source: SurplusLink Core Documentation
source_url: https://surpluslink.lk/docs/matching-workflow
topic: Matching Workflow
country: Sri Lanka
language: English
last_reviewed: 2026-10-01
source_type: SURPLUSLINK_INTERNAL
-->

# AI Matching Workflow

## Overview
SurplusLink uses a multi-agent AI orchestration workflow to match buyer requirements with active seller listings across Sri Lanka.

## Workflow Phases
1. **Planner**: Parses buyer requirement details, standardizes quantities, and determines candidate filters.
2. **Item Relevance**: Strict item identity verification (e.g. Paint ≠ Bee Honey, Generator ≠ Air Compressor). Ensures items are genuinely comparable.
3. **Matching & Scoring**: Evaluates eligible active seller listings based on:
   - Available quantity
   - Material unit price
   - Condition score (New = 1.0, Excellent = 0.9, Good = 0.8, Fair = 0.6, Poor = 0.4)
   - Distance & transport cost (LKR per km)
   - Preference compatibility (Hard vs Soft preference filters)
4. **Logistics Engine**: Calculates optimal single-seller or multi-seller combinations to fulfill 100% of required quantity while minimizing total delivered cost (material + transport).
5. **Validation & Persistence**: Validates final match allocation and persists recommendation reason and score breakdown to the database.
6. **Manager Approval**: Batched match recommendations await human manager approval before reserving inventory.

## Score Breakdown Rules
- The match score is calculated deterministically by ASP.NET / Python matching pipeline.
- The AI Assistant explains persisted match scores and facts to users; it does NOT recalculate or invent different scores.
