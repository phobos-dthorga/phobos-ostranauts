"""Read vanilla navigation geometry into a local-only audit (requires UnityPy).

No textures, game code or asset files are exported or modified. Pass a local game
directory and an output path under .local; this is not a package-build dependency.
"""
import argparse
import hashlib
import json
import struct
from pathlib import Path

import UnityPy


def geometry(reader):
    tree = reader.read_typetree()
    return {key.removeprefix("m_"): tree[key] for key in (
        "m_AnchorMin", "m_AnchorMax", "m_SizeDelta", "m_AnchoredPosition",
        "m_Pivot", "m_LocalScale")}


def inspect(go, depth=0):
    result = {"name": go.m_Name, "components": [], "children": []}
    for component in go.m_Component:
        reader = component.component.deref()
        if reader.type.name == "RectTransform":
            result["rect"] = geometry(reader)
            if depth < 2:
                result["children"] = [inspect(c.read().m_GameObject.read(), depth + 1)
                                      for c in reader.read().m_Children]
        elif reader.type.name == "MonoBehaviour":
            # Managed payload type trees are stripped in this build. Read only
            # the built-in header to identify the script; don't guess its fields.
            data = reader.read(check_read=False)
            script = data.m_Script.read()
            result["components"].append(f"{script.m_Namespace}.{script.m_ClassName}".lstrip("."))
            if script.m_ClassName == "AspectRatioFitter":
                raw = reader.get_raw_data()
                if len(raw) != 40:
                    raise ValueError("AspectRatioFitter serialization changed; inspect before parsing")
                mode, ratio = struct.unpack("<if", raw[32:40])
                result["aspectFitter"] = {"mode": mode, "ratio": ratio}
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--game", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    output = args.output.resolve()
    if ".local" not in output.parts:
        parser.error("Keep the source-derived audit under .local")
    data = args.game / "Ostranauts_Data"
    report = {"unityPyVersion": UnityPy.__version__, "sources": {}, "objects": []}
    samples = {"NavModTimeZoom", "NavModControlToggle", "NavModFlightDynamics",
               "NavModMooringControl", "NavModDisplayControls", "GUIOrbitDraw"}
    for name in ("resources.assets", "level2"):
        source = data / name
        report["sources"][name] = hashlib.sha256(source.read_bytes()).hexdigest()
        env = UnityPy.load(str(source))
        for reader in env.objects:
            if reader.type.name != "GameObject":
                continue
            go = reader.read()
            selected = samples if name == "resources.assets" else {"pnlInteractionNav", "pnlInteractionUI"}
            if go.m_Name in selected:
                record = inspect(go)
                record["source"] = name
                record["pathId"] = reader.path_id
                report["objects"].append(record)
                container = next((c for c in record["children"] if c["name"] == "Container"), None)
                print(go.m_Name, json.dumps((container or record).get("rect", {})))
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(f"Local metadata audit: {output}")


if __name__ == "__main__":
    main()
