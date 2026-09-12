import { describe, test } from "node:test";
import assert from "node:assert/strict";
import { createHash } from "node:crypto";
import { mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";

import {
  parseReleaseAssets,
  createUpdateManifest,
  writeUpdateManifest
} from "./create-update-manifest.mjs";

function sha256Hex(content) {
  return createHash("sha256").update(content).digest("hex");
}

function withTempDir(fn) {
  const dir = mkdtempSync(join(tmpdir(), "update-manifest-"));
  try {
    return fn(dir);
  } finally {
    rmSync(dir, { recursive: true, force: true });
  }
}

describe("parseReleaseAssets", () => {
  test("parses semicolon-separated name:platform:runtimeIdentifier entries", () => {
    assert.deepEqual(
      parseReleaseAssets("release-win-x64.zip:windows:win-x64;release-android.apk:android:android"),
      [
        { assetName: "release-win-x64.zip", platform: "windows", runtimeIdentifier: "win-x64" },
        { assetName: "release-android.apk", platform: "android", runtimeIdentifier: "android" }
      ]
    );
  });

  test("rejects a missing or empty spec", () => {
    assert.throws(() => parseReleaseAssets(undefined), /RELEASE_ASSETS is not set/);
    assert.throws(() => parseReleaseAssets(""), /RELEASE_ASSETS is not set/);
    assert.throws(() => parseReleaseAssets("   "), /RELEASE_ASSETS is not set/);
  });

  test("rejects malformed entries", () => {
    assert.throws(() => parseReleaseAssets("release-win-x64.zip"), /Invalid RELEASE_ASSETS entry/);
    assert.throws(() => parseReleaseAssets("a.zip:windows"), /Invalid RELEASE_ASSETS entry/);
    assert.throws(() => parseReleaseAssets("a.zip:windows:win-x64:extra"), /Invalid RELEASE_ASSETS entry/);
    assert.throws(() => parseReleaseAssets("a.zip::win-x64"), /Invalid RELEASE_ASSETS entry/);
  });
});

describe("createUpdateManifest", () => {
  test("writes an update.json entry per asset with url, sha256 and sizeBytes", () => {
    withTempDir((dir) => {
      const winContent = Buffer.from("win zip payload");
      const apkContent = Buffer.from("android apk payload");
      writeFileSync(join(dir, "release-win-x64.zip"), winContent);
      writeFileSync(join(dir, "release-android.apk"), apkContent);

      const assets = parseReleaseAssets(
        "release-win-x64.zip:windows:win-x64;release-android.apk:android:android"
      );
      const manifest = createUpdateManifest({
        version: "1.2.3-rc.4",
        tag: "v1.2.3-rc.4",
        repository: "martin-stromberg/Reporter",
        assets,
        baseDir: dir
      });

      assert.equal(manifest.version, "1.2.3-rc.4");
      assert.equal(manifest.releaseNotes, "Reporter release v1.2.3-rc.4");
      assert.match(manifest.publishedAt, /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z$/);
      assert.equal(manifest.assets.length, 2);

      const [win, apk] = manifest.assets;
      assert.deepEqual(
        { platform: win.platform, runtimeIdentifier: win.runtimeIdentifier, assetName: win.assetName },
        { platform: "windows", runtimeIdentifier: "win-x64", assetName: "release-win-x64.zip" }
      );
      assert.equal(
        win.assetUrl,
        "https://github.com/martin-stromberg/Reporter/releases/download/v1.2.3-rc.4/release-win-x64.zip"
      );
      assert.match(win.sha256, /^[0-9a-f]{64}$/);
      assert.equal(win.sha256, sha256Hex(winContent));
      assert.equal(win.sizeBytes, winContent.length);
      assert.equal(apk.sha256, sha256Hex(apkContent));
      assert.equal(apk.sizeBytes, apkContent.length);
    });
  });

  test("fails when an expected asset file is missing", () => {
    withTempDir((dir) => {
      writeFileSync(join(dir, "release-win-x64.zip"), "win zip payload");
      const assets = parseReleaseAssets(
        "release-win-x64.zip:windows:win-x64;release-android.apk:android:android"
      );
      assert.throws(
        () =>
          createUpdateManifest({
            version: "1.2.3",
            tag: "v1.2.3",
            repository: "martin-stromberg/Reporter",
            assets,
            baseDir: dir
          }),
        /release-android\.apk.*missing/
      );
    });
  });

  test("fails when an expected asset file is empty", () => {
    withTempDir((dir) => {
      writeFileSync(join(dir, "release-win-x64.zip"), "");
      const assets = parseReleaseAssets("release-win-x64.zip:windows:win-x64");
      assert.throws(
        () =>
          createUpdateManifest({
            version: "1.2.3",
            tag: "v1.2.3",
            repository: "martin-stromberg/Reporter",
            assets,
            baseDir: dir
          }),
        /is empty/
      );
    });
  });

  test("requires version, tag and repository", () => {
    const assets = [{ assetName: "a.zip", platform: "p", runtimeIdentifier: "r" }];
    assert.throws(
      () => createUpdateManifest({ version: "", tag: "v1", repository: "o/r", assets }),
      /RELEASE_VERSION is not set/
    );
    assert.throws(
      () => createUpdateManifest({ version: "1", tag: "", repository: "o/r", assets }),
      /RELEASE_TAG is not set/
    );
    assert.throws(
      () => createUpdateManifest({ version: "1", tag: "v1", repository: "", assets }),
      /GITHUB_REPOSITORY is not set/
    );
  });
});

describe("writeUpdateManifest", () => {
  test("writes the manifest as JSON to update.json", () => {
    withTempDir((dir) => {
      const outputPath = join(dir, "update.json");
      writeUpdateManifest({ version: "1.0.0", assets: [] }, outputPath);
      const written = JSON.parse(readFileSync(outputPath, "utf8"));
      assert.deepEqual(written, { version: "1.0.0", assets: [] });
    });
  });
});
