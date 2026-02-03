import json
import sys
import argparse
from pathlib import Path
from typing import Dict, Set


def load_json_keys(file_path: Path) -> Set[str]:
    """Load a JSON file and return its keys as a set."""
    try:
        # Use utf-8-sig to handle potential BOM
        with open(file_path, 'r', encoding='utf-8-sig') as f:
            data = json.load(f)
            return set(data.keys())
    except json.JSONDecodeError as e:
        print(f"  ERROR: Invalid JSON in {file_path.name}: {e}")
        return set()
    except Exception as e:
        print(f"  ERROR: Could not read {file_path.name}: {e}")
        return set()


def find_missing_keys(reference_keys: Set[str], target_keys: Set[str]) -> Set[str]:
    """Find keys that are in reference but not in target."""
    return reference_keys - target_keys


def find_extra_keys(reference_keys: Set[str], target_keys: Set[str]) -> Set[str]:
    """Find keys that are in target but not in reference."""
    return target_keys - reference_keys


def main():
    parser = argparse.ArgumentParser(description="Check for missing translation keys.")
    parser.add_argument("lang_code", nargs='?', default='all', help="Language code (e.g. 'de') or 'all' (default) to check all files.")
    parser.add_argument("--localization-dir", help="Path to localization directory", default=None)
    
    args = parser.parse_args()

    # Determine localization directory
    if args.localization_dir:
        localization_dir = Path(args.localization_dir)
    else:
        # Default: relative to this script's location
        script_dir = Path(__file__).parent
        localization_dir = script_dir.parent / "Flowery.Uno.Gallery" / "Localization"

    if not localization_dir.exists():
        print(f"ERROR: Localization directory not found: {localization_dir}")
        sys.exit(1)

    # Load English (reference) keys
    en_file = localization_dir / "en.json"
    if not en_file.exists():
        print(f"ERROR: Reference file not found: {en_file}")
        sys.exit(1)

    print(f"Loading reference file: {en_file.name}")
    en_keys = load_json_keys(en_file)
    print(f"  Found {len(en_keys)} keys in en.json\n")

    # Determine target files
    json_files = []
    if args.lang_code.lower() == 'all':
        json_files = sorted([f for f in localization_dir.glob("*.json") if f.name != "en.json"])
    else:
        target_file = localization_dir / f"{args.lang_code}.json"
        if not target_file.exists():
             print(f"ERROR: File not found: {target_file}")
             sys.exit(1)
        json_files = [target_file]

    if not json_files:
        print("No language files found.")
        sys.exit(0)

    total_missing = 0
    total_extra = 0
    files_with_issues = 0

    print("=" * 60)
    print("MISSING TRANSLATIONS REPORT")
    print("=" * 60)

    for json_file in json_files:
        lang_keys = load_json_keys(json_file)
        missing_keys = find_missing_keys(en_keys, lang_keys)
        extra_keys = find_extra_keys(en_keys, lang_keys)

        if missing_keys or extra_keys:
            files_with_issues += 1
            print(f"\n{json_file.name}:")
            
            if missing_keys:
                print(f"  Missing ({len(missing_keys)} keys):")
                for key in sorted(missing_keys):
                    print(f"    - {key}")
                total_missing += len(missing_keys)

            if extra_keys:
                print(f"  Extra ({len(extra_keys)} keys not in en.json):")
                for key in sorted(extra_keys):
                    print(f"    + {key}")
                total_extra += len(extra_keys)
        else:
            print(f"\n{json_file.name}: ✓ Complete ({len(lang_keys)} keys)")

    # Summary
    print("\n" + "=" * 60)
    print("SUMMARY")
    print("=" * 60)
    print(f"Total language files checked: {len(json_files)}")
    print(f"Files with issues: {files_with_issues}")
    print(f"Total missing keys: {total_missing}")
    print(f"Total extra keys: {total_extra}")

    if total_missing > 0 or total_extra > 0:
        sys.exit(1)
    else:
        print("\n✓ All translations are complete!")
        sys.exit(0)


if __name__ == "__main__":
    main()
