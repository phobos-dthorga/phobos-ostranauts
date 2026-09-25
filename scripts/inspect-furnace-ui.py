"""Read vanilla furnace UI candidates; metadata only, output must remain in .local.

Requires UnityPy, as does inspect-nav-layout.py. Custom field decoding is pinned
to the inspected assembly/Unity version and fails closed on changed layouts.
No images, assemblies or decompiled source are exported by this script.
"""
import argparse
import hashlib
import json
import struct
from pathlib import Path

import UnityPy
from UnityPy.classes import PPtr

ASSEMBLY_HASH = "91b50f45cacd64de39b9bcc30ec7b4542f3e3976ac3bc5589b346976a262425e"
UNITY_VERSION = "6000.3.23f1"
SAMPLES = {"GUIReactor", "GUIAirPump", "GUIMeter", "NavModTimeZoom",
           "NavModFlightDynamics", "NavModControlToggle"}


def reference(reader, raw, offset, expected=None):
    file_id, path_id = struct.unpack_from("<iq", raw, offset)
    pointer = PPtr(m_FileID=file_id, m_PathID=path_id, assetsfile=reader.assets_file)
    result = {"fileId": file_id, "pathId": path_id}
    if path_id:
        target = pointer.deref()
        if expected and target.type.name != expected:
            raise ValueError(f"Expected {expected}, found {target.type.name}")
        data = target.read(check_read=False)
        result.update(type=target.type.name, name=getattr(data, "m_Name", ""))
        if expected == "Sprite":
            result["rect"] = {key: getattr(data.m_Rect, key) for key in ("x", "y", "width", "height")}
        elif hasattr(data, "m_GameObject"):
            result["object"] = data.m_GameObject.read().m_Name
    return result


def sprite_array(reader, raw, offset):
    count = struct.unpack_from("<i", raw, offset)[0]
    if not 1 <= count <= 128 or offset + 4 + count * 12 > len(raw):
        raise ValueError("Changed sprite array layout")
    return [reference(reader, raw, offset + 4 + i * 12, "Sprite") for i in range(count)]


def skip_curve(raw, offset):
    count = struct.unpack_from("<i", raw, offset)[0]
    if not 0 <= count <= 128:
        raise ValueError("Changed animation curve layout")
    return offset + 4 + 28 * count + 12


def fields(reader, name):
    if name not in {"GUIKnob", "GUILedMeter", "GUI7Seg", "GUILamp", "GUISafetyToggle", "GUIToggleSwap"}:
        return {}
    raw = reader.get_raw_data()
    # Built-in MonoBehaviour header: object, enabled/alignment, script, empty name.
    if struct.unpack_from("<i", raw, 28)[0] != 0:
        raise ValueError("Unexpected named MonoBehaviour header")
    if name == "GUIToggleSwap":
        if len(raw) != 56:
            raise ValueError("Changed toggle-swap fields")
        return {"targetToggle": reference(reader, raw, 32),
                "selectedSprite": reference(reader, raw, 44, "Sprite")}
    if name == "GUISafetyToggle":
        if len(raw) != 96:
            raise ValueError("Changed safety-toggle fields")
        return {key: reference(reader, raw, 32 + i * 12) for i, key in
                enumerate(("btnOpen", "btnClosed", "cgOpen", "cgClosed", "chkSwitch"))}
    offset = skip_curve(raw, skip_curve(raw, 32)) if name == "GUILamp" else 32
    key = {"GUIKnob": "aStates", "GUILedMeter": "aLEDs",
           "GUI7Seg": "aSpriteSheet", "GUILamp": "aSprites"}[name]
    return {key: sprite_array(reader, raw, offset)}


def inspect(go, path):
    result = {"path": path, "components": [], "children": []}
    for component in go.m_Component:
        reader = component.component.deref()
        if reader.type.name in ("RectTransform", "Transform"):
            data = reader.read()
            if reader.type.name == "RectTransform":
                tree = reader.read_typetree()
                result["geometry"] = {key: tree[key] for key in
                    ("m_AnchorMin", "m_AnchorMax", "m_SizeDelta", "m_AnchoredPosition", "m_Pivot")}
            result["children"] = [inspect(c.read().m_GameObject.read(), path + "/" +
                                  c.read().m_GameObject.read().m_Name) for c in data.m_Children]
        elif reader.type.name == "MonoBehaviour":
            data = reader.read(check_read=False)
            if data.m_Script.path_id:
                script = data.m_Script.read()
                result["components"].append({"type": script.m_ClassName,
                                             "fields": fields(reader, script.m_ClassName)})
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--game", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    if ".local" not in args.output.resolve().parts:
        parser.error("Keep native metadata under .local")
    root = args.game / "Ostranauts_Data"
    assembly_hash = hashlib.sha256((root / "Managed/Assembly-CSharp.dll").read_bytes()).hexdigest()
    if assembly_hash != ASSEMBLY_HASH:
        parser.error("Assembly changed: inspect component contracts before updating the decoder")
    source = root / "resources.assets"
    env = UnityPy.load(str(source))
    assets = next(iter(env.files.values()))
    if assets.unity_version != UNITY_VERSION:
        parser.error("Unity serialization version changed")
    report = {"assemblySha256": assembly_hash, "unityVersion": assets.unity_version,
              "resourceSha256": hashlib.sha256(source.read_bytes()).hexdigest(), "prefabs": []}
    for reader in env.objects:
        if reader.type.name == "GameObject":
            go = reader.read()
            if go.m_Name in SAMPLES:
                report["prefabs"].append(inspect(go, go.m_Name))
    if not any(p["path"] == "GUIReactor" for p in report["prefabs"]):
        raise ValueError("Reactor prefab missing")
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print("Inspected:", ", ".join(p["path"] for p in report["prefabs"]))


if __name__ == "__main__":
    main()
