#!/usr/bin/env python3
"""Serial deterministic .NET package/example builder. No registry publishing."""

import argparse
import hashlib
import json
import pathlib
import shutil
import stat
import subprocess
import zipfile

VERSION = "0.1.1"
STAMP = (2026, 10, 25, 0, 0, 0)

def secure_file(root, relative):
    """Reject a selected symlink at any level before reading package bytes."""
    path = root / relative
    for component in (path, *path.parents):
        if component.is_symlink():
            raise ValueError("Symlink is not a package input")
        if component == root:
            break
    if not stat.S_ISREG(path.stat().st_mode):
        raise ValueError("Package input must be a regular file")
    return path

def write_examples(root, archive):
    inputs = [(relative, secure_file(root, relative)) for relative in EXAMPLE_FILES]
    with zipfile.ZipFile(archive, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as output:
        for relative, source in inputs:
            entry = zipfile.ZipInfo(relative, STAMP)
            entry.compress_type = zipfile.ZIP_DEFLATED
            entry.external_attr = 0o100644 << 16
            output.writestr(entry, source.read_bytes())

def digest(path, algorithm="sha256"):
    return hashlib.new(algorithm, path.read_bytes()).hexdigest()

EXAMPLE_FILES = ('coverage.json', 'runtime-policy.json', 'README.md', 'METHODS.md', 'LICENSE', 'global.json', 'examples/FirstBooking/FirstBooking.csproj', 'examples/FirstBooking/Program.cs', 'examples/FirstBooking/NuGet.Config', 'CHANGELOG.md', 'SECURITY.md', 'SUPPORT.md', 'DEVELOPMENT.md', 'contract-projection.json')
CS_SOURCES = ('AvailKitException.cs', 'ClientCore.cs', 'ClientOptions.cs', 'Models.cs', 'PrivateFiles.cs', 'Protocol.cs', 'PublicBookingClient.cs', 'RequestIntent.cs', 'Secrets.cs', 'Webhooks.cs', 'WorkspaceClient.cs', 'AvailKit.csproj', 'contract.json')
PACKAGE_FILES = frozenset(("CHANGELOG.md", "SECURITY.md", "SUPPORT.md", "DEVELOPMENT.md", "contract-projection.json", "AvailKit.nuspec", "coverage.json", "runtime-policy.json", "LICENSE", "README.md", "[Content_Types].xml", "_rels/.rels", "lib/net10.0/AvailKit.dll", "package/services/metadata/core-properties/nuget.psmdcp"))

def normalize_package(source, destination):
    with zipfile.ZipFile(source) as original:
        names = original.namelist()
        if len(names) != len(PACKAGE_FILES) or set(names) != PACKAGE_FILES:
            raise ValueError("Unexpected NuGet package input")
        with zipfile.ZipFile(destination, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as output:
            for name in sorted(names):
                entry = zipfile.ZipInfo(name, STAMP)
                entry.compress_type = zipfile.ZIP_DEFLATED
                entry.external_attr = 0o100644 << 16
                output.writestr(entry, original.read(name))

def check_sources(root):
    source_root = root / "src/AvailKit"
    for path in source_root.rglob("*"):
        relative = path.relative_to(source_root)
        if relative.parts[0] in ("bin", "obj"):
            continue
        if path.is_symlink():
            raise ValueError("Symlink is not an SDK source input")
        if path.is_file() and relative.as_posix() not in CS_SOURCES:
            raise ValueError("Unexpected SDK source input")
    for name in CS_SOURCES:
        secure_file(root, "src/AvailKit/" + name)

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", required=True)
    parser.add_argument("--skip-tests", action="store_true")
    args = parser.parse_args()
    root = pathlib.Path(__file__).resolve().parent
    out = pathlib.Path(args.output).resolve()
    out.mkdir(parents=True, exist_ok=True)
    for relative in EXAMPLE_FILES:
        secure_file(root, relative)
    check_sources(root)
    raw = root / "build/raw"
    raw.mkdir(parents=True, exist_ok=True)
    if not args.skip_tests:
        subprocess.run(["dotnet", "build", "tests/AvailKit.Tests.csproj", "-c", "Release", "--disable-build-servers", "-m:1", "/p:UseSharedCompilation=false"], cwd=root, check=True)
        subprocess.run(["dotnet", "run", "--project", "tests/AvailKit.Tests.csproj", "-c", "Release", "--no-build", "--no-restore"], cwd=root, check=True)
    subprocess.run(["dotnet", "pack", "src/AvailKit/AvailKit.csproj", "-c", "Release", "--disable-build-servers", "--output", str(raw), "-m:1", "/p:UseSharedCompilation=false"], cwd=root, check=True)
    package = out / ("AvailKit." + VERSION + ".nupkg")
    normalize_package(secure_file(root, "build/raw/" + package.name), package)
    archive = out / ("availkit-dotnet-examples-" + VERSION + ".zip")
    write_examples(root, archive)
    for path in (package, archive):
        path.with_name(path.name + ".sha256").write_text(digest(path) + "  " + path.name + "\n")
    print(json.dumps({"artifacts": [package.name, archive.name]}))

if __name__ == "__main__":
    main()
