"""Puts the newest installer (link, version, size and SHA-256) into the website.

GitHub's "latest release" link skips pre-releases, and every EcclesiaCast
version so far is a beta, so the site's download buttons point straight at
the newest published installer instead. Run by .github/workflows/pages.yml
before uploading sitio/; locally it needs no token for a public repository.

The HTML marks what to fill with data-release="url|version|size|sha". If no
installer is found, the files are left as they are.
"""

import json
import os
import pathlib
import re
import sys
import urllib.request

REPO = "EcclesiaCast/EcclesiaCast"
SITE = pathlib.Path(__file__).resolve().parents[2] / "sitio"


def newest_installer():
    request = urllib.request.Request(
        f"https://api.github.com/repos/{REPO}/releases?per_page=20",
        headers={"Accept": "application/vnd.github+json"},
    )
    token = os.environ.get("GH_TOKEN")
    if token:
        request.add_header("Authorization", f"Bearer {token}")
    with urllib.request.urlopen(request, timeout=30) as response:
        releases = json.load(response)

    for release in releases:  # newest first
        if release.get("draft"):
            continue
        for asset in release.get("assets", []):
            if asset["name"].lower().endswith("-setup.exe"):
                digest = asset.get("digest") or ""
                return {
                    "url": asset["browser_download_url"],
                    "version": release["tag_name"].lstrip("v"),
                    "size": f"{round(asset['size'] / 1_000_000)} MB",
                    "sha": digest.removeprefix("sha256:") if digest.startswith("sha256:") else "",
                }
    return None


def fill(html, data):
    # href="…" data-release="url"
    html = re.sub(r'href="[^"]*"(\s+data-release="url")', lambda m: f'href="{data["url"]}"{m.group(1)}', html)
    for key in ("version", "size", "sha"):
        if data[key]:
            html = re.sub(
                rf'(<[^>]*data-release="{key}"[^>]*>)[^<]*(<)',
                lambda m: f"{m.group(1)}{data[key]}{m.group(2)}",
                html,
            )
    return html


def main():
    data = newest_installer()
    if data is None:
        print("No hay ningún instalador publicado: el sitio queda como está.")
        return 0
    print(f"Instalador: {data['version']}, {data['size']}, {data['url']}")
    if not data["sha"]:
        print("GitHub no dio la huella SHA-256 de ese instalador: queda la que estaba.", file=sys.stderr)
    for page in SITE.glob("*.html"):
        text = page.read_text(encoding="utf-8")
        new = fill(text, data)
        if new != text:
            page.write_text(new, encoding="utf-8", newline="\n")
            print(f"  actualizado {page.name}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
