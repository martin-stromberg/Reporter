// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

import { createHash } from "node:crypto";
import { readFileSync, statSync, writeFileSync } from "node:fs";
import { join } from "node:path";
import { pathToFileURL } from "node:url";

export function parseReleaseAssets(spec) {
  if (!spec?.trim()) {
    throw new Error("RELEASE_ASSETS is not set or empty.");
  }
  return spec
    .split(";")
    .filter((entry) => entry.trim().length > 0)
    .map((entry) => {
      const parts = entry.split(":");
      if (parts.length !== 3 || parts.some((part) => part.trim() === "")) {
        throw new Error(`Invalid RELEASE_ASSETS entry '${entry}'. Expected format 'name:platform:runtimeIdentifier'.`);
      }
      const [assetName, platform, runtimeIdentifier] = parts;
      return { assetName, platform, runtimeIdentifier };
    });
}

export function createUpdateManifest({ version, tag, repository, assets, baseDir = process.cwd() }) {
  if (!version) {
    throw new Error("RELEASE_VERSION is not set.");
  }
  if (!tag) {
    throw new Error("RELEASE_TAG is not set.");
  }
  if (!repository) {
    throw new Error("GITHUB_REPOSITORY is not set.");
  }

  const manifestAssets = assets.map(({ assetName, platform, runtimeIdentifier }) => {
    const filePath = join(baseDir, assetName);
    let stat;
    try {
      stat = statSync(filePath);
    } catch {
      throw new Error(`Expected release asset '${assetName}' is missing at ${filePath}.`);
    }
    if (stat.size === 0) {
      throw new Error(`Expected release asset '${assetName}' at ${filePath} is empty.`);
    }
    const sha256 = createHash("sha256").update(readFileSync(filePath)).digest("hex");
    return {
      platform,
      runtimeIdentifier,
      assetName,
      assetUrl: `https://github.com/${repository}/releases/download/${tag}/${assetName}`,
      sha256,
      sizeBytes: stat.size
    };
  });

  return {
    version,
    releaseNotes: `Reporter release ${tag}`,
    publishedAt: new Date().toISOString().replace(/\.\d{3}Z$/, "Z"),
    assets: manifestAssets
  };
}

export function writeUpdateManifest(manifest, outputPath = "update.json") {
  writeFileSync(outputPath, `${JSON.stringify(manifest, null, 2)}\n`);
}

function main() {
  const assets = parseReleaseAssets(process.env.RELEASE_ASSETS);
  const manifest = createUpdateManifest({
    version: process.env.RELEASE_VERSION,
    tag: process.env.RELEASE_TAG,
    repository: process.env.GITHUB_REPOSITORY,
    assets
  });
  writeUpdateManifest(manifest);
  console.log(`update.json written for version ${manifest.version} (${manifest.assets.length} assets).`);
}

const isMainModule = process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href;
if (isMainModule) {
  main();
}
