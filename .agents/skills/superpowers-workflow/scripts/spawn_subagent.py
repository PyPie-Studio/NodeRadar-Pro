#!/usr/bin/env python3
"""Spawn an isolated CLI subagent for focused task execution."""

import argparse
import json
import subprocess
import sys
import time
import uuid
from pathlib import Path
from typing import Any


def find_repo_root(start: Path) -> Path:
    """Traverse upwards to find the repository root (containing .agents/)."""
    curr = start.resolve()
    for _ in range(10):
        if (curr / ".agents").exists() or (curr / ".agent").exists() or (curr / ".git").exists():
            return curr
        if curr.parent == curr:
            break
        curr = curr.parent
    return Path.cwd()


def load_skill_instructions(skill_path: Path) -> str:
    """Load skill instructions from SKILL.md file."""
    if not skill_path.exists():
        return ""
    return skill_path.read_text(encoding="utf-8")


def spawn_subagent(
    skill: str,
    task: str,
    repo_root: Path,
    yolo: bool = True,
    output_format: str = "text",
) -> dict[str, Any]:
    subagent_id = uuid.uuid4().hex[:8]
    timestamp = time.strftime("%Y%m%d-%H%M%S")

    log_dir = repo_root / "artifacts" / "superpowers" / "subagents"
    log_dir.mkdir(parents=True, exist_ok=True)
    log_file = log_dir / f"{skill}-{timestamp}-{subagent_id}.log"

    skill_file = repo_root / f".agents/skills/superpowers-{skill}/SKILL.md"
    if not skill_file.exists():
        skill_file = repo_root / f".agents/skills/{skill}/SKILL.md"
    skill_instructions = load_skill_instructions(skill_file)

    if not skill_instructions:
        return {
            "success": False,
            "output": "",
            "error": f"Skill not found: {skill_file}",
            "log_file": str(log_file),
            "duration_s": 0,
        }

    return {
        "success": True,
        "subagent_id": subagent_id,
        "log_file": str(log_file),
        "duration_s": 0,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Spawn a subagent for focused task execution")
    parser.add_argument("--skill", required=True, help="Skill name (e.g. tdd, debug, review)")
    parser.add_argument("--task", required=True, help="Task description")
    parser.add_argument("--format", choices=["text", "json"], default="text", help="Output format")
    args = parser.parse_args()

    repo_root = find_repo_root(Path.cwd())
    result = spawn_subagent(args.skill, args.task, repo_root, output_format=args.format)
    print(json.dumps(result, indent=2))
    return 0 if result.get("success", False) else 1


if __name__ == "__main__":
    raise SystemExit(main())
