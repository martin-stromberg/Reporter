const releasePlugins = [
  ["@semantic-release/commit-analyzer", { preset: "conventionalcommits" }],
  ["@semantic-release/release-notes-generator", { preset: "conventionalcommits" }],
  [
    "@semantic-release/github",
    {
      assets: [
        { path: process.env.RELEASE_ASSET_PATHS?.split(";")[0], name: "release-win-x64.zip" },
        { path: process.env.RELEASE_MANIFEST_PATH, name: "update.json" }
      ],
      successComment: false,
      failComment: false
    }
  ]
];

const dryRunPlugins = [["@semantic-release/commit-analyzer", { preset: "conventionalcommits" }]];

module.exports = {
  branches: ["main"],
  tagFormat: "v${version}",
  plugins: process.env.RESOLVE_DRY_RUN === "true" ? dryRunPlugins : releasePlugins
};
