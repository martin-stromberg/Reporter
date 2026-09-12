// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

import { describe, test } from "node:test";
import assert from "node:assert/strict";
import { mkdtempSync, readFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";

import {
  buildReleaseAssets,
  releaseAssetEnv,
  emitReleaseAssetEnv
} from "./release-assets.mjs";

describe("buildReleaseAssets", () => {
  test("returns the mandatory assets without iOS when signing is disabled", () => {
    assert.deepEqual(buildReleaseAssets({ iosSigningEnabled: false }), [
      { assetName: "release-win-x64.zip", platform: "windows", runtimeIdentifier: "win-x64" },
      { assetName: "release-android.apk", platform: "android", runtimeIdentifier: "android" }
    ]);
  });

  test("includes release-ios.ipa when signing is enabled", () => {
    const assets = buildReleaseAssets({ iosSigningEnabled: true });
    assert.deepEqual(assets[assets.length - 1], {
      assetName: "release-ios.ipa",
      platform: "ios",
      runtimeIdentifier: "ios"
    });
    assert.equal(assets.length, 3);
  });
});

describe("releaseAssetEnv", () => {
  test("emits RELEASE_ASSETS, RELEASE_ASSET_PATHS and RELEASE_ASSET_FILES", () => {
    const env = releaseAssetEnv(buildReleaseAssets({ iosSigningEnabled: true }));
    assert.equal(
      env.RELEASE_ASSETS,
      "release-win-x64.zip:windows:win-x64;release-android.apk:android:android;release-ios.ipa:ios:ios"
    );
    assert.equal(env.RELEASE_ASSET_PATHS, "release-win-x64.zip;release-android.apk;release-ios.ipa");
    assert.equal(env.RELEASE_ASSET_FILES, "release-win-x64.zip release-android.apk release-ios.ipa");
  });
});

describe("emitReleaseAssetEnv", () => {
  test("writes all env entries to the GITHUB_ENV file", () => {
    const dir = mkdtempSync(join(tmpdir(), "release-assets-"));
    const envFile = join(dir, "github_env");
    emitReleaseAssetEnv({ iosSigningEnabled: false, githubEnv: envFile });
    const content = readFileSync(envFile, "utf8");
    assert.match(content, /RELEASE_ASSETS=release-win-x64\.zip:windows:win-x64;release-android\.apk:android:android\n/);
    assert.match(content, /RELEASE_ASSET_PATHS=release-win-x64\.zip;release-android\.apk\n/);
    assert.match(content, /RELEASE_ASSET_FILES=release-win-x64\.zip release-android\.apk\n/);
    assert.doesNotMatch(content, /release-ios\.ipa/);
  });

  test("throws when no GITHUB_ENV path is provided", () => {
    assert.throws(
      () => emitReleaseAssetEnv({ iosSigningEnabled: false, githubEnv: undefined }),
      /GITHUB_ENV is not set/
    );
  });
});
