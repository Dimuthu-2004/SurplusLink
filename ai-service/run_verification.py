#!/usr/bin/env python3
"""
Authoritative SurplusLink AI Assistant Verification Suite.
Executes pytest test suite (invariants, unit normalizer, state machine, capability registry,
validation gate, golden multi-turn conversations) and outputs full metrics.
"""

import sys
import pytest


def run_all_verifications():
    print("=" * 70)
    print("SURPLUSLINK AI ASSISTANT ARCHITECTURE VERIFICATION SUITE")
    print("=" * 70)

    import os
    tests_dir = os.path.join(os.path.dirname(__file__), "tests")
    args = [
        tests_dir,
        "-v",
        "--tb=short",
    ]

    print("\nRunning pytest test suite (Invariants, UnitNormalizer, StateMachine, CapabilityRegistry, Golden Conversations)...\n")
    exit_code = pytest.main(args)

    print("\n" + "=" * 70)
    if exit_code == 0:
        print("ALL ASSISTANT VERIFICATIONS PASSED SUCCESSFULLY!")
    else:
        print(f"VERIFICATION SUITE FAILED WITH EXIT CODE: {exit_code}")
    print("=" * 70)

    sys.exit(exit_code)


if __name__ == "__main__":
    run_all_verifications()
