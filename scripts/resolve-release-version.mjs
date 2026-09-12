// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

import { spawnSync } from "node:child_process";
import { appendFileSync } from "node:fs";
import { pathToFileURL } from "node:url";

import { buildReleaseAssets } from "./release-assets.mjs";

const VERSION_PATTERN = /^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(-[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?$/;
const AUTOMATIC_RELEASE_BRANCHES = ["main"];
const EXPECTED_ASSETS = [
  ...buildReleaseAssets({ iosSigningEnabled: process.env.IOS_SIGNING_ENABLED === "true" })
    .map((asset) => asset.assetName),
  "update.json"
];

function setOutput(name, value) {
  const outputPath = process.env.GITHUB_OUTPUT;
  if (!outputPath) {
    throw new Error("GITHUB_OUTPUT is not set");
  }
  appendFileSync(outputPath, `${name}=${value}\n`);
}

export function parseManualTag(tagName) {
  if (!tagName?.startsWith("v")) {
    throw new Error(`Expected a vX.Y.Z tag, received '${tagName ?? ""}'.`);
  }
  const version = tagName.slice(1);
  if (!VERSION_PATTERN.test(version)) {
    throw new Error(`Tag '${tagName}' is not a valid vX.Y.Z release tag.`);
  }
  return version;
}

export function classifyWorkflowRef({ refType, refName }) {
  if (refType === "tag") {
    return { kind: "manual", version: parseManualTag(refName), tag: refName };
  }
  if (refType === "branch" && AUTOMATIC_RELEASE_BRANCHES.includes(refName)) {
    return { kind: "automatic" };
  }
  throw new Error(`Unsupported release ref '${refType ?? ""}:${refName ?? ""}'.`);
}

export function releaseHasExpectedAsset(release) {
  const assets = release.assets ?? [];
  return EXPECTED_ASSETS.every((name) =>
    assets.some((asset) => asset.name === name && asset.state === "uploaded" && asset.size > 0)
  );
}

export function incompleteReleases(releases) {
  return releases.filter((release) => {
    if (release.prerelease) {
      return false;
    }
    return !releaseHasExpectedAsset(release);
  });
}

function ghApi(args) {
  const result = spawnSync("gh", ["api", ...args], {
    encoding: "utf8",
    env: { ...process.env, GH_TOKEN: process.env.GITHUB_TOKEN || "" },
    stdio: ["ignore", "pipe", "pipe"]
  });
  if (result.status !== 0) {
    const err = result.stderr?.trim();
    if (err?.includes("HTTP 404")) {
      return null;
    }
    throw new Error(`gh api failed: ${err || result.status}`);
  }
  try {
    return JSON.parse(result.stdout);
  } catch {
    return result.stdout;
  }
}

function getGitHubRelease(tag) {
  const [owner, repo] = (process.env.GITHUB_REPOSITORY || "").split("/");
  return ghApi([`repos/${owner}/${repo}/releases/tags/${tag}`]);
}

function listGitHubReleases() {
  const [owner, repo] = (process.env.GITHUB_REPOSITORY || "").split("/");
  const all = [];
  let page = 1;
  while (true) {
    const releases = ghApi([`repos/${owner}/${repo}/releases?per_page=100&page=${page}`]);
    if (!Array.isArray(releases) || releases.length === 0) {
      break;
    }
    all.push(...releases);
    if (releases.length < 100) {
      break;
    }
    page++;
  }
  return all;
}

export function semanticReleaseDryRunVersion(result) {
  if (result.error) {
    throw new Error(`semantic-release dry-run could not be started: ${result.error.message}`);
  }
  const output = (result.stdout || "") + (result.stderr || "");
  if (result.status !== 0) {
    const context = (result.stderr || "").trim() || output.trim() || "no output";
    throw new Error(`semantic-release dry-run failed (exit code ${result.status}): ${context}`);
  }
  const match = output.match(/the next release version is ([0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z.-]+)?)/i);
  return match ? match[1] : null;
}

function runSemanticReleaseDryRun() {
  const result = spawnSync(
    "npx",
    ["semantic-release", "--dry-run", "--no-ci"],
    {
      encoding: "utf8",
      env: { ...process.env, RESOLVE_DRY_RUN: "true" },
      stdio: ["ignore", "pipe", "pipe"]
    }
  );
  return semanticReleaseDryRunVersion(result);
}

function resolveManualRelease(version, tag) {
  const existing = getGitHubRelease(tag);
  if (existing) {
    if (releaseHasExpectedAsset(existing)) {
      return { released: "false", reason: `Release ${tag} already exists with all assets.`, version, tag, release_kind: "manual", release_action: "none" };
    }
    return { released: "true", reason: `Release ${tag} exists but is missing assets; repair.`, version, tag, release_kind: "manual", release_action: "upload-existing" };
  }
  return { released: "true", reason: `Create new manual release ${tag}.`, version, tag, release_kind: "manual", release_action: "create" };
}

export function repairIncompleteRelease(release) {
  const version = parseManualTag(release.tag_name);
  return { released: "true", reason: `Repair oldest incomplete release ${release.tag_name}.`, version, tag: release.tag_name, release_kind: "automatic", release_action: "upload-existing" };
}

function resolveAutomaticRelease() {
  const version = runSemanticReleaseDryRun();
  if (!version) {
    const allReleases = listGitHubReleases();
    const incomplete = incompleteReleases(allReleases);
    if (incomplete.length === 0) {
      return { released: "false", reason: "No releasable commits and no incomplete releases.", version: "", tag: "", release_kind: "automatic", release_action: "none" };
    }
    const oldest = incomplete[incomplete.length - 1];
    return repairIncompleteRelease(oldest);
  }

  const tag = `v${version}`;
  const existing = getGitHubRelease(tag);
  if (existing) {
    if (releaseHasExpectedAsset(existing)) {
      return { released: "false", reason: `Release ${tag} already exists with all assets.`, version, tag, release_kind: "automatic", release_action: "none" };
    }
    return { released: "true", reason: `Release ${tag} exists but is missing assets; repair.`, version, tag, release_kind: "automatic", release_action: "upload-existing" };
  }
  return { released: "true", reason: `Create new automatic release ${tag}.`, version, tag, release_kind: "automatic", release_action: "create" };
}

export function resolveReleaseVersion() {
  const refType = process.env.GITHUB_REF_TYPE;
  const refName = process.env.GITHUB_REF_NAME;
  const classification = classifyWorkflowRef({ refType, refName });

  let result;
  if (classification.kind === "manual") {
    result = resolveManualRelease(classification.version, classification.tag);
  } else {
    result = resolveAutomaticRelease();
  }

  for (const [key, value] of Object.entries(result)) {
    setOutput(key, value);
  }

  console.log(JSON.stringify(result, null, 2));
}

const isMainModule = process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href;
if (isMainModule) {
  resolveReleaseVersion();
}
