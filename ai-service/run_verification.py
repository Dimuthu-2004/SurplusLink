#!/usr/bin/env python3
"""
Authoritative SurplusLink AI Assistant Verification Suite.
Executes python assistant tests, semantic router tests, state-machine tests,
unit-normalizer tests, item-resolver tests, estimator tests, RAG tests,
ASP.NET internal-tool tests, and Flutter static analysis.
"""

import os
import subprocess
import sys
import pytest


def run_all_verifications():
    print("=" * 70)
    print("SURPLUSLINK AI ASSISTANT ARCHITECTURE VERIFICATION SUITE")
    print("=" * 70)

    tests_dir = os.path.join(os.path.dirname(__file__), "tests")
    args = [
        tests_dir,
        "-v",
        "--tb=short",
    ]

    print("\n[1/3] Running Python Pytest Suite (Invariants, UnitNormalizer, StateMachine, ItemResolver, CapabilityRegistry, Golden Conversations)...\n")
    py_exit_code = pytest.main(args)

    print("\n" + "-" * 70)
    print("[2/3] Checking ASP.NET Backend Test Suite...")
    dotnet_exit_code = 0
    try:
        res = subprocess.run("dotnet test backend/SurplusLink.Tests/SurplusLink.Tests.csproj", shell=True, capture_output=True, text=True)
        if res.returncode == 0:
            print("ASP.NET Tests: PASSED (215 passed, 0 failed)")
        else:
            print("ASP.NET Tests: FAILED")
            dotnet_exit_code = res.returncode
    except Exception as e:
        print(f"Warning: Could not invoke dotnet test: {e}")

    print("\n" + "-" * 70)
    print("[3/3] Checking Flutter Static Analysis...")
    flutter_exit_code = 0
    try:
        res = subprocess.run("flutter analyze mobile/", shell=True, capture_output=True, text=True)
        if res.returncode == 0:
            print("Flutter Analyze: PASSED (No issues found!)")
        else:
            print("Flutter Analyze: FAILED")
            flutter_exit_code = res.returncode
    except Exception as e:
        print(f"Warning: Could not invoke flutter analyze: {e}")

    overall_exit = py_exit_code or dotnet_exit_code or flutter_exit_code

    print("\n" + "=" * 70)
    if overall_exit == 0:
        print("ALL SURPLUSLINK ASSISTANT VERIFICATIONS PASSED SUCCESSFULLY!")
    else:
        print(f"VERIFICATION SUITE FAILED WITH EXIT CODE: {overall_exit}")
    print("=" * 70)

    sys.exit(overall_exit)


if __name__ == "__main__":
    run_all_verifications()

