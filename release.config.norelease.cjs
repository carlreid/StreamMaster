/**
 * @type {import('semantic-release').GlobalConfig}
 */

module.exports = {
  branches: [
    // "+([0-9])?(.{+([0-9]),x}).x",
    {
      channel: "main",
      name: "main",
      prerelease: false
    },
    {
      name: "!main",
      prerelease: true
    }
  ],
  ci: false,
  debug: false,
  dryRun: false,
  plugins: [
    [
      "@semantic-release/commit-analyzer",
      {
        preset: "angular",
        releaseRules: [
          { type: "docs", scope: "README", release: "minor" },
          { type: "refactor", release: "minor" },
          { type: "style", release: "patch" },
          // { type: "build", release: "false" },
          { scope: "no-release", release: false },
          { type: "update", release: "patch" }
        ],
        parserOpts: {
          noteKeywords: ["BREAKING CHANGE", "BREAKING CHANGES"]
        }
      }
    ],
    [
      "@semantic-release/exec",
      {
        verifyConditionsCmd: ":",
        prepareCmd:
          "node src/updateAssemblyInfo.js ${nextRelease.version} ${nextRelease.gitHead} ${nextRelease.channel}",
        publishCmd: [
          "node src/updateAssemblyInfo.js ${nextRelease.version} ${nextRelease.gitHead} ${nextRelease.channel}",
          "git add ./src/StreamMaster.API/AssemblyInfo.cs",
          'git diff-index --quiet HEAD || git commit -m "chore: update AssemblyInfo.cs to version ${nextRelease.version}"'
        ].join(" && ")
      }
    ]
  ]
};
