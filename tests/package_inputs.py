"""Exercise the real archive writer with injected private files and symlink replacements."""
import importlib.util
import pathlib
import tempfile
import unittest
import zipfile

ROOT = pathlib.Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location("sdk_packager", ROOT / "build.py")
builder = importlib.util.module_from_spec(spec)
spec.loader.exec_module(builder)

class PackageInputs(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="availkit-package-")
        self.root = pathlib.Path(self.temp.name) / "sdk"
        self.root.mkdir()
        for relative in builder.EXAMPLE_FILES:
            path = self.root / relative
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text("Public fixture: " + relative)
        self.archive = pathlib.Path(self.temp.name) / "examples.zip"

    def tearDown(self):
        self.temp.cleanup()

    def test_private_nested_files_are_not_packaged(self):
        for relative in ("examples/.local/settings.xml", "examples/private/config.xml", "examples/private/NuGet.Config", "examples/private/secret.cs", "examples/private/secret.kt", "examples/private/secret.java"):
            path = self.root / relative
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text("PRIVATE-INJECTION-SENTINEL")
        builder.write_examples(self.root, self.archive)
        with zipfile.ZipFile(self.archive) as archive:
            self.assertEqual(set(archive.namelist()), set(builder.EXAMPLE_FILES))
            self.assertFalse(any(b"PRIVATE-INJECTION-SENTINEL" in archive.read(name) for name in archive.namelist()))

    def test_selected_file_symlink_is_rejected(self):
        target = pathlib.Path(self.temp.name) / "private.xml"
        target.write_text("PRIVATE-INJECTION-SENTINEL")
        path = self.root / builder.EXAMPLE_FILES[-1]
        path.unlink()
        path.symlink_to(target)
        with self.assertRaises(ValueError):
            builder.write_examples(self.root, self.archive)

    def test_selected_parent_symlink_is_rejected(self):
        examples = self.root / "examples"
        moved = pathlib.Path(self.temp.name) / "elsewhere"
        examples.rename(moved)
        examples.symlink_to(moved, target_is_directory=True)
        with self.assertRaises(ValueError):
            builder.write_examples(self.root, self.archive)

    def test_root_notice_symlink_is_rejected(self):
        target = pathlib.Path(self.temp.name) / "notice"
        target.write_text("PRIVATE-INJECTION-SENTINEL")
        path = self.root / "README.md"
        path.unlink()
        path.symlink_to(target)
        with self.assertRaises(ValueError):
            builder.write_examples(self.root, self.archive)

    def test_example_bytes_are_deterministic(self):
        second = self.archive.with_name("repeat.zip")
        builder.write_examples(self.root, self.archive)
        builder.write_examples(self.root, second)
        self.assertEqual(self.archive.read_bytes(), second.read_bytes())

if __name__ == "__main__":
    unittest.main()
