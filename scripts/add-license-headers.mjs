// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

// Inserts the short PolyForm Noncommercial license header into every text file
// in the repository that supports comments. Idempotent: files that already
// contain the header are skipped. Usage: node scripts/add-license-headers.mjs
// [--check] [--staged]
//   --check   only lists files still missing the header, exit code 1 if any
//   --staged  like --check, but limited to files staged in git (pre-commit)

import { execFileSync } from "node:child_process";
import { existsSync } from "node:fs";
import { readdir, readFile, writeFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const CHECK_ONLY = process.argv.includes("--check") || process.argv.includes("--staged");
const STAGED_ONLY = process.argv.includes("--staged");

const MARKER = "PolyForm Noncommercial License";
const TEXT = "Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.";

const SKIP_DIRS = new Set([".git", ".vs", "node_modules", "bin", "obj", "TestResults"]);
const SKIP_FILES = new Set(["LICENSE"]);

// Comment style per extension / basename.
const STYLE_BY_EXT = new Map([
  [".cs", "slash"], [".mjs", "slash"], [".js", "slash"], [".c", "slash"], [".h", "slash"], [".cpp", "slash"],
  [".ps1", "hash"], [".py", "hash"], [".yml", "hash"], [".yaml", "hash"], [".sh", "hash"],
  [".cmd", "rem"], [".bat", "rem"],
  [".xaml", "xml"], [".csproj", "xml"], [".props", "xml"], [".targets", "xml"], [".resx", "xml"],
  [".xml", "xml"], [".plist", "xml"], [".xcprivacy", "xml"], [".manifest", "xml"], [".appxmanifest", "xml"],
  [".runsettings", "xml"], [".svg", "xml"], [".md", "xml"], [".html", "xml"], [".htm", "xml"],
  [".txt", "plain"], [".log", "plain"],
]);
const STYLE_BY_NAME = new Map([
  [".gitignore", "hash"], ["pre-commit", "hash"], ["pre-push", "hash"], ["post-commit", "hash"],
]);

function headerFor(style) {
  switch (style) {
    case "slash": return `// ${TEXT}`;
    case "hash": return `# ${TEXT}`;
    case "rem": return `REM ${TEXT}`;
    case "xml": return `<!-- ${TEXT} -->`;
    case "plain": return TEXT;
    default: return null;
  }
}

async function* walk(dir) {
  for (const entry of await readdir(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      if (!SKIP_DIRS.has(entry.name)) yield* walk(full);
    } else if (entry.isFile()) {
      yield full;
    }
  }
}

function styleFor(file) {
  const base = path.basename(file);
  if (SKIP_FILES.has(base)) return null;
  return STYLE_BY_NAME.get(base) ?? STYLE_BY_EXT.get(path.extname(base).toLowerCase()) ?? null;
}

function insertHeader(content, eol, header, ext) {
  // Keep a leading shebang or XML declaration as the very first line.
  const firstBreak = content.indexOf("\n");
  const firstLine = (firstBreak === -1 ? content : content.slice(0, firstBreak)).replace(/\r$/, "");
  if (firstLine.startsWith("#!") || /^<\?xml[\s?]/i.test(firstLine.trimStart())) {
    const rest = firstBreak === -1 ? "" : content.slice(firstBreak + 1);
    return firstLine + eol + header + eol + eol + rest;
  }
  // HTML: keep <!DOCTYPE ...> on the first line.
  if (ext === ".html" && /^<!doctype/i.test(firstLine.trimStart())) {
    const rest = firstBreak === -1 ? "" : content.slice(firstBreak + 1);
    return firstLine + eol + header + eol + eol + rest;
  }
  // Markdown with YAML frontmatter: insert after the closing --- line.
  if (ext === ".md" && firstLine.trim() === "---") {
    const lines = content.split(/\r?\n/);
    for (let i = 1; i < lines.length; i++) {
      if (lines[i].trim() === "---") {
        return lines.slice(0, i + 1).join(eol) + eol + header + eol + eol + lines.slice(i + 1).join(eol);
      }
    }
  }
  return header + eol + eol + content;
}

function stagedFiles() {
  const out = execFileSync(
    "git",
    ["diff", "--cached", "--name-only", "--diff-filter=ACMR"],
    { cwd: ROOT, encoding: "utf8" },
  );
  return out
    .split(/\r?\n/)
    .filter(Boolean)
    .map((f) => path.join(ROOT, f))
    .filter((f) => existsSync(f));
}

let added = 0;
const missing = [];
const candidates = STAGED_ONLY ? stagedFiles() : walk(ROOT);
for await (const file of candidates) {
  const style = styleFor(file);
  if (!style) continue;

  const buf = await readFile(file);
  const hasBom = buf.length >= 3 && buf[0] === 0xef && buf[1] === 0xbb && buf[2] === 0xbf;
  const content = buf.toString("utf8").slice(hasBom ? 1 : 0);
  const rel = path.relative(ROOT, file);

  // Skip files that already carry the header anywhere (it may sit behind
  // YAML frontmatter) or that reference the license in their preamble.
  if (content.includes(TEXT) || content.slice(0, 800).includes(MARKER)) continue;
  if (content.trim() === "") continue; // empty file

  if (CHECK_ONLY) {
    missing.push(rel);
    continue;
  }

  const eol = content.includes("\r\n") ? "\r\n" : "\n";
  const next = insertHeader(content, eol, headerFor(style), path.extname(file).toLowerCase());
  await writeFile(file, (hasBom ? "\ufeff" : "") + next, "utf8");
  added++;
  console.log(`header added: ${rel}`);
}

if (CHECK_ONLY) {
  for (const rel of missing) console.log(`missing header: ${rel}`);
  console.log(`${missing.length} file(s) missing a license header`);
  if (missing.length > 0) {
    console.log("Run 'node scripts/add-license-headers.mjs' to add the missing headers.");
    process.exit(1);
  }
  process.exit(0);
}
console.log(`done — ${added} file(s) updated`);
