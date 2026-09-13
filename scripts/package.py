#!/usr/bin/env python3
"""Build a small Thunderstore package from the Release output."""

from pathlib import Path
import zipfile


PROJECT_NAME = "SwmarlyValheimBetterFistsWeapons"
VERSION = "1.0.0"


def find_build_output(root: Path) -> Path:
    candidates = sorted((root / "src" / "bin").rglob(PROJECT_NAME + ".dll"))
    if not candidates:
        raise FileNotFoundError(
            "Build output was not found under src/bin. Build the project before packaging."
        )
    return candidates[0]


def main() -> None:
    root = Path(__file__).resolve().parents[1]
    dll = find_build_output(root)
    pdb = dll.with_suffix(".pdb")
    manifest = root / "thunderstore" / "manifest.json"
    icon = root / "thunderstore" / "icon.png"
    if not icon.exists():
        raise FileNotFoundError("Thunderstore icon was not found at thunderstore/icon.png")
    output_dir = root / "dist"
    output_dir.mkdir(parents=True, exist_ok=True)
    archive = output_dir / f"{PROJECT_NAME}-{VERSION}.zip"

    with zipfile.ZipFile(archive, "w", compression=zipfile.ZIP_DEFLATED) as package:
        package.write(
            dll,
            f"BepInEx/plugins/{PROJECT_NAME}/{dll.name}",
        )
        if pdb.exists():
            package.write(
                pdb,
                f"BepInEx/plugins/{PROJECT_NAME}/{pdb.name}",
            )
        package.write(manifest, "manifest.json")
        package.write(icon, "icon.png")
        package.write(root / "README.md", "README.md")
        package.write(root / "CHANGELOG.md", "CHANGELOG.md")

    print(f"Created {archive}")


if __name__ == "__main__":
    main()
