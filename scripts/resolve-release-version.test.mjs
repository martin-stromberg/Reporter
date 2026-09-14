// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

import { describe, test } from "node:test";
import assert from "node:assert/strict";

import {
  parseManualTag,
  classifyWorkflowRef,
  releaseHasExpectedAsset,
  incompleteReleases,
  semanticReleaseDryRunVersion,
  repairIncompleteRelease
} from "./resolve-release-version.mjs";

function uploadedAsset(name, size = 100) {
  return { name, state: "uploaded", size };
}

function completeRelease(overrides = {}) {
  return {
    assets: [
      uploadedAsset("release-win-x64.zip"),
      uploadedAsset("release-android.apk"),
      uploadedAsset("update.json")
    ],
    ...overrides
  };
}

describe("parseManualTag", () => {
  test("returns the version without the leading v for a valid tag", () => {
    assert.equal(parseManualTag("v1.2.3"), "1.2.3");
    assert.equal(parseManualTag("v0.0.1"), "0.0.1");
    assert.equal(parseManualTag("v1.2.3-beta.1"), "1.2.3-beta.1");
  });

  test("rejects a tag without the leading v", () => {
    assert.throws(() => parseManualTag("1.2.3"), /Expected a vX\.Y\.Z tag/);
  });

  test("rejects non-semver tag names", () => {
    assert.throws(() => parseManualTag("v1.2"), /not a valid vX\.Y\.Z release tag/);
    assert.throws(() => parseManualTag("v1.2.3.4"), /not a valid vX\.Y\.Z release tag/);
    assert.throws(() => parseManualTag("vlatest"), /not a valid vX\.Y\.Z release tag/);
  });

  test("rejects empty or missing input", () => {
    assert.throws(() => parseManualTag(""), /Expected a vX\.Y\.Z tag/);
    assert.throws(() => parseManualTag(undefined), /Expected a vX\.Y\.Z tag/);
  });
});

describe("classifyWorkflowRef", () => {
  test("classifies a tag ref as manual release with version and tag", () => {
    assert.deepEqual(
      classifyWorkflowRef({ refType: "tag", refName: "v2.0.0" }),
      { kind: "manual", version: "2.0.0", tag: "v2.0.0" }
    );
  });

  test("classifies a push to main as automatic release", () => {
    assert.deepEqual(
      classifyWorkflowRef({ refType: "branch", refName: "main" }),
      { kind: "automatic" }
    );
  });

  test("rejects branches that are not release branches", () => {
    assert.throws(
      () => classifyWorkflowRef({ refType: "branch", refName: "staging" }),
      /Unsupported release ref/
    );
  });

  test("rejects unsupported ref types", () => {
    assert.throws(
      () => classifyWorkflowRef({ refType: "pull_request", refName: "42" }),
      /Unsupported release ref/
    );
    assert.throws(
      () => classifyWorkflowRef({ refType: undefined, refName: undefined }),
      /Unsupported release ref/
    );
  });
});

describe("releaseHasExpectedAsset", () => {
  test("returns true when all expected assets are uploaded and non-empty", () => {
    assert.equal(releaseHasExpectedAsset(completeRelease()), true);
  });

  test("returns false when an expected asset is missing", () => {
    const release = completeRelease();
    release.assets = release.assets.filter((asset) => asset.name !== "release-android.apk");
    assert.equal(releaseHasExpectedAsset(release), false);
  });

  test("returns false when an expected asset is empty", () => {
    const release = completeRelease();
    release.assets.find((asset) => asset.name === "update.json").size = 0;
    assert.equal(releaseHasExpectedAsset(release), false);
  });

  test("returns false when an expected asset is not in uploaded state", () => {
    const release = completeRelease();
    release.assets.find((asset) => asset.name === "release-win-x64.zip").state = "starter";
    assert.equal(releaseHasExpectedAsset(release), false);
  });

  test("returns false when the release has no assets at all", () => {
    assert.equal(releaseHasExpectedAsset({}), false);
    assert.equal(releaseHasExpectedAsset({ assets: [] }), false);
  });

  test("requires release-ios.ipa only when IOS_SIGNING_ENABLED is true", async () => {
    process.env.IOS_SIGNING_ENABLED = "true";
    try {
      const mod = await import("./resolve-release-version.mjs?ios-enabled");

      const withoutIos = completeRelease();
      assert.equal(mod.releaseHasExpectedAsset(withoutIos), false);

      const withIos = completeRelease();
      withIos.assets.push(uploadedAsset("release-ios.ipa"));
      assert.equal(mod.releaseHasExpectedAsset(withIos), true);
    } finally {
      delete process.env.IOS_SIGNING_ENABLED;
    }
  });
});

describe("semanticReleaseDryRunVersion", () => {
  test("returns the announced version on a clean exit", () => {
    const result = {
      status: 0,
      stdout: "[semantic-release] › ℹ  The next release version is 1.4.2",
      stderr: ""
    };
    assert.equal(semanticReleaseDryRunVersion(result), "1.4.2");
  });

  test("returns null on a clean exit without a version announcement", () => {
    const result = {
      status: 0,
      stdout: "[semantic-release] › ℹ  There are no relevant changes, so no new version is released.",
      stderr: ""
    };
    assert.equal(semanticReleaseDryRunVersion(result), null);
  });

  test("throws with stderr context when semantic-release exits non-zero", () => {
    const result = {
      status: 1,
      stdout: "",
      stderr: "npm error code EAI_AGAIN"
    };
    assert.throws(
      () => semanticReleaseDryRunVersion(result),
      /exit code 1[\s\S]*EAI_AGAIN/
    );
  });

  test("throws when the semantic-release process could not be spawned", () => {
    const result = {
      error: new Error("spawn npx ENOENT"),
      status: null,
      stdout: "",
      stderr: ""
    };
    assert.throws(() => semanticReleaseDryRunVersion(result), /ENOENT/);
  });
});

describe("repairIncompleteRelease", () => {
  test("derives version and tag from a valid vX.Y.Z release tag", () => {
    const result = repairIncompleteRelease({ tag_name: "v0.0.1" });
    assert.equal(result.version, "0.0.1");
    assert.equal(result.tag, "v0.0.1");
    assert.equal(result.released, "true");
    assert.equal(result.release_kind, "automatic");
    assert.equal(result.release_action, "upload-existing");
  });

  test("rejects a release tag that is not a valid vX.Y.Z tag instead of corrupting the version", () => {
    assert.throws(
      () => repairIncompleteRelease({ tag_name: "1.2.3" }),
      /Expected a vX\.Y\.Z tag/
    );
    assert.throws(
      () => repairIncompleteRelease({ tag_name: "release-1.2.3" }),
      /Expected a vX\.Y\.Z tag/
    );
  });
});

describe("incompleteReleases (prerelease guard, template 11.1)", () => {
  test("never selects a prerelease for repair, even when it has no assets", () => {
    const releases = [
      { tag_name: "v1.0.0-rc.1", prerelease: true, assets: [] },
      { tag_name: "v0.0.1", prerelease: false, ...completeRelease() }
    ];
    assert.deepEqual(incompleteReleases(releases), []);
  });

  test("selects stable releases that are missing expected assets", () => {
    const incompleteStable = {
      tag_name: "v0.0.1",
      prerelease: false,
      assets: [uploadedAsset("release-win-x64.zip")]
    };
    const releases = [
      completeRelease({ tag_name: "v1.0.0", prerelease: false }),
      { tag_name: "v1.0.0-rc.3", prerelease: true, assets: [] },
      incompleteStable
    ];
    assert.deepEqual(incompleteReleases(releases), [incompleteStable]);
  });

  test("keeps the API order so the caller can pick the oldest incomplete release", () => {
    const older = { tag_name: "v0.0.1", prerelease: false, assets: [] };
    const newer = {
      tag_name: "v0.1.0",
      prerelease: false,
      assets: [uploadedAsset("update.json")]
    };
    const result = incompleteReleases([newer, older]);
    assert.equal(result[result.length - 1], older);
  });
});
