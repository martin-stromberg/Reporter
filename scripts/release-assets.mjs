import { appendFileSync } from "node:fs";
import { pathToFileURL } from "node:url";

export function buildReleaseAssets({ iosSigningEnabled = false } = {}) {
  const assets = [
    { assetName: "release-win-x64.zip", platform: "windows", runtimeIdentifier: "win-x64" },
    { assetName: "release-android.apk", platform: "android", runtimeIdentifier: "android" }
  ];
  if (iosSigningEnabled) {
    assets.push({ assetName: "release-ios.ipa", platform: "ios", runtimeIdentifier: "ios" });
  }
  return assets;
}

export function releaseAssetEnv(assets) {
  return {
    RELEASE_ASSETS: assets
      .map((asset) => `${asset.assetName}:${asset.platform}:${asset.runtimeIdentifier}`)
      .join(";"),
    RELEASE_ASSET_PATHS: assets.map((asset) => asset.assetName).join(";"),
    RELEASE_ASSET_FILES: assets.map((asset) => asset.assetName).join(" ")
  };
}

export function emitReleaseAssetEnv({
  iosSigningEnabled = process.env.IOS_SIGNING_ENABLED === "true",
  githubEnv = process.env.GITHUB_ENV
} = {}) {
  if (!githubEnv) {
    throw new Error("GITHUB_ENV is not set.");
  }
  const env = releaseAssetEnv(buildReleaseAssets({ iosSigningEnabled }));
  for (const [name, value] of Object.entries(env)) {
    appendFileSync(githubEnv, `${name}=${value}\n`);
  }
  return env;
}

const isMainModule = process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href;
if (isMainModule) {
  const env = emitReleaseAssetEnv();
  console.log(JSON.stringify(env, null, 2));
}
