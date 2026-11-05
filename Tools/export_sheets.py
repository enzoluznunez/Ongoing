import argparse
import json
import math
import shutil
import sys
from pathlib import Path

PROJECT_ROOT = Path(__file__).resolve().parent.parent
DEFAULT_BACKEND = PROJECT_ROOT.parent / "Cyberattack-Platform" / "Backend"
STREAMING_ASSETS = PROJECT_ROOT / "Assets" / "StreamingAssets"
NATIONAL_SHEET = "United States"
ROW_COLUMNS = ["breach_key", "company", "ticker", "disclosed_on", "region", "city", "lat", "lon", "metro"]


def blank_to_none(value):
    if value is None:
        return None
    if isinstance(value, float) and math.isnan(value):
        return None
    if value.__class__.__name__ in ("NAType", "NaTType"):
        return None
    return value


def read_us_rows(backend, clean, api):
    breaches, _ = clean.usable(*clean.read_raw(str(backend / "data")))
    columns = ROW_COLUMNS + list(api.METRICS)
    us_breaches = breaches[breaches["country"] == api.US]
    return [
        {column: blank_to_none(record[column]) for column in columns}
        for record in us_breaches[columns].astype(object).to_dict("records")
    ]


def sheet_path(sheets_folder, place):
    return sheets_folder / f"{place.replace('/', '-').replace(chr(92), '-')}.csv"


def write_sheet(sheets_folder, place, text):
    if text is None:
        raise ValueError(f"The map lists {place}, but it has no sheet.")
    sheet_path(sheets_folder, place).write_text(text, encoding="utf-8", newline="\n")


def export(backend, output):
    sys.path.insert(0, str(backend))
    import api
    import clean

    rows = read_us_rows(backend, clean, api)
    breach_map = api.breach_map(rows)

    sheets_folder = output / "Sheets"
    if sheets_folder.exists():
        shutil.rmtree(sheets_folder)
    sheets_folder.mkdir(parents=True)

    write_sheet(sheets_folder, NATIONAL_SHEET, api.us_sheet(rows))
    for region in breach_map["regions"]:
        name = region["region"]
        write_sheet(sheets_folder, name, api.metro_coloured_sheet(rows, lambda row: row["region"] == name))

    charted_metros = sorted({city["metro"] for city in breach_map["cities"] if city["metro"] and city["charted"]})
    for metro in charted_metros:
        write_sheet(sheets_folder, metro, api.metro_coloured_sheet(rows, lambda row: row["metro"] == metro))

    (output / "map.json").write_text(json.dumps(breach_map, ensure_ascii=False), encoding="utf-8", newline="\n")
    print(f"{len(rows)} breaches -> map.json, {len(breach_map['regions'])} regions, "
          f"{len(breach_map['cities'])} cities, {len(charted_metros)} metro sheets in {output}")


def main():
    parser = argparse.ArgumentParser(description="Write map.json and every sheet from the backend's local exports.")
    parser.add_argument("--backend", type=Path, default=DEFAULT_BACKEND)
    parser.add_argument("--output", type=Path, default=STREAMING_ASSETS)
    arguments = parser.parse_args()
    export(arguments.backend.resolve(), arguments.output.resolve())
    return 0


if __name__ == "__main__":
    sys.exit(main())
