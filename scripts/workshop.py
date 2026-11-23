"""Steam Workshop publishing through the shared steam-publish (phobos-modkit, pinned at external/phobos-modkit).

The same commands D'thorga Companion's Publish page runs; the package each item holds is built by
scripts/workshop_payload.py, which config/workshop-publishing.json names as its payload hook. Guide:
external/phobos-modkit/docs/workshop-publishing.md. The older scripts (prepare-workshop, upload-workshop, update-workshop,
pull-workshop) stay until this has made a real upload (the owner's choice of 23 Nov 2026: retire them after proof).

    python scripts/workshop.py status                      every mod: package, blockers, sizes, publication order
    python scripts/workshop.py pending                     which items have a newer version than the one uploaded
    python scripts/workshop.py show Agriculture            one mod's card: the change note Steam would get, receipts
    python scripts/workshop.py prepare Agriculture         stage a checked candidate, offline
    python scripts/workshop.py upload Agriculture --update OWNER ONLY: through the running Steam client (never an agent)

Agents never run upload or update (AGENTS.md): --what-if at most.
"""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
KIT = ROOT / 'external' / 'phobos-modkit' / 'python'


def main(argv=None):
    if not (KIT / 'phobos_modkit').is_dir():
        print('The shared tools are missing: run git submodule update --init', file=sys.stderr)
        return 2
    sys.path.insert(0, str(KIT))
    from phobos_modkit.steam.publish.cli import main as publish
    argv = list(sys.argv[1:] if argv is None else argv)
    if argv and not argv[0].startswith('-') and '--root' not in argv:
        argv[1:1] = ['--root', str(ROOT)]
    if hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    return publish(argv, root=ROOT)


if __name__ == '__main__':
    sys.exit(main())
